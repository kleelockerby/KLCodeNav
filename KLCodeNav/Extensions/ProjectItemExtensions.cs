using EnvDTE;
using System;
using System.Linq;

namespace KLCodeNav
{
    internal static class ProjectItemExtensions
    {
        internal static string GetFileName(this ProjectItem projectItem)
        {
            try
            {
                return projectItem.FileNames[1];
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        internal static ProjectItem GetParentProjectItem(this ProjectItem projectItem)
        {
            try
            {
                var parentProjectItem = projectItem.Collection?.Parent as ProjectItem;
                return parentProjectItem;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        internal static bool IsExternal(this ProjectItem projectItem)
        {
            try
            {
                if (projectItem.Collection == null || !projectItem.IsPhysicalFile())
                {
                    return true;
                }

                return projectItem.Collection.OfType<ProjectItem>().All(x => x.Object != projectItem.Object);
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        internal static bool IsPhysicalFile(this ProjectItem projectItem)
        {
            try
            {
                return string.Equals(projectItem.Kind, Constants.vsProjectItemKindPhysicalFile, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
