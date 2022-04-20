using EnvDTE;

namespace KLCodeNav
{
    public static class TextDocumentExtensions
    {
        internal static CodeLanguage GetCodeLanguage(this TextDocument document)
        {
            return CodeLanguageHelper.GetCodeLanguage(document.Language);
        }

        internal static EditPoint GetEditPointAtCursor(this TextDocument textDocument)
        {
            var cursor = textDocument.CreateEditPoint();
            cursor.MoveToPoint(textDocument.Selection.ActivePoint);

            return cursor;
        }
    }
}
