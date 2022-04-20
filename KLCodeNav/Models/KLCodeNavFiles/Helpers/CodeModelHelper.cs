using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;

namespace KLCodeNav
{
    public class CodeModelHelper
    {
        private readonly KLCodeNavPackage package; 
        private string RegionPattern => @"^[ \t]*#([Rr]egion|endregion|End Region)";
        private static CodeModelHelper instance;

        private CodeModelHelper(KLCodeNavPackage Package)
        {
            package = Package;
        }

        internal static CodeModelHelper GetInstance(KLCodeNavPackage package)
        {
            return instance ?? (instance = new CodeModelHelper(package));
        }

        internal IEnumerable<CodeItemRegion> RetrieveCodeRegions(TextDocument textDocument)
        {
            IEnumerable<EditPoint> editPoints = TextDocumentHelper.FindMatches(textDocument, RegionPattern);
            return RetrieveCodeRegions(editPoints);
        }

        private IEnumerable<CodeItemRegion> RetrieveCodeRegions(IEnumerable<EditPoint> editPoints)
        {
            throw new NotImplementedException();
        }

        internal IEnumerable<CodeItemRegion> RetrieveCodeRegions(TextSelection textSelection)
        {
            IEnumerable<EditPoint> editPoints = TextDocumentHelper.FindMatches(textSelection, RegionPattern);
            return RetrieveCodeRegions(editPoints);
        }

    }
}
