using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EnvDTE;

namespace KLCodeNav
{
    public static class DteHelper4
    {
        public static ProjectItem FindProjectItemByName(Project project, string relativePath, ProjectItemType itemType)
        {
            return FindItemByName(project.ProjectItems, relativePath, itemType);
        }

        public static List<string> GetPathParts(string path)
        {
            List<string> parts = new List<string>();
            parts.AddRange(path.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries));

            return parts;
        }

        public static string GetVariableFromGlobals(EnvDTE.Globals globals, string name)
        {
            if (false == IsGlobalsVariableExists(globals, name))
                throw new ArgumentOutOfRangeException("Name");
            else
                return globals[name].ToString();
        }

        public static void SetGlobalsVariable(EnvDTE.Globals globals, string name, string value)
        {
            globals[name] = value;
            globals.set_VariablePersists(name, true);
        }

        public static bool IsGlobalsVariableExists(EnvDTE.Globals globals, string name)
        {
            return globals.get_VariableExists(name);
        }

        public static void RemoveGlobalsVariable(EnvDTE.Globals globals, string name)
        {
            globals[name] = null;
            globals.set_VariablePersists(name, false);
        }
        
        public static string GetProjectTargetPath(Project project)
        {
            string outputPath = (string)GetProjectConfigurationProperty(project, "OutputPath");
            if (null == outputPath)
                return null;

            string buildPath = (string)GetProjectProperty(project, "LocalPath");
            if (null == buildPath)
                return null;

            string targetName = (string)GetProjectProperty(project, "OutputFileName");
            if (null == targetName)
                return null;

            return Path.Combine(buildPath, Path.Combine(outputPath, targetName));
        }
        
        public static object GetProjectProperty(Project project, string propertyName)
        {
            Properties properties = project.Properties;
            if (null == properties)
                return null;

            foreach (Property property in properties)
            {
                try
                {
                    if (0 == string.Compare(property.Name, propertyName, true))
                        return property.Value;
                }
                catch
                {
                }
            }

            return null;
        }

        private static ProjectItem FindItemByName(ProjectItems projectItems, string itemPath, ProjectItemType itemType)
        {
            List<string> partNames = GetPathParts(itemPath);

            ProjectItems currentProjectItems = projectItems;
            ProjectItem subItem = null;
            foreach (string partName in partNames)
            {
                //subItem = GATLib.DteHelper.FindItemByName(currentProjectItems, partName, false);
                subItem = FindItemByName(currentProjectItems, partName, false);

                if (null == subItem)
                    return null;

                if ((ProjectItemType.Folder == itemType)
                    && (Constants.vsProjectItemKindPhysicalFolder != subItem.Kind))
                    return null;

                currentProjectItems = subItem.ProjectItems;
            }

            if ((ProjectItemType.File == itemType)
                && (Constants.vsProjectItemKindPhysicalFile != subItem.Kind))
                return null;

            return subItem;
        }

        private static object GetProjectConfigurationProperty(Project project, string propertyName)
        {
            if ((null == project.ConfigurationManager)
                || (null == project.ConfigurationManager.ActiveConfiguration))
                return null;

            Properties properties = project.ConfigurationManager.ActiveConfiguration.Properties;
            foreach (Property property in properties)
            {
                try
                {
                    if (0 == string.Compare(property.Name, propertyName, true))
                        return property.Value;
                }
                catch
                {
                }
            }

            return null;
        }

        public static ProjectItem FindItemByName(ProjectItems collection, string name, bool recursive)
        {
            foreach (ProjectItem item1 in collection)
            {
                if (string.Compare(item1.Name, name, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    return item1;
                }

                if (recursive)
                {
                    ProjectItem item2 = DteHelper4.FindItemByName(item1.ProjectItems, name, recursive);
                    if (item2 != null)
                    {
                        return item2;
                    }
                }
            }
            return null;
        }

        public enum ProjectItemType
        {
            File,
            Folder
        }
    }
}
