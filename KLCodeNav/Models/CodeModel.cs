using EnvDTE;
using System.Threading;
namespace KLCodeNav
{
    public class CodeModel
    {
        private bool isBuilding;
        private bool isStale;
        
        internal Document Document { get; }
        internal SetCodeItems CodeItems { get; set; }
        internal ManualResetEvent IsBuiltWaitHandle { get; }

        internal CodeModel(Document document)
        {
            CodeItems = new SetCodeItems();
            Document = document;
            IsBuiltWaitHandle = new ManualResetEvent(false);
        }

        internal bool IsBuilding
        {
            get { return isBuilding; }
            set
            {
                if (isBuilding != value)
                {
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

        internal bool IsStale
        {
            get { return isStale; }
            set
            {
                if (isStale != value)
                {
                    isStale = value;
                }
            }
        }
    }
}
