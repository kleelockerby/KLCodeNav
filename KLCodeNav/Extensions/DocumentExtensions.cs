using EnvDTE;

namespace KLCodeNav
{
    internal static class DocumentExtensions
    {
        internal static CodeLanguage GetCodeLanguage(this Document document)
        {
            return CodeLanguageHelper.GetCodeLanguage(document.Language);
        }

        internal static TextDocument GetTextDocument(this Document document)
        {
            return document.Object("TextDocument") as TextDocument;
        }

        internal static bool IsExternal(this Document document)
        {
            var projectItem = document.ProjectItem;

            return projectItem == null || projectItem.IsExternal();
        }
    }
}
