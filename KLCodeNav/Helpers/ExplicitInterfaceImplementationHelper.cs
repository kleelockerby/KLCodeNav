using EnvDTE;
using EnvDTE80;

namespace KLCodeNav
{
    public static class ExplicitInterfaceImplementationHelper
    {
        public static bool IsExplicitInterfaceImplementation(CodeEvent codeEvent)
        {
            if (codeEvent.Name.Contains("."))
            {
                return true;
            }

            var declaration = CodeElementHelper.GetEventDeclaration(codeEvent);
            var matchString = @"\." + codeEvent.Name;

            return RegexNullSafe.IsMatch(declaration, matchString);
        }

        public static bool IsExplicitInterfaceImplementation(CodeFunction2 codeFunction)
        {
            if (codeFunction.Name.Contains("."))
            {
                return true;
            }

            var declaration = CodeElementHelper.GetMethodDeclaration(codeFunction);
            var matchString = @"\." + codeFunction.Name;

            return RegexNullSafe.IsMatch(declaration, matchString);
        }

        public static bool IsExplicitInterfaceImplementation(CodeProperty codeProperty)
        {
            if (codeProperty.Name.Contains("."))
            {
                return true;
            }

            var declaration = CodeElementHelper.GetPropertyDeclaration(codeProperty);
            var matchString = @"\." + codeProperty.Name;

            return RegexNullSafe.IsMatch(declaration, matchString);
        }
    }
}
