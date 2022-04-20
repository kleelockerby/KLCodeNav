using System;
using System.Collections.Generic;
using System.Text;
using EnvDTE80;
using EnvDTE;
using System.Diagnostics;

namespace KLCodeNav
{
    public static class VsCodeModelUtilities
    {
        public static CodeElement[] GetAllCodeElementsOfKind(FileCodeModel codeModel, vsCMElement kind)
        {
            List<CodeElement> list = new List<CodeElement>();
            GetAllCodeElementsOfKind(codeModel.CodeElements, kind, list);
            return list.ToArray();
        }

        private static void GetAllCodeElementsOfKind(CodeElements codeElements, vsCMElement kind, List<CodeElement> list)
        {
            foreach (CodeElement codeElement in codeElements)
            {
                if (codeElement.Kind == kind)
                {
                    list.Add(codeElement);
                }

                CodeElements children = GetCodeElementMembers(codeElement);
                if (children != null)
                {
                    GetAllCodeElementsOfKind(children, kind, list);
                }
            }
        }

        public static CodeClass2 GetClassFromSelection(DTE2 dte)
        {
            if (dte == null)
                throw new ArgumentNullException("dte");

            return GetCodeElementFromSelection(dte, vsCMElement.vsCMElementClass) as CodeClass2;
        }

        public static CodeInterface2 GetInterfaceFromSelection(DTE2 dte)
        {
            if (dte == null)
                throw new ArgumentNullException("dte");

            return GetCodeElementFromSelection(dte, vsCMElement.vsCMElementInterface) as CodeInterface2;
        }

        public static CodeElement GetCodeElementFromSelection(DTE2 dte, vsCMElement elementType)
        {
            if (dte == null)
                throw new ArgumentNullException("dte");

            if (dte.ActiveDocument != null)
                return GetCodeElementFromSelection(dte.ActiveDocument, elementType);
            return null;
        }

        public static CodeElement GetCodeElementFromSelection(Document document, vsCMElement elementType)
        {
            if (document == null)
                throw new ArgumentNullException("document");

            ProjectItem projectItem = document.ProjectItem;
            if (projectItem != null)
            {
                FileCodeModel codeModel = projectItem.FileCodeModel;
                if (codeModel != null)
                {
                    try
                    {
                        TextSelection textSelection = (TextSelection)document.Selection;
                        return codeModel.CodeElementFromPoint(textSelection.TopPoint, elementType);
                    }
                    catch (Exception)
                    {
                        //Swallow this exception ... 
                    }
                }
            }

            return null;
        }

        public static CodeVariable2[] GetVariables(CodeClass2 codeClass)
        {
            List<CodeVariable2> codeVariables = new List<CodeVariable2>();
            foreach (CodeElement codeElement in codeClass.Members)
            {
                if (codeElement.Kind == vsCMElement.vsCMElementVariable)
                {
                    CodeVariable2 codeVariable = codeElement as CodeVariable2;
                    if (codeVariable != null && codeVariable.ConstKind == vsCMConstKind.vsCMConstKindNone)
                    {
                        codeVariables.Add(codeVariable);
                    }
                }
            }
            return codeVariables.ToArray();
        }

        public static bool ContainsMember(CodeClass2 codeClass, string memberName)
        {
            foreach (CodeElement member in codeClass.Members)
            {
                if (member.Name == memberName)
                    return true;
            }
            return false;
        }

        /*public static void GeneratePropertyFromVariable(CodeVariable2 variable, bool generateGetter, bool generateSetter, bool generateComments)
        {
            CodeClass2 codeClass = variable.Collection.Parent as CodeClass2;
            CodeGenerator codeGenerator = CreateCodeGenerator(codeClass.Language);

            string propertyName = ConvertVariableNameToPropertyName(variable.Name);
            if (!ContainsMember(codeClass, propertyName))
            {

                string getterName = String.Empty;
                string setterName = String.Empty;

                if (generateGetter)
                    getterName = propertyName;
                if (generateSetter)
                    setterName = propertyName;

                CodeProperty property = (CodeProperty)codeClass.AddProperty(getterName, setterName, variable.Type, -1, vsCMAccess.vsCMAccessPublic, null);
                if (generateComments)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("<doc>");
                    sb.AppendLine("<summary>");
                    sb.AppendLine();
                    sb.AppendLine("</summary>");
                    sb.Append("</doc>");

                    property.DocComment = sb.ToString();
                }

                if (generateGetter)
                {
                    EditPoint2 editPoint = (EditPoint2)property.Getter.GetStartPoint(vsCMPart.vsCMPartBody).CreateEditPoint();
                    editPoint.EndOfLine();
                    int position = editPoint.LineCharOffset;
                    editPoint.StartOfLine();
                    editPoint.Delete(position);
                    editPoint.Insert(codeGenerator.GenerateReturnStatement(variable.Name));
                    editPoint.SmartFormat(editPoint);
                }

                if (generateSetter)
                {
                    EditPoint2 editPoint = (EditPoint2)property.Setter.GetStartPoint(vsCMPart.vsCMPartBody).CreateEditPoint();
                    editPoint.Insert(codeGenerator.GenerateAssignStatement(variable.Name, "value"));
                    editPoint.SmartFormat(editPoint);
                }
            }

        }*/

        /*private static CodeGenerator CreateCodeGenerator(string language)
        {
            switch (language)
            {
                case CodeModelLanguageConstants.vsCMLanguageCSharp:
                    return new CSharpCodeGenerator();

                case CodeModelLanguageConstants.vsCMLanguageVB:
                    return new VBCodeGenerator();

                default:
                    throw new NotImplementedException();
            }
        }*/

        public static bool IsConstructorDefined(CodeClass2 codeClass, CodeVariable2[] codeVariables)
        {
            List<CodeFunction2> constructors = GetConstructors(codeClass);
            foreach (CodeFunction2 constructor in constructors)
            {
                if (constructor.Parameters.Count == codeVariables.Length)
                {
                    bool areEqual = true;
                    foreach (CodeElement codeElement in constructor.Parameters)
                    {
                        CodeParameter2 codeParameter = (CodeParameter2)codeElement;
                        if (!ContainsType(codeVariables, codeParameter.Type.AsFullName))
                        {
                            areEqual = false;
                            break;
                        }

                    }

                    if (areEqual)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ContainsType(CodeVariable2[] codeVariables, string fullName)
        {
            foreach (CodeVariable2 codeVariable in codeVariables)
            {
                if (codeVariable.Type.AsFullName == fullName)
                    return true;
            }
            return false;
        }


        private static List<CodeFunction2> GetConstructors(CodeClass2 codeClass)
        {
            List<CodeFunction2> constructors = new List<CodeFunction2>();

            foreach (CodeElement codeElement in codeClass.Members)
            {
                if (codeElement.Kind == vsCMElement.vsCMElementFunction && codeElement.Name == codeClass.Name)
                {
                    constructors.Add((CodeFunction2)codeElement);
                }
            }
            return constructors;
        }


        /*public static void GenerateConstructor(CodeClass2 codeClass, CodeVariable2[] codeVariables, bool generateComments, vsCMAccess accessModifier)
        {
            CodeGenerator codeGenerator = CreateCodeGenerator(codeClass.Language);


            CodeFunction2 codeFunction = null;
            if (codeClass.Language == CodeModelLanguageConstants.vsCMLanguageCSharp)
            {
                codeFunction = (CodeFunction2)codeClass.AddFunction(codeClass.Name, vsCMFunction.vsCMFunctionConstructor, null, -1, accessModifier, null);
            }
            else if (codeClass.Language == CodeModelLanguageConstants.vsCMLanguageVB)
            {
                codeFunction = (CodeFunction2)codeClass.AddFunction("New", vsCMFunction.vsCMFunctionSub, vsCMTypeRef.vsCMTypeRefVoid, -1, accessModifier, null);
            }

            if (generateComments)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<doc>");
                sb.AppendLine("<summary>");
                sb.AppendLine("</summary>");
                foreach (CodeVariable2 codeVariable in codeVariables)
                {
                    sb.AppendLine(String.Format("<param name=\"{0}\"></param>", codeVariable.Name));
                }
                sb.Append("</doc>");

                codeFunction.DocComment = sb.ToString();
            }

            foreach (CodeVariable2 codeVariable in codeVariables)
            {
                codeFunction.AddParameter(codeVariable.Name, codeVariable.Type.AsString, -1);
            }

            EditPoint2 editPoint = (EditPoint2)codeFunction.GetStartPoint(vsCMPart.vsCMPartBody).CreateEditPoint();

            foreach (CodeVariable2 codeVariable in codeVariables)
            {
                editPoint.Insert(codeGenerator.GenerateAssignStatement(codeVariable.Name, codeVariable.Name));
                editPoint.SmartFormat(editPoint);

                if (Array.IndexOf(codeVariables, codeVariable) < codeVariables.Length - 1)
                {
                    editPoint.InsertNewLine(1);
                }
            }

            editPoint.TryToShow(vsPaneShowHow.vsPaneShowCentered, codeFunction.StartPoint);
        }*/

        public static string ConvertVariableNameToPropertyName(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");

            if (text == String.Empty)
                throw new ArgumentException("Argument can not be empty.", "text");

            if (text.StartsWith("_"))
                text = text.Substring(1);

            string propertyName = text.Substring(0, 1).ToUpper();
            if (text.Length > 1)
                propertyName += text.Substring(1, text.Length - 1);
            return propertyName;

        }

        public static bool IsCodeElementPrivate(CodeElement codeElement)
        {
            switch (codeElement.Kind)
            {
                case vsCMElement.vsCMElementVariable:
                    CodeVariable2 codeVariable = (CodeVariable2)codeElement;
                    if (codeVariable.Access == vsCMAccess.vsCMAccessPrivate)
                        return true;
                    break;

                case vsCMElement.vsCMElementFunction:
                    CodeFunction2 codeFunction = (CodeFunction2)codeElement;
                    if (codeFunction.Access == vsCMAccess.vsCMAccessPrivate)
                        return true;
                    break;

                case vsCMElement.vsCMElementProperty:
                    CodeProperty codeProperty = (CodeProperty)codeElement;
                    if (codeProperty.Access == vsCMAccess.vsCMAccessPrivate)
                        return true;
                    break;

                case vsCMElement.vsCMElementClass:
                    CodeClass2 codeClass = (CodeClass2)codeElement;
                    if (codeClass.Access == vsCMAccess.vsCMAccessPrivate)
                        return true;
                    break;

                case vsCMElement.vsCMElementDelegate:
                    CodeDelegate2 codeDelegate = (CodeDelegate2)codeElement;
                    if (codeDelegate.Access == vsCMAccess.vsCMAccessPrivate)
                        return true;
                    break;

                case vsCMElement.vsCMElementEvent:
                    CodeEvent codeEvent = (CodeEvent)codeElement;
                    if (codeEvent.Access == vsCMAccess.vsCMAccessPrivate)
                        return true;
                    break;

                case vsCMElement.vsCMElementInterface:
                    CodeInterface2 codeInterface = (CodeInterface2)codeElement;
                    if (codeInterface.Access == vsCMAccess.vsCMAccessPrivate)
                        return true;
                    break;

                case vsCMElement.vsCMElementStruct:
                    CodeStruct2 codeStruct = (CodeStruct2)codeElement;
                    if (codeStruct.Access == vsCMAccess.vsCMAccessPrivate)
                        return true;
                    break;
            }

            return false;
        }


        public static CodeElements GetCodeElementMembers(CodeElement codeElement)
        {
            if (codeElement.Kind == vsCMElement.vsCMElementClass)
            {
                CodeClass2 codeClass = (CodeClass2)codeElement;
                return codeClass.Members;
            }
            else if (codeElement.Kind == vsCMElement.vsCMElementInterface)
            {
                CodeInterface2 codeInterface = (CodeInterface2)codeElement;
                return codeInterface.Members;
            }
            else if (codeElement.Kind == vsCMElement.vsCMElementStruct)
            {
                CodeStruct2 codeStruct = (CodeStruct2)codeElement;
                return codeStruct.Members;
            }
            else if (codeElement.Kind == vsCMElement.vsCMElementEnum)
            {
                CodeEnum codeEnum = (CodeEnum)codeElement;
                return codeEnum.Members;
            }
            else if (codeElement.Kind == vsCMElement.vsCMElementNamespace)
            {
                CodeNamespace codeNamespace = (CodeNamespace)codeElement;
                return codeNamespace.Members;
            }

            return null;
        }

    }
}
