using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.ComponentModelHost;
using EnvDTE;

namespace KLCodeNav
{
    [Guid(WinformsToolWindow.ToolWindowId)]
    public class WinformsToolWindow : ToolWindowPane
    {
        internal const string ToolWindowId = "f8bc7f91-375b-4de1-9a16-a63f9bb5d47a";
        private readonly WinformsToolWindowControl control;

        //private CodeModelManager codeModelManager;

        public WinformsToolWindow() : base(null)
        {
            this.Caption = "Winforms ToolWindow";
            BitmapImageMoniker = new ImageMoniker { Guid = PackageGuids.guidImages, Id = 2 };

            using (DpiAwareness.EnterDpiScope(DpiAwarenessContext.SystemAware))
            
            using (this.control = new WinformsToolWindowControl())
            {
                this.control = new WinformsToolWindowControl();
            }
        }

        public override void OnToolWindowCreated()
        {
            KLCodeNavPackage klCodeNavPackage = Package as KLCodeNavPackage;
            if ((klCodeNavPackage.DTE != null) && (klCodeNavPackage.DTE2 != null))
            {              
                //control.Dte = klCodeNavPackage.DTE;
                //control.CreateProjectItems();

                //control.Dte2 = klCodeNavPackage.DTE2;
                //control.ComponentModel = klCodeNavPackage.ComponentModel;

                control.ActiveDocument = klCodeNavPackage.ActiveDocument;
                control.GetCodeItems();
            }
        }

        public override IWin32Window Window
        {
            get { return (IWin32Window)control; }
        }
    }
}
