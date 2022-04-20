using EnvDTE;
using System.Linq;

namespace KLCodeNav
{
    public class CodeModelBuilder
    {
        private readonly KLCodeNavPackage package;
        private readonly CodeModelHelper codeModelHelper;
        private static CodeModelBuilder instance;

        private CodeModelBuilder(KLCodeNavPackage Package)
        {
            package = Package;
            codeModelHelper = CodeModelHelper.GetInstance(package);
        }

        internal static CodeModelBuilder GetInstance(KLCodeNavPackage package)
        {
            return instance ?? (instance = new CodeModelBuilder(package));
        }

        internal SetCodeItems RetrieveAllCodeItems(Document document)
        {
            var codeItems = new SetCodeItems();

            var fileCodeModel = RetrieveFileCodeModel(document.ProjectItem);
            RetrieveCodeItems(codeItems, fileCodeModel);

            codeItems.AddRange(codeModelHelper.RetrieveCodeRegions(document.GetTextDocument()));

            return codeItems;
        }

        private FileCodeModel RetrieveFileCodeModel(ProjectItem projectItem)
        {
            if (projectItem == null)
            {
                return null;
            }

            if (projectItem.FileCodeModel != null)
            {
                return projectItem.FileCodeModel;
            }

            //const string sharedProjectTypeGUID = "{d954291e-2a0b-460d-934e-dc6b0785db48}";
            /*var containingProject = projectItem.ContainingProject;

            if (containingProject != null && containingProject.Kind != null &&
                containingProject.Kind.ToLowerInvariant() == sharedProjectTypeGUID)
            {
                var similarProjectItems = SolutionHelper.GetSimilarProjectItems(package, projectItem);
                var fileCodeModel = similarProjectItems.Select(x => x.FileCodeModel).FirstOrDefault(y => y != null);

                return fileCodeModel;
            }*/

            return null;
        }

        private static void RetrieveCodeItems(SetCodeItems codeItems, FileCodeModel fcm)
        {
            if (fcm != null && fcm.CodeElements != null)
            {
                RetrieveCodeItemsFromElements(codeItems, fcm.CodeElements);
            }
        }

        private static void RetrieveCodeItemsFromElements(SetCodeItems codeItems, CodeElements codeElements)
        {
            foreach (CodeElement child in codeElements)
            {
                RetrieveCodeItemsRecursively(codeItems, child);
            }
        }

        private static void RetrieveCodeItemsRecursively(SetCodeItems codeItems, CodeElement codeElement)
        {
            var parentCodeItem = FactoryCodeItems.CreateCodeItemElement(codeElement);
            if (parentCodeItem != null)
            {
                codeItems.Add(parentCodeItem);
            }

            if (codeElement.Children != null)
            {
                RetrieveCodeItemsFromElements(codeItems, codeElement.Children);
            }
        }

    }
}
