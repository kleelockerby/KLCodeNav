using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.PlatformUI;

namespace KLCodeNav
{
    public partial class WinformsToolWindowControl : UserControl
    {
        public DTE Dte { get; set; }

        public WinformsToolWindowControl()
        {
            InitializeComponent();
        }

        public void CreateProjectItems()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            EnvDTE.Solution sln = Dte.Solution;

            foreach (EnvDTE.Project project in sln.Projects)
            {
                lbProjects.Items.Add($"Projct Name: {project.Name}");

                ProjectItems projectItems = project.ProjectItems;
                ScanProjectItems(projectItems, 1);
            }
        }

        private void ScanProjectItems(ProjectItems projectItems, int level)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (ProjectItem projectItem in projectItems)
            {
                string itemName = "Name: " + projectItem.Name + " " + level.ToString();
                lbProjects.Items.Add($"\t {itemName}");

                ProjectItems projectItems2 = projectItem.ProjectItems;
                if (projectItems2 != null)
                {
                    ScanProjectItems(projectItems2, level++);
                }
            }
        }
    }
}
