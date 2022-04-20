using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace KLCodeNav
{
    public partial class WinformsToolWindowControl : UserControl
    {
        public DTE Dte { get; set; }
        public DTE2 Dte2 { get; set; }
        public IComponentModel ComponentModel { get; set; }
        public Document ActiveDocument { get; set; }

        public WinformsToolWindowControl()
        {
            InitializeComponent();
        }

        public void CreateAll()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            GetDocument();
            CreateProjectItems();
        }

        public void GetDocument()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string documentName = Dte2.ActiveDocument.FullName;
            lblDocumentName.Text = documentName;

            IVsTextView viewAdapter = GetCurrentNativeTextView();
   

            IWpfTextView wpfView = GetCurentTextView();

        }


        public IWpfTextView GetCurentTextView()
        {
            IComponentModel componentModel = this.ComponentModel;
            if (componentModel == null)
            {
                return null;
            }

            IVsEditorAdaptersFactoryService editorAdapter = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            return editorAdapter.GetWpfTextView(GetCurrentNativeTextView());
        }

        public static IVsTextView GetCurrentNativeTextView()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var textManager = (IVsTextManager)ServiceProvider.GlobalProvider.GetService(typeof(SVsTextManager));
            Assumes.Present(textManager);

            ErrorHandler.ThrowOnFailure(textManager.GetActiveView(1, null, out IVsTextView activeView));
            return activeView;
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
