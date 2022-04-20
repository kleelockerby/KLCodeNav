using System;
using System.Collections.Generic;
using System.Text;
using EnvDTE;
using System.Reflection;
using Microsoft.VisualStudio.Shell;
using EnvDTE80;
using VSLangProj;
using System.Xml;

namespace KLCodeNav
{
    public static class ProjectItemHelper
    {
        public static CodeElement GetClassOrInterface(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            CodeNamespace codeNamespace = GetNamespace(projectItem);
            if (codeNamespace != null)
            {
                foreach (CodeElement codeElement in codeNamespace.Children)
                {
                    CodeClass codeClass = codeElement as CodeClass;
                    if (codeClass != null)
                    {
                        return (CodeElement)codeClass;
                    }
                    CodeInterface codeInterace = codeElement as CodeInterface;
                    if (codeInterace != null)
                    {
                        return (CodeElement)codeInterace;
                    }
                }
            }
            return null;
        }

        public static CodeClass GetBaseClass(CodeClass codeClass)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (CodeElement codeElement in codeClass.Bases)
            {
                CodeClass baseClass = codeElement as CodeClass;
                if (baseClass != null)
                {
                    return baseClass;
                }
            }
            return null;
        }

        public static CodeClass GetClass(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            CodeNamespace codeNamespace = GetNamespace(projectItem);
            if (codeNamespace != null)
            {
                foreach (CodeElement codeElement in codeNamespace.Children)
                {
                    CodeClass codeClass = codeElement as CodeClass;
                    if (codeClass != null)
                    {
                        return codeClass;
                    }
                }
            }
            else if (projectItem != null && projectItem.FileCodeModel != null && projectItem.FileCodeModel.CodeElements != null)
            {
                foreach (CodeElement codeElement in projectItem.FileCodeModel.CodeElements)
                {
                    CodeClass codeClass = codeElement as CodeClass;
                    if (codeClass != null)
                    {
                        return codeClass;
                    }
                }
            }
            return null;
        }

        public static string GetFullNameType(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (projectItem != null)
            {
                return GetClassOrInterface(projectItem).FullName;
            }
            return null;
        }

        public static IList<CodeImport> GetImports(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            List<CodeImport> imports = new List<CodeImport>();
            foreach (CodeElement codeElement in projectItem.FileCodeModel.CodeElements)
            {
                CodeImport codeImport = codeElement as CodeImport;
                if (codeImport != null)
                {
                    imports.Add(codeImport);
                }
            }
            return imports;
        }

        public static CodeNamespace GetNamespace(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            CodeNamespace codeNamespace = null;
            if (projectItem != null && projectItem.FileCodeModel != null && projectItem.FileCodeModel.CodeElements != null)
            {
                foreach (CodeElement codeElement in projectItem.FileCodeModel.CodeElements)
                {
                    CodeImport codeImport = codeElement as CodeImport;
                    codeNamespace = codeElement as CodeNamespace;
                    if (codeNamespace != null)
                    {
                        return codeNamespace;
                    }
                }
            }
            return codeNamespace;
        }

        public static IList<CodeClass> GetClasses(CodeNamespace codeNamespaces)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            List<CodeClass> classes = new List<CodeClass>();
            if (codeNamespaces == null)
            {
                return classes;
            }
            foreach (CodeElement codeElement in codeNamespaces.Children)
            {
                CodeClass codeClass = codeElement as CodeClass;
                if (codeClass != null)
                {
                    classes.Add(codeClass);
                }
            }
            return classes;
        }
    }
}
