using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ComponentModelHost;

namespace KLCodeNav
{
    [Guid(WinformsToolWindow.ToolWindowId)]
    public class WinformsToolWindow : ToolWindowPane
    {
        internal const string ToolWindowId = "f8bc7f91-375b-4de1-9a16-a63f9bb5d47a";
        private readonly WinformsToolWindowControl control;

        public WinformsToolWindow() : base(null)
        {
            this.Caption = "Winforms ToolWindow";
            BitmapImageMoniker = new ImageMoniker { Guid = PackageGuids.guidImages, Id = 2 };
            this.control = new WinformsToolWindowControl();
        }

        public override void OnToolWindowCreated()
        {
            KLCodeNavPackage klCodeNavPackage = Package as KLCodeNavPackage;
            if ((klCodeNavPackage.DTE != null) && (klCodeNavPackage.DTE2 != null))
            {
                control.Dte = klCodeNavPackage.DTE;
                control.Dte2 = klCodeNavPackage.DTE2;
                control.ComponentModel = klCodeNavPackage.ComponentModel;
                control.ActiveDocument = klCodeNavPackage.ActiveDocument;

                control.CreateAll();
                //control.CreateProjectItems();
            }
        }

        public override IWin32Window Window
        {
            get { return (IWin32Window)control; }
        }
    }
}
