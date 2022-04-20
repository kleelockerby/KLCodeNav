
namespace KLCodeNav
{
    internal static class CodeLanguageHelper
    {
        internal static CodeLanguage GetCodeLanguage(string language)
        {
            switch (language)
            {
                case "Basic": return CodeLanguage.VisualBasic;
                case "CSharp": return CodeLanguage.CSharp;
                case "C/C++":
                case "C/C++ (VisualGDB)": return CodeLanguage.CPlusPlus;
                case "CSS": return CodeLanguage.CSS;
                case "F#": return CodeLanguage.FSharp;
                case "HTML":
                case "HTMLX": return CodeLanguage.HTML;
                case "JavaScript":
                case "JScript":
                case "Node.js": return CodeLanguage.JavaScript;
                case "JSON": return CodeLanguage.JSON;
                case "LESS": return CodeLanguage.LESS;
                case "PHP": return CodeLanguage.PHP;
                case "PowerShell": return CodeLanguage.PowerShell;
                case "R": return CodeLanguage.R;
                case "SCSS": return CodeLanguage.SCSS;
                case "TypeScript": return CodeLanguage.TypeScript;
                case "XAML": return CodeLanguage.XAML;
                case "XML": return CodeLanguage.XML;
                default: return CodeLanguage.Unknown;
            }
        }
    }
}
