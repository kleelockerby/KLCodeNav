using EnvDTE;

namespace KLCodeNav
{
    internal class SnapshotCodeItems
    {
        internal SnapshotCodeItems(Document document, SetCodeItems codeItems)
        {
            Document = document;
            CodeItems = codeItems;
        }

        internal Document Document { get; private set; }

        internal SetCodeItems CodeItems { get; private set; }

    }
}