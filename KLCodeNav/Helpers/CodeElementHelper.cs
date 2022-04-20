using EnvDTE;
using EnvDTE80;
using System;
using System.Text.RegularExpressions;

namespace KLCodeNav
{
    internal static class CodeElementHelper
    {
        internal static int CalculateComplexity(CodeElement element)
        {
            EditPoint startPoint = element.StartPoint.CreateEditPoint();
            string functionText = startPoint.GetText(element.EndPoint);

            functionText = Regex.Replace(functionText, @"//.*" + Environment.NewLine, Environment.NewLine);

            functionText = Regex.Replace(functionText, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

            functionText = Regex.Replace(functionText, @"""[^""]*""", string.Empty);

            functionText = Regex.Replace(functionText, @"'[^']*'", string.Empty);

            int ifCount = Regex.Matches(functionText, @"\sif[\s\(]").Count;
            int elseCount = Regex.Matches(functionText, @"\selse\s").Count;
            int elseIfCount = Regex.Matches(functionText, @"\selse if[\s\(]").Count;
            int whileCount = Regex.Matches(functionText, @"\swhile[\s\(]").Count;
            int forCount = Regex.Matches(functionText, @"\sfor[\s\(]").Count;
            int forEachCount = Regex.Matches(functionText, @"\sforeach[\s\(]").Count;
            int switchCount = Regex.Matches(functionText, @"\sswitch[\s\(]").Count;
            int caseCount = Regex.Matches(functionText, @"\scase\s[^;]*;").Count;
            int catchCount = Regex.Matches(functionText, @"\scatch[\s\(]").Count;
            int tertiaryCount = Regex.Matches(functionText, @"\s\?\s").Count;
            int andCount = Regex.Matches(functionText, @"\&\&").Count;
            int orCount = Regex.Matches(functionText, @"\|\|").Count;

            int complexity = 1 +
                             ifCount + elseCount - elseIfCount +             
                             whileCount + forCount + forEachCount + switchCount + caseCount +
                             catchCount + tertiaryCount + andCount + orCount;

            return complexity;
        }

        internal static string GetAccessModifierKeyword(vsCMAccess accessModifier)
        {
            switch (accessModifier)
            {
                case vsCMAccess.vsCMAccessPublic: return "public";
                case vsCMAccess.vsCMAccessProtected: return "protected";
                case vsCMAccess.vsCMAccessProject: return "internal";
                case vsCMAccess.vsCMAccessProjectOrProtected: return "protected internal";
                case vsCMAccess.vsCMAccessPrivate: return "private";
                default: return null;
            }
        }

        internal static string GetClassDeclaration(CodeClass codeClass)
        {
            var startPoint = codeClass.GetStartPoint(vsCMPart.vsCMPartHeader);

            return TextDocumentHelper.GetTextToFirstMatch(startPoint, @"\{");
        }

        internal static string GetDelegateDeclaration(CodeDelegate codeDelegate)
        {
            var startPoint = codeDelegate.Attributes.Count > 0
                ? codeDelegate.GetEndPoint(vsCMPart.vsCMPartAttributesWithDelimiter)
                : codeDelegate.StartPoint;

            return TextDocumentHelper.GetTextToFirstMatch(startPoint, @";");
        }

        internal static string GetEnumerationDeclaration(CodeEnum codeEnum)
        {
            var startPoint = codeEnum.GetStartPoint(vsCMPart.vsCMPartHeader);

            return TextDocumentHelper.GetTextToFirstMatch(startPoint, @"\{");
        }

        internal static string GetEventDeclaration(CodeEvent codeEvent)
        {
            var startPoint = codeEvent.Attributes.Count > 0
                ? codeEvent.GetEndPoint(vsCMPart.vsCMPartAttributesWithDelimiter)
                : codeEvent.StartPoint;

            return TextDocumentHelper.GetTextToFirstMatch(startPoint, @"[\{;]");
        }

        internal static string GetFieldDeclaration(CodeVariable codeField)
        {
            var startPoint = codeField.Attributes.Count > 0
                ? codeField.GetEndPoint(vsCMPart.vsCMPartAttributesWithDelimiter)
                : codeField.StartPoint;

            return TextDocumentHelper.GetTextToFirstMatch(startPoint, @"[,;]");
        }

        internal static string GetInterfaceDeclaration(CodeInterface codeInterface)
        {
            var startPoint = codeInterface.GetStartPoint(vsCMPart.vsCMPartHeader);

            return TextDocumentHelper.GetTextToFirstMatch(startPoint, @"\{");
        }

        internal static string GetMethodDeclaration(CodeFunction codeFunction)
        {
            var startPoint = codeFunction.GetStartPoint(vsCMPart.vsCMPartHeader);

            return TextDocumentHelper.GetTextToFirstMatch(startPoint, @"[\(\{;]");
        }

        internal static string GetPropertyDeclaration(CodeProperty codeProperty)
        {
            var startPoint = codeProperty.Attributes.Count > 0
                ? codeProperty.GetEndPoint(vsCMPart.vsCMPartAttributesWithDelimiter)
                : codeProperty.StartPoint;

            return TextDocumentHelper.GetTextToFirstMatch(startPoint, @"\{");
        }

        internal static string GetStructDeclaration(CodeStruct codeStruct)
        {
            var startPoint = codeStruct.GetStartPoint(vsCMPart.vsCMPartHeader);

            return TextDocumentHelper.GetTextToFirstMatch(startPoint, @"\{");
        }
    }
}
