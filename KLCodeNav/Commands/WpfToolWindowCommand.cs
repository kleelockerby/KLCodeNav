using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace KLCodeNav
{
    internal sealed class WpfToolWindowCommand
    {
        private readonly AsyncPackage package;

        private WpfToolWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(PackageGuids.KLCodeNavCmdSet, PackageIds.WpfToolWindowCommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static WpfToolWindowCommand Instance { get; private set; }
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new WpfToolWindowCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            this.package.JoinableTaskFactory.RunAsync(async delegate
            {
                ToolWindowPane window = await this.package.ShowToolWindowAsync(typeof(WpfToolWindow), 0, true, this.package.DisposalToken);
                if ((null == window) || (null == window.Frame))
                {
                    throw new NotSupportedException("Cannot create tool window");
                }
            });
        }
    }
}
