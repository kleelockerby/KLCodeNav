using EnvDTE;

namespace KLCodeNav
{
    internal static class EditPointExtensions
    {
        internal static CodeLanguage GetCodeLanguage(this EditPoint editPoint)
        {
            return editPoint.Parent.GetCodeLanguage();
        }

        internal static string GetLine(this EditPoint editPoint)
        {
            return editPoint.GetLines(editPoint.Line, editPoint.Line + 1);
        }
    }
}