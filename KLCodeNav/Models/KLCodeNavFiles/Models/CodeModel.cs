using EnvDTE;
using System.Threading;

namespace KLCodeNav
{
    public class CodeModel
    {
        private bool isBuilding;
        private bool isStale;

        internal CodeModel(Document document)
        {
            CodeItems = new SetCodeItems();
            Document = document;
            IsBuiltWaitHandle = new ManualResetEvent(false);
        }

        internal Document Document { get; }

        internal SetCodeItems CodeItems { get; set; }

        internal bool IsBuilding
        {
            get { return isBuilding; }
            set
            {
                if (isBuilding != value)
                {
                    //OutputWindowHelper.DiagnosticWriteLine($"CodeModel.IsBuilding changing to '{value}' for '{Document.FullName}'");

                    isBuilding = value;
                    if (isBuilding)
                    {
                        IsBuiltWaitHandle.Reset();
                    }
                    else
                    {
                        IsBuiltWaitHandle.Set();
                    }
                }
            }
        }

        internal ManualResetEvent IsBuiltWaitHandle { get; }

        internal bool IsStale
        {
            get { return isStale; }
            set
            {
                if (isStale != value)
                {
                    //OutputWindowHelper.DiagnosticWriteLine($"CodeModel.IsStale changing to '{value}' for '{Document.FullName}'");
                    isStale = value;
                }
            }
        }

    }
}
