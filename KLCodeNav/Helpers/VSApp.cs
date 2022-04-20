using EnvDTE;
using EnvDTE80;

namespace KLCodeNav
{
    public class VSApp
    {
        const string solutionFolderGuid = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";

        public VSApp(DTE2 dte)
        {
            _dte = dte;
        }
        public DTE2 DTE
        {
            get { return _dte; }
        }
        public Solution Solution
        {
            get { return _dte.Solution; }
        }
        public bool IsBuilding
        {
            get
            {
                if ((_dte.Solution != null)&& (_dte.Solution.SolutionBuild != null))
                {
                    return _dte.Solution.SolutionBuild.BuildState == vsBuildState.vsBuildStateInProgress;
                }
                return false;
            }
        }
        public bool IsDebugging
        {
            get
            {
                if (_dte.Debugger != null)
                {
                    return _dte.Debugger.CurrentMode != dbgDebugMode.dbgDesignMode;
                }
                return false;
            }
        }
        public UIHierarchyItem FindUIProject(Project project)
        {
            return FindUIProject(project, _dte.ToolWindows.SolutionExplorer.UIHierarchyItems);
        }
        private static UIHierarchyItem FindUIProject(Project project, UIHierarchyItems items)
        {
            foreach (UIHierarchyItem item in items)
            {
                ProjectItem pI = item.Object as ProjectItem;
                Project projectItem = pI != null ? pI.Object as Project : item.Object as Project;

                if (projectItem != null)
                {
                    if (projectItem.UniqueName == project.UniqueName)
                    {
                        return item;
                    }
                    //if (projectItem.Kind != VSMisc.PROJECT_KIND_SOLUTION_FOLDER)
                    if (projectItem.Kind != solutionFolderGuid)
                    {
                        continue;
                    }
                }
                UIHierarchyItem item2 = FindUIProject(project, item.UIHierarchyItems);
                if (item2 != null)
                {
                    return item2;
                }
            }
            return null;
        }
        public string GetItemPath(object o)
        {
            if (o is Solution)
            {
                return ((Solution)o).FullName;
            }
            if (o is Project)
            {
                Project project = (Project)o;
                if (IsWebProject(project))
                {
                    return project.Properties.Item(@"FullPath").Value + @"\\";
                }
                return project.FullName;
            }
            if (o is ProjectItem)
            {
                ProjectItem projectItem = (ProjectItem)o;
                if (projectItem.SubProject != null)
                {
                    return GetItemPath(projectItem.SubProject);
                }
                return projectItem.get_FileNames(0);
            }
            return null;
        }
        private static bool IsWebProject(Project project)
        {
            foreach (Property p in project.Properties)
            {
                if (p.Name == @"OpenedURL")
                {
                    return true;
                }
            }
            return false;
        }
        public string GetSelectedText()
        {
            TextSelection selection = ((TextSelection)_dte.ActiveWindow.Selection);
            if ((selection != null) && IsSignificant(selection.Text))
            {
                return selection.Text;
            }
            return GetNearestWord(selection);
        }
        private static string GetNearestWord(TextSelection selection)
        {
            string nearestWord = GetWord(selection, true);
            if (!IsSignificant(nearestWord))
            {
                nearestWord = GetWord(selection, false);
                if (!IsSignificant(nearestWord))
                {
                    nearestWord = string.Empty;
                }
            }
            return nearestWord;
        }
        private static string GetWord(TextSelection selection, bool searchOnLeftSide)
        {
            if (selection == null)
            {
                return null;
            }
            EditPoint activePoint = selection.ActivePoint.CreateEditPoint();
            if (searchOnLeftSide)
            {
                selection.WordLeft(false, 1);
                selection.WordRight(true, 1);
            }
            else
            {
                selection.WordRight(false, 1);
                selection.WordLeft(true, 1);
            }
            string word = selection.Text.Trim();
            selection.MoveToPoint(activePoint, false);
            return word;
        }
        private static bool IsSignificant(string text)
        {
            return (text != null) && (text.Trim().Length >= 2);
        }
        private readonly DTE2 _dte;
    }
}
