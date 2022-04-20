using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.Modeling;
using System.IO;
using EnvDTE80;
using System.Windows.Forms;

namespace KLCodeNav
{
    public static class DTEHelper3
    {
        public static DTE DTE { get; set; }
        public static DTE2 DTE2 { get; set; }

        public static Project Project
        {
            get
            {
                object[] projects = ((object[])DTE.ActiveSolutionProjects);
                if (projects.Length > 0)
                {
                    return ((object[])DTE.ActiveSolutionProjects)[0] as Project;
                }
                else return null;
            }
        }

        private static OutputWindowPane outputPane = null;

        private static OutputWindowPane OutputWindowPane
        {
            get
            {
                if (outputPane == null)
                {
                    OutputWindow outputWindow = DTEHelper3.DTE.Windows.Item(EnvDTE.Constants.vsWindowKindOutput).Object as OutputWindow;

                    foreach (OutputWindowPane pane in outputWindow.OutputWindowPanes)
                    {
                        if (pane.Guid.Equals(BuildOutputPaneGuid))
                        {
                            outputPane = pane;
                            break;
                        }
                    }
                    if (outputPane == null)
                    {
                        outputWindow.OutputWindowPanes.Add("Build");
                    }
                    else
                    {
                        DTE.ExecuteCommand("View.Output");
                        outputPane.Activate();
                    }
                }
                return outputPane;
            }
        }

        const string BuildOutputPaneGuid = "{1BD8A850-02D1-11D1-BEE7-00A0C913D1F8}";

        public static void Initialize(IServiceProvider serviceProvider)
        {
            DTE = serviceProvider.GetService(typeof(DTE)) as DTE;
            DTE2 = serviceProvider.GetService(typeof(DTE2)) as DTE2;
        }

        public static string GetProjectItemPath(string projectItemName)
        {
            string result = string.Empty;
            foreach (ProjectItem item in Project.ProjectItems)
            {
                if (item.Name.Equals(projectItemName))
                {
                    result = item.get_FileNames(0);
                    break;
                }
            }

            return result;
        }

        public static void OpenFile(string filePath)
        {
            DTE.ItemOperations.OpenFile(filePath, EnvDTE.Constants.vsViewKindAny);
        }

        public static List<string> GetAllProjectItemNames()
        {
            List<string> result = new List<string>();
            foreach (ProjectItem item in Project.ProjectItems)
            {
                result.Add(item.Name);
            }
            return result;
        }

        public static string GetProjectFolderPath()
        {
            return Path.GetDirectoryName(Project.FullName);
        }


        public static void AddTextToProjectAsFile(string targetFileName, string text)
        {
            string tempFilePath = Path.Combine(Path.GetTempPath(), targetFileName);
            StreamWriter sr = new StreamWriter(tempFilePath);
            sr.Write(text);
            sr.Close();
            ProjectItem projectItemToBeDeleted = null;
            foreach (ProjectItem projectItem in Project.ProjectItems)
            {
                if (projectItem.Name == targetFileName)
                {
                    projectItemToBeDeleted = projectItem;
                }
            }

            if (projectItemToBeDeleted != null)
            {
                projectItemToBeDeleted.Delete();
            }
            else
            {
                string fileName = Path.Combine(Path.GetDirectoryName(Project.FullName), targetFileName);
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
            Project.ProjectItems.AddFromFileCopy(tempFilePath);
        }

        public static void AddErrorToErrorListFromOutputPane(string errorMessage)
        {
            OutputWindowPane.OutputTaskItemString(errorMessage, vsTaskPriority.vsTaskPriorityHigh, "", vsTaskIcon.vsTaskIconCompile, "Confeaturator", 0, errorMessage, true);
            DTE.ExecuteCommand("View.ErrorList");
            try
            {
                DTE.Windows.Item("Error List").Activate();
            }
            catch (ArgumentException)
            {

            }

        }

        public static void ClearErrorsFromOutputPane()
        {
            OutputWindowPane.Clear();
        }

        public static string GetFullProjectItemPath(string projectItemFileName)
        {
            return Path.Combine(Path.GetDirectoryName(Project.FullName), projectItemFileName);
        }

        public static string GetAbsoluteTestPath(string relativePath, string projectUniqueName)
        {
            Project project = DTE.Solution.Projects.Item(projectUniqueName);
            String path = project.FullName.Remove(project.FullName.Length - projectUniqueName.Length);

            string testFilePath = String.Concat(path, relativePath);
            return testFilePath;
        }

        public static void OpenAcceptanceTest(string relativePath, string projectUniqueName)
        {
            String absPath = GetAbsoluteTestPath(relativePath, projectUniqueName);

            if (File.Exists(absPath))
            {
                OpenDocument(new FileInfo(absPath), EnvDTE.Constants.vsViewKindDesigner);
            }
            else
            {
                Console.WriteLine("APLDRessources.ErrorOpenTestFile, \"Error\", MessageBoxButtons.OK, MessageBoxIcon.Error");
            }

        }

        public static void OpenDocument(FileInfo docPath, string viewKind)
        {
            if (docPath.Exists)
            {
                DTE.ItemOperations.OpenFile(docPath.FullName, viewKind);
            }
        }
    }
}
