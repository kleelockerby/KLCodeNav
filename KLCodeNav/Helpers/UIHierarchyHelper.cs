using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KLCodeNav
{
    internal static class UIHierarchyHelper
    {
        internal static void CollapseRecursively(UIHierarchyItem parentItem)
        {
            if (parentItem == null)
            {
                throw new ArgumentNullException(nameof(parentItem));
            }

            if (!parentItem.UIHierarchyItems.Expanded) return;

            foreach (UIHierarchyItem childItem in parentItem.UIHierarchyItems)
            {
                CollapseRecursively(childItem);
            }

            /*if (ShouldCollapseItem(parentItem))
            {
                parentItem.UIHierarchyItems.Expanded = false;

                if (parentItem.UIHierarchyItems.Expanded)
                {
                    parentItem.Select(vsUISelectionType.vsUISelectionTypeSelect);
                    ((DTE2)parentItem.DTE).ToolWindows.SolutionExplorer.DoDefaultAction();
                }
            }*/
        }

        internal static IEnumerable<UIHierarchyItem> GetSelectedUIHierarchyItems(KLCodeNavPackage package)
        {
            var solutionExplorer = GetSolutionExplorer(package);

            return ((object[])solutionExplorer.SelectedItems).Cast<UIHierarchyItem>().ToList();
        }

        internal static UIHierarchy GetSolutionExplorer(KLCodeNavPackage package)
        {
            return package.DTE2.ToolWindows.SolutionExplorer;
        }

        internal static UIHierarchyItem GetTopUIHierarchyItem(KLCodeNavPackage package)
        {
            var solutionExplorer = GetSolutionExplorer(package);

            return solutionExplorer.UIHierarchyItems.Count > 0 ? solutionExplorer.UIHierarchyItems.Item(1) : null;
        }

        internal static bool HasExpandedChildren(UIHierarchyItem parentItem)
        {
            if (parentItem == null)
            {
                throw new ArgumentNullException(nameof(parentItem));
            }

            return parentItem.UIHierarchyItems.Cast<UIHierarchyItem>().Any( childItem => childItem.UIHierarchyItems.Expanded || HasExpandedChildren(childItem));
        }

        /*private static bool ShouldCollapseItem(UIHierarchyItem parentItem)
        {
            if (parentItem.Object is Solution)
            {
                return false;
            }

            if (Settings.Default.Collapsing_KeepSoloProjectExpanded && parentItem.Object is Project)
            {
                var solution = parentItem.DTE.Solution;

                if (solution != null && solution.Projects.OfType<Project>().All(x => x == parentItem.Object || x.Name == "Miscellaneous Files"))
                {
                    return false;
                }
            }

            return true;
        }*/

    }
}