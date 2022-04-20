using System;
using System.IO;
using System.Collections.Generic;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;

namespace KLCodeNav
{
    public static class DTEHelper
    {
        private static readonly Regex GenericPartRegex = new Regex(@"(<.*<)|(\(Of .*\))", RegexOptions.Compiled);

        public static DTE2 GetDte()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var provider = ServiceProvider.GlobalProvider;
            var vs = (DTE2)provider.GetService(typeof(DTE));

            if (vs == null)
            {
                throw new InvalidOperationException("DTE not found.");
            }
            return vs;
        }

        private static string GetFullPath(string path, string basePath)
        {
            bool isAbsolute = Path.IsPathRooted(path);
            if (isAbsolute)
            {
                return path;
            }
            string saved = Environment.CurrentDirectory;
            Environment.CurrentDirectory = basePath;
            try
            {
                return Path.GetFullPath(path);
            }
            finally
            {
                Environment.CurrentDirectory = saved;
            }
        }

        public static List<EnvDTE.Project> GetProjects(DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var projects = dte.Solution.Projects;
            var list = new List<EnvDTE.Project>();
            var item = projects.GetEnumerator();

            while (item.MoveNext())
            {
                if (!(item.Current is EnvDTE.Project project))
                {
                    continue;
                }

                if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    list.AddRange(GetSolutionFolderProjects(project));
                }
                else
                {
                    list.Add(project);
                }
            }
            return list;
        }

        private static IEnumerable<EnvDTE.Project> GetSolutionFolderProjects(EnvDTE.Project solutionFolder)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var list = new List<EnvDTE.Project>();
            for (var i = 1 ; i <= solutionFolder.ProjectItems.Count ; i++)
            {
                Project subProject = solutionFolder.ProjectItems.Item(i).SubProject;

                if (subProject == null)
                    continue;

                if (subProject.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                    list.AddRange(GetSolutionFolderProjects(subProject));
                else
                    list.Add(subProject);
            }

            return list;
        }


        public static string GetOutputAssemblyFileName()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = GetDte();
            var sb = (SolutionBuild2)dte.Solution.SolutionBuild;
            var projects = sb.StartupProjects as Array;
            if (projects == null)
            {
                return null;
            }
            var activeProject = dte.Solution.Item(projects.GetValue(0));
            var activeConf = activeProject.ConfigurationManager.ActiveConfiguration;
            string outFn = activeConf.Properties.Item("OutputPath").Value.ToString();
            string fullPath = GetFullPath(outFn, Path.GetDirectoryName(activeProject.FileName));
            return Path.Combine(fullPath, activeProject.Properties.Item("OutputFileName").Value.ToString());
        }

        private static CodeFunction GetCodeFunction(FileCodeModel2 fcm, TextPoint point)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                var element = fcm.CodeElementFromPoint(point, vsCMElement.vsCMElementFunction);
                return (CodeFunction)element;
            }
            catch
            {
                return null;
            }
        }
    }
}
