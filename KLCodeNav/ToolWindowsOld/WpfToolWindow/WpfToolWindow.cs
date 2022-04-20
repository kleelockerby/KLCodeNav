using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Imaging.Interop;

namespace KLCodeNav
{
    [Guid(WpfToolWindow.ToolWindowId)]
    public class WpfToolWindow : ToolWindowPane
    {
        internal const string ToolWindowId = "7699f388-b29f-4002-a17e-2433ee20ffaf";
        private readonly WpfToolWindowControl control;

        public WpfToolWindow() : base(null)
        {
            this.Caption = "WpfToolWindow";
            BitmapImageMoniker = new ImageMoniker { Guid = PackageGuids.guidImages, Id = 1 };
            this.control = new WpfToolWindowControl();
            this.Content = control;
        }

        public override void OnToolWindowCreated()
        {
            KLCodeNavPackage klCodeNavPackage = Package as KLCodeNavPackage;
            if (klCodeNavPackage.DTE != null)
            {
                control.CreateProjectItems(klCodeNavPackage.DTE);
            }

        }
    }
}
