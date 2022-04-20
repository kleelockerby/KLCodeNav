using EnvDTE;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace KLCodeNav
{
    internal static class CodeCommentHelper
    {
        public const int CopyrightExtraIndent = 4;
        public const char KeepTogetherSpacer = '\a';
        public const char Spacer = ' ';

        internal static string FakeToSpace(string value)
        {
            return value.Replace(KeepTogetherSpacer, Spacer);
        }

        internal static string GetCommentPrefix(TextDocument document)
        {
            return GetCommentPrefixForLanguage(document.GetCodeLanguage());
        }

        internal static string GetCommentPrefixForLanguage(CodeLanguage codeLanguage)
        {
            switch (codeLanguage)
            {
                case CodeLanguage.CPlusPlus:
                case CodeLanguage.CSharp:
                case CodeLanguage.CSS:
                case CodeLanguage.FSharp:
                case CodeLanguage.JavaScript:
                case CodeLanguage.LESS:
                case CodeLanguage.PHP:
                case CodeLanguage.SCSS:
                case CodeLanguage.TypeScript:
                    return "///?";

                case CodeLanguage.PowerShell:
                case CodeLanguage.R:
                    return "#+";

                case CodeLanguage.VisualBasic:
                    return "'+";

                default:
                    return null;
            }
        }

        internal static Regex GetCommentRegex(CodeLanguage codeLanguage, bool includePrefix = true)
        {
            string prefix = null;
            if (includePrefix)
            {
                prefix = GetCommentPrefixForLanguage(codeLanguage);
                if (prefix == null)
                {
                    Debug.Fail("Attempting to create a comment regex for a document that has no comment prefix specified.");
                }

                prefix = string.Format(@"(?<prefix>[\t ]*{0})(?<initialspacer>( |\t|\r|\n|$))?", prefix);
            }

            var pattern = string.Format(@"^{0}(?<indent>[\t ]*)(?<line>(?<listprefix>[-=\*\+]+[ \t]*|\w+[\):][ \t]+|\d+\.[ \t]+)?((?<words>[^\t\r\n ]+)*[\t ]*)*)\r*\n?$", prefix);
            return new Regex(pattern, RegexOptions.ExplicitCapture | RegexOptions.Multiline);
        }

        public static IEnumerable<string> GetTaskListTokens(KLCodeNavPackage package)
        {
            var settings = package.DTE.Properties["Environment", "TaskList"];
            var tokens = settings.Item("CommentTokens").Value as string[];
            if (tokens == null || tokens.Length < 1)
                return Enumerable.Empty<string>();

            return tokens.Select(t => t.Substring(0, t.LastIndexOf(':') + 1) + " ");
        }

        internal static bool IsCommentLine(EditPoint point)
        {
            return LineMatchesRegex(point, GetCommentRegex(point.GetCodeLanguage())).Success;
        }

        internal static Match LineMatchesRegex(EditPoint point, Regex regex)
        {
            var line = point.GetLine();
            var match = regex.Match(line);
            return match;
        }

        internal static string SpaceToFake(string value)
        {
            return value.Replace(Spacer, KeepTogetherSpacer);
        }
    }
}
