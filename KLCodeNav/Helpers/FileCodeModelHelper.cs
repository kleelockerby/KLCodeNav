using System;
using System.Collections.Generic;
using System.Text;
using EnvDTE;
using System.Globalization;
using VSLangProj;

namespace KLCodeNav
{
    public static class FileCodeModelHelper
    {
        public static CodeElements GetCodeElementsForDocument(Document document)
        {
            var fileCodeModel = GetFileCodeModelForDocument(document);

            if (fileCodeModel == null)
                return null;

            CodeElements codeElements;
            var tries = 3;
            do
            {
                codeElements = fileCodeModel.CodeElements;

                if (codeElements == null && tries > 0)
                    System.Threading.Thread.Sleep(10);

                tries--;
            } while (tries > 0 && codeElements == null);

            if (codeElements == null)
                Console.WriteLine("Could not get code elements for file '{document.Name}'");

            return codeElements;
        }

        private static FileCodeModel GetFileCodeModelForDocument(Document document)
        {
            FileCodeModel fileCodeModel = null;
            var tries = 3;
            do
            {
                try
                {
                    fileCodeModel = document.ProjectItem.FileCodeModel;
                }
                catch (InvalidOperationException)
                {
                }

                if (fileCodeModel == null && tries > 0)
                {
                    System.Threading.Thread.Sleep(10);
                }

                tries--;
            } while (tries > 0 && fileCodeModel == null);

            if (fileCodeModel == null)
            {
                Console.WriteLine("Could not get file code model for file '{document.Name}'");
            }

            return fileCodeModel;
        }

        public static void AddAttribute(CodeClass element, string attributeName, string attributeValue)
        {
            if (!HasAttribute(element, attributeName))
            {
                element.AddAttribute(attributeName, attributeValue, 0);
            }
        }

        public static void AddAttribute(CodeProperty element, string attributeName, string attributeValue)
        {
            if (!HasAttribute(element, attributeName))
            {
                element.AddAttribute(attributeName, attributeValue, 0);
            }
        }

        public static bool UpdateCodeAttributeArgument(
            CodeElements attributes,
            string attributeName,
            string argumentName,
            string argumentValue)
        {
            return UpdateCodeAttributeArgument(attributes, attributeName, argumentName, argumentValue, true);
        }

        public static bool UpdateCodeAttributeArgument( CodeElements attributes, string attributeName, string argumentName, string argumentValue, bool createIfNew) {
            bool result = false;
            foreach (CodeElement attribute in attributes)
            {
                CodeAttribute codeAttribute = (CodeAttribute)attribute;
                if (attribute.FullName.Equals(attributeName, StringComparison.InvariantCultureIgnoreCase))
                {
                    result = UpdateCodeAttributeArgument(codeAttribute, argumentName, argumentValue, createIfNew);
                    break;
                }
            }

            return result;
        }

        public static bool UpdateCodeAttributeArgument( CodeAttribute codeAttribute, string argumentName, string argumentValue, bool createIfNew)
        {
            bool result = false;

            EnvDTE80.CodeAttribute2 attribute2 = (EnvDTE80.CodeAttribute2)codeAttribute;
            EnvDTE80.CodeAttributeArgument argumentMatch = null;
            foreach (EnvDTE80.CodeAttributeArgument argument in attribute2.Arguments)
            {
                if (argument.Name.Equals(argumentName, StringComparison.InvariantCultureIgnoreCase))
                {
                    argumentMatch = argument;
                    break;
                }
            }
            if (argumentMatch != null)
            {
                argumentMatch.Value = argumentValue;
                result = true;
            }
            else if (createIfNew)
            {
                attribute2.AddArgument(argumentValue, argumentName, attribute2.Arguments.Count);
                result = true;
            }

            return result;
        }

        public static bool HasAttribute(CodeClass element, string attributeName)
        {
            if (element.Attributes.Count > 0)
            {
                foreach (CodeElement att in element.Attributes)
                {
                    CodeAttribute codeAttribute = (CodeAttribute)att;

                    if (att.Name.Equals(attributeName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool HasAttribute(CodeInterface element, string attributeName)
        {
            if (element.Attributes.Count > 0)
            {
                foreach (CodeElement att in element.Attributes)
                {
                    CodeAttribute codeAttribute = (CodeAttribute)att;

                    if (att.Name.Equals(attributeName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool HasAttribute(CodeProperty element, string attributeName)
        {
            if (element.Attributes.Count > 0)
            {
                foreach (CodeElement att in element.Attributes)
                {
                    CodeAttribute codeAttribute = (CodeAttribute)att;
                    if (att.Name.Equals(attributeName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static object GetCodeElement(DTE vs, EnvDTE.FileCodeModel fileCodeModel, Type codeElementType)
        {
            TextSelection textSelection = (TextSelection)vs.ActiveDocument.Selection;
            TextPoint point = textSelection.ActivePoint;

            object element;

            if (codeElementType.IsAssignableFrom(typeof(CodeNamespace)))
            {
                try
                {
                    element = (CodeNamespace)fileCodeModel.CodeElementFromPoint( point, vsCMElement.vsCMElementNamespace);
                    return element;
                }
                catch
                {
                    return null;
                }
            }

            if (codeElementType.IsAssignableFrom(typeof(CodeAttribute)))
            {
                try
                {
                    element = (CodeAttribute)fileCodeModel.CodeElementFromPoint( point, vsCMElement.vsCMElementAttribute);
                    return element;
                }
                catch
                {
                    return null;
                }
            }

            if (codeElementType.IsAssignableFrom(typeof(CodeProperty)))
            {
                try
                {
                    element = (CodeProperty)fileCodeModel.CodeElementFromPoint( point, vsCMElement.vsCMElementProperty);
                    return element;
                }
                catch
                {
                    return null;
                }
            }

            if (codeElementType.IsAssignableFrom(typeof(CodeFunction)))
            {
                try
                {
                    element = (CodeFunction)fileCodeModel.CodeElementFromPoint( point, vsCMElement.vsCMElementFunction);
                    return element;
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        public static CodeProperty AddProperty(CodeClass codeClass, CodeVariable var)
        {
            CodeProperty prop = null;

            try
            {
                prop = codeClass.AddProperty( FormatPropertyName(var.Name), FormatPropertyName(var.Name), var.Type.AsFullName, -1, vsCMAccess.vsCMAccessPublic, null);

                EditPoint editPoint = prop.Getter.GetStartPoint(vsCMPart.vsCMPartBody).CreateEditPoint();

                editPoint.Delete(editPoint.LineLength);

                editPoint.Indent(null, 4);
                editPoint.Insert(string.Format(CultureInfo.InvariantCulture, "return {0};", var.Name));

                editPoint = prop.Setter.GetStartPoint(vsCMPart.vsCMPartBody).CreateEditPoint();

                editPoint.Indent(null, 1);
                editPoint.Insert(string.Format(CultureInfo.InvariantCulture, "{0} = value;", var.Name));
                editPoint.SmartFormat(editPoint);

                return prop;
            }
            catch
            {
                return null;
            }
        }

        public static CodeElement FindCodeElementFromType(CodeElement element, string typeName, vsCMElement elementKind)
        {
            if (element.Kind == elementKind &&
                element.FullName.Equals(typeName, StringComparison.InvariantCultureIgnoreCase))
            {
                return element;
            }

            return InspectBaseCodeElement(element, typeName, elementKind);
        }

        private static CodeElement FindCodeElementByFullName(
            Project project, string targetFullName, vsCMElement elementKind)
        {
            if (project == null)
            {
                return null;
            }

            foreach (ProjectItem projectItem in new DteHelperEx.ProjectItemIterator(project))
            {
                CodeElement element = FindCodeElementByFullName(projectItem, targetFullName, elementKind);
                if (element != null)
                {
                    return element;
                }
            }

            return null;
        }

        private static CodeElement FindCodeElementByFullName( ProjectItem projectItem, string targetFullName, vsCMElement elementKind)
        {
            foreach (CodeElement element in new DteHelperEx.CodeElementsIterator(projectItem))
            {
                if (element.Kind == vsCMElement.vsCMElementNamespace)
                {
                    foreach (CodeElement type in ((CodeNamespace)element).Members)
                    {
                        CodeElement result = InspectCodeElement(type, targetFullName, elementKind);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
                else
                {
                    CodeElement result = InspectCodeElement(element, targetFullName, elementKind);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return null;
        }

        private static CodeElement InspectCodeElement(
            CodeElement element, string targetFullName, vsCMElement elementKind)
        {
            if (element.IsCodeType)
            {
                if (element.Kind == elementKind &&
                    element.FullName.Equals(targetFullName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return element;
                }
                CodeElement result = InspectBaseCodeElement(element, targetFullName, elementKind);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private static CodeElement InspectBaseCodeElement(
            CodeElement element, string targetFullName, vsCMElement elementKind)
        {
            if (element.Kind == vsCMElement.vsCMElementClass)
            {
                CodeClass target = (CodeClass)element;
                foreach (CodeElement interfaceType in target.ImplementedInterfaces)
                {
                    if (interfaceType.Kind == elementKind &&
                        interfaceType.FullName.Equals(targetFullName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return interfaceType;
                    }
                    if (interfaceType.InfoLocation == vsCMInfoLocation.vsCMInfoLocationExternal)
                    {
                        ProjectItem item = DteHelperEx.FindContainingProjectItem(element.ProjectItem.DTE, (CodeType)interfaceType);
                        if (item != null)
                        {
                            return FindCodeElementByFullName(item, targetFullName, elementKind);
                        }
                        return null;
                    }
                    CodeElement child = InspectChildren(interfaceType.Children, targetFullName, elementKind);
                    if (child != null)
                    {
                        return child;
                    }
                }
                return null;
            }

            if (element.Kind == vsCMElement.vsCMElementInterface)
            {
                return InspectChildren(element.Children, targetFullName, elementKind);
            }

            return null;
        }

        private static CodeElement InspectChildren(
            CodeElements elements, string targetFullName, vsCMElement elementKind)
        {
            foreach (CodeElement children in elements)
            {
                if (children.Kind == elementKind &&
                    children.FullName.Equals(targetFullName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return children;
                }
            }
            return null;
        }

        private static string FormatPropertyName(string variableName)
        {
            StringInfo si = new StringInfo(variableName);
            return si.SubstringByTextElements(0, 1).ToUpperInvariant() + si.SubstringByTextElements(1, si.LengthInTextElements - 1);
        }
    }
}
