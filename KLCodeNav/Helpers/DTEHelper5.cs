using System;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace KLCodeNav
{
    public static class DTEHelper5
    {
        public const string TemplateDirectory = "Templates";

        public static DTE GetDTEInstance()
        {
            var dte = (DTE)Package.GetGlobalService(typeof(DTE));
            return dte;
        }

        public static Project GetActiveProject()
        {
            Project project = null;

            var dte = GetDTEInstance();
            if (dte != null)
            {
                var activeProjects = (Array)dte.ActiveSolutionProjects;
                if (activeProjects != null && activeProjects.Length > 0)
                {
                    project = activeProjects.GetValue(0) as Project;
                }
            }
            return project;
        }

        public static ProjectItem GetTemplatesFolder()
        {
            ProjectItem templatesFolder = null;

            var project = GetActiveProject();
            if (project != null)
            {
                foreach (var item in project.ProjectItems)
                {
                    var folder = item as ProjectItem;
                    if (folder != null)
                    {
                        if (TemplateDirectory.Equals(folder.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            templatesFolder = folder;
                            break;
                        }
                    }
                }
            }
            return templatesFolder;
        }

        public static SelectedItem GetSelectedItem()
        {
            DTE dte = GetDTEInstance();

            SelectedItems items = dte.SelectedItems;

            if (items != null && items.Count == 1)
            {
                SelectedItem item = items.Item(1);
                return item;
            }
            return null;
        }

        public static bool IsFolder(this ProjectItem item)
        {
            return item.Kind == Constants.vsProjectItemKindPhysicalFolder
                || item.Kind == Constants.vsProjectItemKindVirtualFolder;
        }

        public static bool IsFile(this ProjectItem item)
        {
            return item.Kind == Constants.vsProjectItemKindPhysicalFile;
        }
    }
}
