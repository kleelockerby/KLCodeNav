using EnvDTE;
using System;

namespace KLCodeNav
{
    public static class RegionHelper
    {
        internal static string GetRegionName(EditPoint editPoint, string regionText)
        {
            var codeLanguage = editPoint.GetCodeLanguage();
            switch (codeLanguage)
            {
                case CodeLanguage.CSharp:
                    return regionText.Substring(8).Trim();

                case CodeLanguage.VisualBasic:
                    var text = regionText.Substring(8).Trim();
                    text = text.Substring(1, text.Length - 2);
                    return text;

                default:
                    throw new NotImplementedException($"Regions are not supported for '{codeLanguage}'.");
            }
        }

        internal static string GetRegionTagText(EditPoint editPoint, string name = null)
        {
            var codeLanguage = editPoint.GetCodeLanguage();
            switch (codeLanguage)
            {
                case CodeLanguage.CSharp:
                    return "#region " +
                           (name ?? string.Empty);

                case CodeLanguage.VisualBasic:
                    return "#Region " +
                           (name != null ? $"\"{name}\"" : string.Empty);

                default:
                    throw new NotImplementedException($"Regions are not supported for '{codeLanguage}'.");
            }
        }

        internal static string GetEndRegionTagText(EditPoint editPoint)
        {
            var codeLanguage = editPoint.GetCodeLanguage();
            switch (codeLanguage)
            {
                case CodeLanguage.CSharp:
                    return "#endregion";

                case CodeLanguage.VisualBasic:
                    return "#End Region";

                default:
                    throw new NotImplementedException($"Regions are not supported for '{codeLanguage}'.");
            }
        }

        internal static bool LanguageSupportsUpdatingEndRegionDirectives(EditPoint editPoint)
        {
            var codeLanguage = editPoint.GetCodeLanguage();

            switch (codeLanguage)
            {
                case CodeLanguage.CSharp:
                    return true;

                default:
                    return false;
            }
        }

    }
}
