using System.Diagnostics;
using EnvDTE;
using EnvDTE80;
using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.Linq;

namespace KLCodeNav
{
    public static class ProjectsHelper
    {
        public static void ParseAllProjectItems(Solution2 solution)
        {
            if (solution == null) return;

            foreach (Project project in solution.Projects)
            {
                if (project.Object != null)
                {
                    for (int x = 1 ; x < project.ProjectItems.Count + 1 ; x++)
                    {
                        ParseProjectItems(project.ProjectItems.Item(x));
                    }
                }
            }
        }
        public static void ParseProjectItems(ProjectItem projectItem)
        {
            if (projectItem == null) return;

            FileCodeModel2 fileCodeModel = projectItem.FileCodeModel as FileCodeModel2;

            for (int x = 1 ; x < projectItem.ProjectItems.Count + 1 ; x++)
            {
                fileCodeModel = projectItem.ProjectItems.Item(x).FileCodeModel as FileCodeModel2;

                if (fileCodeModel != null)
                {
                    ParseFileCodeModel(fileCodeModel);
                }

                ParseProjectItems(projectItem.ProjectItems.Item(x));
            }
        }
        public static void ParseFileCodeModel(FileCodeModel2 fileCodeModel)
        {
            CodeElement codeElement = null;

            for (int x = 1 ; x < fileCodeModel.CodeElements.Count + 1 ; x++)
            {
                codeElement = fileCodeModel.CodeElements.Item(x);

                if (codeElement.Kind == vsCMElement.vsCMElementNamespace)
                {
                    foreach (CodeElement cdClassElmnt in ((CodeNamespace)codeElement).Members)
                    {
                        Debug.WriteLine("Element Type: " + cdClassElmnt.Kind + "Element Name: " + cdClassElmnt.Name);
                    }
                }
            }
        }
    }
}
