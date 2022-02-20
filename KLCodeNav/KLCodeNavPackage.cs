using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;

namespace KLCodeNav
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuids.KLCodeNavString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(WpfToolWindow), MultiInstances = false, Style = VsDockStyle.Tabbed, Orientation = ToolWindowOrientation.Left, Window = EnvDTE.Constants.vsWindowKindSolutionExplorer)]
    [ProvideToolWindow(typeof(WinformsToolWindow))]
    public sealed class KLCodeNavPackage : AsyncPackage
    {
        public DTE DTE;
        public DTE2 DTE2;
        public IComponentModel ComponentModel;

        public Document ActiveDocument
        {
            get
            {
                try
                {
                    return DTE2.ActiveDocument;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public KLCodeNavPackage() { }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            DTE = await GetServiceAsync(typeof(DTE)) as DTE;
            DTE2 =  (DTE2)await GetServiceAsync(typeof(DTE));

            ComponentModel = await GetServiceAsync(typeof(SComponentModel)) as IComponentModel;

            await WpfToolWindowCommand.InitializeAsync(this);
            await WinformsToolWindowCommand.InitializeAsync(this);
            await base.InitializeAsync(cancellationToken, progress);
            
        }
    }
}
