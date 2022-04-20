using EnvDTE;
using System;
using System.Threading.Tasks;

namespace KLCodeNav
{
    public class CodeModelManager
    {
        private readonly KLCodeNavPackage package;
        //private readonly CodeModelBuilder codeModelBuilder;
        private static CodeModelManager instance;

        private CodeModelManager(KLCodeNavPackage Package)
        {
            package = Package;
            //codeModelBuilder = CodeModelBuilder.GetInstance(package);
        }

        internal static CodeModelManager GetInstance(KLCodeNavPackage package)
        {
            return instance ?? (instance = new CodeModelManager(package));
        }

        /*internal SetCodeItems RetrieveAllCodeItems(Document document, bool loadLazyInitializedValues = false)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }


            var codeModel = _codeModelCache.GetCodeModel(document);
            if (codeModel.IsBuilding)
            {
                if (!codeModel.IsBuiltWaitHandle.WaitOne(TimeSpan.FromSeconds(3)))
                {
                    return null;
                }
            }
            else if (codeModel.IsStale)
            {
                BuildCodeItems(codeModel);

                if (loadLazyInitializedValues)
                {
                    LoadLazyInitializedValues(codeModel);
                }
            }

            return codeModel.CodeItems;
        }*/
    }
}
