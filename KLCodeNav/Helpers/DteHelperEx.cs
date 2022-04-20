using System;
using System.Collections.Generic;
using System.Text;
using EnvDTE;
using System.IO;
using VSLangProj;
using System.Diagnostics;
using System.CodeDom.Compiler;
using System.Collections;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;

namespace KLCodeNav
{
    public static class DteHelperEx
    {
        private const string CurrentWebsiteLanguagePropertyItem = "CurrentWebsiteLanguage";
        private const string CurrentWebsiteLanguagePropertyValue = "Visual C#";

        public static Project FindProject(_DTE vs, Predicate<Project> match)
        {
            foreach (Project project in vs.Solution.Projects)
            {
                if (match(project))
                {
                    return project;
                }
                Project found = FindProjectInternal(project.ProjectItems, match);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        public static Project FindProjectByName(DTE dte, string name, bool isWeb)
        {
            Project project = null;

            if (!isWeb)
            {
                project = FindProject(dte, delegate (Project internalProject)
                {
                    return internalProject.Name == name;
                });
            }
            else
            {
                foreach (Project projectTemp in dte.Solution.Projects)
                {
                    if (projectTemp.Name.Contains(name))
                    {
                        project = projectTemp;
                        break;
                    }

                    if (projectTemp.ProjectItems != null)
                    {
                        Project projectTemp1 = FindProjectByName(projectTemp.ProjectItems, name);
                        if (projectTemp1 != null)
                        {
                            project = projectTemp1;
                            break;
                        }
                    }
                }
            }
            return project;
        }

        public static ProjectItem FindItem(_DTE vs, Predicate<Project> match)
        {
            throw new NotImplementedException();
        }

       /* public static bool ProjectExists(Solution solution, string projectName, LanguageType language)
        {
            bool exists = false;

            string solutionPath = Path.GetDirectoryName(
                            solution.Properties.Item("Path").Value.ToString());

            if (Directory.Exists(solutionPath))
            {
                string projectFile = string.Concat(projectName, GetProjectExtension(language));

                exists = (Directory.GetFiles(
                            solutionPath,
                            projectFile,
                            SearchOption.AllDirectories).Length > 0);
            }

            return exists;
        }*/

        public static ProjectItem FindItemByName(ProjectItems collection, string name, bool recursive)
        {
            if (collection != null)
            {
                foreach (ProjectItem item1 in collection)
                {
                    if (item1.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return item1;
                    }
                    if (recursive)
                    {
                        ProjectItem item2 = DteHelperEx.FindItemByName(item1.ProjectItems, name, recursive);
                        if (item2 != null)
                        {
                            return item2;
                        }
                    }
                }
            }
            return null;
        }

        public static ProjectItem FindContainingProjectItem(_DTE dte, CodeType type)
        {
            foreach (Project project in new ProjectIterator(dte))
            {
                foreach (ProjectItem item in project.ProjectItems)
                {
                    if (item.FileCodeModel != null)
                    {
                        foreach (CodeElement element in item.FileCodeModel.CodeElements)
                        {
                            if (element.Kind == vsCMElement.vsCMElementNamespace)
                            {
                                foreach (CodeElement member in ((CodeNamespace)element).Members)
                                {
                                    if (member.Kind == type.Kind &&
                                        member.FullName.Equals(type.FullName, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        return item;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        /*public static bool IsWebCSharpProject(object target)
        {
            Project project = null;
            if (target is Project)
            {
                project = (Project)target;
            }
            else if (target is ProjectItem)
            {
                project = ((ProjectItem)target).ContainingProject;
            }

            if (project != null &&
                DteHelper.IsWebProject(project) &&
                project.Properties != null)
            {
                try
                {
                    Property property = project.Properties.Item(CurrentWebsiteLanguagePropertyItem);
                    return (property.Value != null &&
                        property.Value.ToString().Equals(CurrentWebsiteLanguagePropertyValue, StringComparison.InvariantCultureIgnoreCase));
                }
                catch (Exception exception)
                {
                    Trace.TraceError(exception.ToString());
                    return false;
                }
            }

            return false;
        }*/


        public static void ShowMessageInOutputWindow(DTE dte, string message)
        {
            ShowMessageInOutputWindow(dte, message, null);
        }

        public static void ShowMessageInOutputWindow(DTE dte, string message, string paneName)
        {
             OutputWindow outputWindow = ((EnvDTE80.DTE2)dte).ToolWindows.OutputWindow;
            OutputWindowPane pane = GetPane(outputWindow, paneName);
            pane.OutputString(message);
            pane.OutputString(Environment.NewLine);
            pane.Activate();
            outputWindow.Parent.Activate();
        }

        /*public static CodeDomProvider GetCodeDomProvider(Project project)
        {
            if (project != null)
            {
                return CodeDomProvider.CreateProvider(
                    CodeDomProvider.GetLanguageFromExtension(GetDefaultExtension(project)));
            }

            return CodeDomProvider.CreateProvider("C#");
        }
*/
        private static OutputWindowPane GetPane(OutputWindow outputWindow, string panelName)
        {
            if (!string.IsNullOrEmpty(panelName))
            {
                foreach (OutputWindowPane pane in outputWindow.OutputWindowPanes)
                {
                    if (pane.Name.Equals(panelName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return pane;
                    }
                }
                return outputWindow.OutputWindowPanes.Add(panelName);
            }
            return outputWindow.ActivePane;
        }

        private static Project FindProjectInternal(ProjectItems items, Predicate<Project> match)
        {
            if (items == null)
            {
                return null;
            }

            foreach (ProjectItem item in items)
            {
                Project project = item.SubProject ?? item.Object as Project; ;
                if (project != null)
                {
                    if (match(project))
                    {
                        return project;
                    }
                    project = FindProjectInternal(project.ProjectItems, match);
                    if (project != null)
                    {
                        return project;
                    }
                }
            }
            return null;
        }

        private static Project FindProjectByName(ProjectItems items, string name)
        {
            foreach (ProjectItem item1 in items)
            {
                if ((item1.Object is Project) && (((Project)item1.Object).Name.Contains(name)))
                {
                    return (item1.Object as Project);
                }

                if (item1.ProjectItems != null)
                {
                    Project project1 = FindProjectByName(item1.ProjectItems, name);
                    if (project1 != null)
                    {
                        return project1;
                    }
                }
            }
            return null;
        }

        public class ProjectIterator : IEnumerable<Project>
        {
            private _DTE dte;

            public ProjectIterator(_DTE dte)
            {
                this.dte = dte;
            }

            public IEnumerator<Project> GetEnumerator()
            {
                if (dte.Solution.Projects == null)
                {
                    yield break;
                }

                foreach (Project project in dte.Solution.Projects)
                {
                    yield return project;

                    foreach (Project subProject in GetSubprojects(project))
                    {
                        yield return subProject;
                    }
                }
            }

            private IEnumerable<Project> GetSubprojects(Project project)
            {
                if (project.ProjectItems == null)
                {
                    yield break;
                }

                foreach (ProjectItem item in project.ProjectItems)
                {
                    Project subProject = item.SubProject ?? item.Object as Project;
                    if (subProject != null)
                    {
                        yield return subProject;
                        foreach (Project subSubProject in GetSubprojects(subProject))
                        {
                            yield return subSubProject;
                        }
                    }
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

        }

        public class ProjectItemIterator : IEnumerable<ProjectItem>
        {
            private Project project;
            private ProjectItem projectItem;

            public ProjectItemIterator(Project project)
            {
                this.project = project;
            }

            public ProjectItemIterator(ProjectItem projectItem)
            {
                this.projectItem = projectItem;
            }

            public IEnumerator<ProjectItem> GetEnumerator()
            {
                if (project != null)
                {
                    foreach (ProjectItem item in project.ProjectItems)
                    {
                        yield return item;
                        foreach (ProjectItem subItem in EnumerateProjectItem(item))
                        {
                            yield return subItem;
                        }
                    }
                }
                else if (projectItem != null)
                {
                    foreach (ProjectItem subItem in EnumerateProjectItem(projectItem))
                    {
                        yield return subItem;
                    }
                }
                else
                {
                    yield break;
                }
            }

            private IEnumerable<ProjectItem> EnumerateProjectItem(ProjectItem item)
            {
                if (item.ProjectItems == null)
                {
                    yield break;
                }

                foreach (ProjectItem subItem in item.ProjectItems)
                {
                    yield return subItem;
                    foreach (ProjectItem subSubItem in EnumerateProjectItem(subItem))
                    {
                        yield return subSubItem;
                    }
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

        }

        /*public class ProjectReferencesIterator : IEnumerable<Project>
        {
            private Project project;

            public ProjectReferencesIterator(Project project)
            {
                this.project = project;
            }

            public IEnumerator<Project> GetEnumerator()
            {
                if (DteHelper.IsWebProject(project))
                {
                    return IterateWebProjectReferences((VSWebSite)project.Object);
                }
                else
                {
                    return IterateProjectReferences((VSProject)project.Object);
                }
            }

            private IEnumerator<Project> IterateProjectReferences(VSProject vsProject)
            {
                if (vsProject.References == null)
                {
                    yield break;
                }

                foreach (Reference reference in vsProject.References)
                {
                    if (reference.SourceProject != null)
                    {
                        yield return reference.SourceProject;
                    }
                }
            }

            private IEnumerator<Project> IterateWebProjectReferences(VSWebSite vsWebSite)
            {
                if (vsWebSite.References == null)
                {
                    yield break;
                }

                foreach (AssemblyReference reference in vsWebSite.References)
                {
                    if (reference.ReferencedProject != null)
                    {
                        yield return reference.ReferencedProject;
                    }
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }

        }*/

        public class CodeElementsIterator : IEnumerable<CodeElement>
        {
            private CodeElements elements;

            public CodeElementsIterator(ProjectItem projectItem)
            {
                if (projectItem.FileCodeModel != null)
                {
                    elements = projectItem.FileCodeModel.CodeElements;
                }
            }

            public CodeElementsIterator(CodeElement codeElement)
            {
                elements = codeElement.Children;
            }

            public IEnumerator<CodeElement> GetEnumerator()
            {
                if (elements == null)
                {
                    yield break;
                }
                foreach (CodeElement element in EnumerateCodeElements(elements))
                {
                    yield return element;
                }
            }

            private IEnumerable<CodeElement> EnumerateCodeElements(CodeElements elements)
            {
                foreach (CodeElement element in elements)
                {
                    yield return element;
                    foreach (CodeElement subElement in EnumerateCodeElements(element.Children))
                    {
                        yield return subElement;
                    }
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

        }

    }
}
