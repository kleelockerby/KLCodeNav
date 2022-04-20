using System.Diagnostics;
using EnvDTE;
using EnvDTE80;
using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KLCodeNav
{
    public static class ProjectHelper
    {
        public static DTE2 DTE { get; }

        static ProjectHelper()
        {
            DTE = (DTE2)Package.GetGlobalService(typeof(DTE));
        }

        public static void AddFileToProject(this Project project, string file)
        {
            if (project.IsKind(ProjectTypes.ASPNET_5, ProjectTypes.SSDT, ProjectTypes.MISC, ProjectTypes.SOLUTION_FOLDER))
                return;

            if (DTE.Solution.FindProjectItem(file) == null)
            {
                ProjectItem item = project.ProjectItems.AddFromFile(file);
            }
        }

        public static void AddNestedFile(string parentFile, string newFile, bool force = false)
        {
            ProjectItem item = DTE.Solution.FindProjectItem(parentFile);

            try
            {
                if (item == null || item.ContainingProject == null || item.ContainingProject.IsKind(ProjectTypes.ASPNET_5))
                {
                    return;
                }

                if (item.ProjectItems == null || item.ContainingProject.IsKind(ProjectTypes.UNIVERSAL_APP))
                {
                    item.ContainingProject.AddFileToProject(newFile);
                }
                else if (DTE.Solution.FindProjectItem(newFile) == null || force)
                {
                    item.ProjectItems.AddFromFile(newFile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static string CreateNewFileBesideExistingFile(string newFileName, string existingFileFullPath)
        {
            try
            {
                string fileDir = Path.GetDirectoryName(existingFileFullPath);
                string newFilePath = Path.GetFullPath(Path.Combine(fileDir, newFileName));
                Directory.CreateDirectory(Path.GetDirectoryName(newFilePath));
                File.WriteAllBytes(newFilePath, new byte[0]);

                Project project = DTE.Solution?.FindProjectItem(existingFileFullPath)?.ContainingProject;
                if (project == null)
                    return null;
                if (project.IsKind(ProjectTypes.SOLUTION_FOLDER))
                {
                    string relativePath = Path.GetDirectoryName(newFileName);
                    var newFolder = project.AsSolutionFolder()
                                      .AddNestedSolutionFolder(relativePath);
                    newFolder.ProjectItems.AddFromFile(newFilePath);
                }
                else
                {
                    project.AddFileToProject(newFilePath);
                }
                return newFilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        public static Project AddNestedSolutionFolder(this SolutionFolder solutionFolder, string path)
        {
            if (string.IsNullOrEmpty(path))
                return solutionFolder.Parent;
            var pathParts = path.Split(new[] { '\\', '/' }, 2);
            string solutioFolderName = pathParts[0];
            Project folder = solutionFolder.GetCreateSolutionFolder(solutioFolderName);
            if (pathParts.Length <= 1)
                return folder;
            string pathRecedure = pathParts[1];
            return AddNestedSolutionFolder(folder.AsSolutionFolder(), pathRecedure);
        }

        private static Project GetCreateSolutionFolder(this SolutionFolder solutionFolder, string folderName)
        {
            var projectItems = solutionFolder.Parent.ProjectItems.OfType<ProjectItem>();
            var existingProjectItem = projectItems.FirstOrDefault(item => item.Name == folderName);
            if (existingProjectItem != null)
                return (Project)existingProjectItem.Object;
            return solutionFolder.AddSolutionFolder(folderName);
        }

        public static SolutionFolder AsSolutionFolder(this Project project)
        {
            return (SolutionFolder)project.Object;
        }

        public static bool DeleteFileFromProject(string file)
        {
            ProjectItem item = DTE.Solution.FindProjectItem(file);

            if (item == null)
                return false;

            try
            {
                item.Delete();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        public static bool IsKind(this Project project, params string[] kindGuids)
        {
            foreach (var guid in kindGuids)
            {
                if (project.Kind.Equals(guid, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static bool IsKind(this ProjectItem projectItem, params string[] kindGuids)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            foreach (var guid in kindGuids)
            {
                if (projectItem.Kind.Equals(guid, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static void OpenFileInPreviewTab(string file)
        {
            IVsNewDocumentStateContext newDocumentStateContext = null;
            bool failedToOpen = false;

            try
            {
                var openDoc3 = Package.GetGlobalService(typeof(SVsUIShellOpenDocument)) as IVsUIShellOpenDocument3;

                Guid reason = VSConstants.NewDocumentStateReason.Navigation;
                newDocumentStateContext = openDoc3.SetNewDocumentState((uint)__VSNEWDOCUMENTSTATE.NDS_Provisional, ref reason);

                DTE.ItemOperations.OpenFile(file);
            }
            catch (COMException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                failedToOpen = true;
            }
            finally
            {
                if (newDocumentStateContext != null)
                    newDocumentStateContext.Restore();

                if (failedToOpen)
                    DTE.ItemOperations.OpenFile(file);
            }
        }

        public static object GetSelectedItem()
        {
            IntPtr hierarchyPointer, selectionContainerPointer;
            object selectedObject = null;
            IVsMultiItemSelect multiItemSelect;
            uint itemId;

            var monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));

            try
            {
                monitorSelection.GetCurrentSelection(out hierarchyPointer,
                                                 out itemId,
                                                 out multiItemSelect,
                                                 out selectionContainerPointer);

                IVsHierarchy selectedHierarchy = Marshal.GetTypedObjectForIUnknown(
                                                     hierarchyPointer,
                                                     typeof(IVsHierarchy)) as IVsHierarchy;

                if (selectedHierarchy != null)
                {
                    ErrorHandler.ThrowOnFailure(selectedHierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_ExtObject, out selectedObject));
                }

                Marshal.Release(hierarchyPointer);
                Marshal.Release(selectionContainerPointer);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);
            }

            return selectedObject;
        }

        public static async Task<(string path, string text)> GetDocumentTextAsync(this ProjectItem projectItem, string solutionDirectory)
        {
            if (projectItem == null) return (string.Empty, string.Empty);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            string path = await GetFullPathAsync(projectItem.Properties);
            if (string.IsNullOrWhiteSpace(path))
            {
                path = await GetFullPathAsync(projectItem.ContainingProject?.Properties);

                if (string.IsNullOrWhiteSpace(path))
                {
                    path = Path.Combine(solutionDirectory, projectItem.Name);
                }
                else
                {
                    path = Path.Combine(path, projectItem.Name);
                }
            }

            return (path, File.ReadAllText(path));
        }

        public static async System.Threading.Tasks.Task OpenDocumentForProjectItemAsync(this ProjectItem originalProjectItem)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var window = originalProjectItem.Open();
            window.Visible = true;
        }

        private async static Task<string> GetFullPathAsync(Properties properties)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                return properties?.Item("FullPath")?.Value?.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        public static async Task<Project> GetProjectByNameAsync(this Solution solution, string name)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var sln = Microsoft.Build.Construction.SolutionFile.Parse(solution.FullName);

            foreach (Project p in solution.Projects)
            {
                if (p.Name == name)
                {
                    return p;
                }
            }

            return null;
        }

        public static async Task<ProjectItem> GetProjectItemByNameAsync(this ProjectItems projectItems, string name, CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            foreach (ProjectItem item in projectItems)
            {
                if (item.Name == name) return item;
            }

            return null;
        }
    }
}
