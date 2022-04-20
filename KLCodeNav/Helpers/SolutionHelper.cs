using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KLCodeNav
{
    internal static class SolutionHelper
    {
        internal static IEnumerable<T> GetAllItemsInSolution<T>(Solution solution) where T : class
        {
            var allProjects = new List<T>();
            if (solution != null)
            {
                allProjects.AddRange(GetItemsRecursively<T>(solution));
            }
            return allProjects;
        }

        internal static IEnumerable<T> GetItemsRecursively<T>(object parentItem) where T : class
        {
            if (parentItem == null)
            {
                throw new ArgumentNullException(nameof(parentItem));
            }

            var projectItems = new List<T>();

            if (parentItem is T desiredType)
            {
                projectItems.Add(desiredType);
            }

            var children = GetChildren(parentItem);

            foreach (var childItem in children)
            {
                projectItems.AddRange(GetItemsRecursively<T>(childItem));
            }

            return projectItems;
        }

        internal static IEnumerable<ProjectItem> GetSelectedProjectItemsRecursively(KLCodeNavPackage package)
        {
            var selectedProjectItems = new List<ProjectItem>();
            var selectedUIHierarchyItems = UIHierarchyHelper.GetSelectedUIHierarchyItems(package);

            foreach (var item in selectedUIHierarchyItems.Select(uiHierarchyItem => uiHierarchyItem.Object))
            {
                selectedProjectItems.AddRange(GetItemsRecursively<ProjectItem>(item));
            }

            return selectedProjectItems;
        }

        internal static IEnumerable<ProjectItem> GetSimilarProjectItems(KLCodeNavPackage package, ProjectItem projectItem)
        {
            var allItems = GetAllItemsInSolution<ProjectItem>(package.DTE2.Solution);

            return allItems.Where(x => x.Name == projectItem.Name && x.Kind == projectItem.Kind && x.Document.FullName == projectItem.Document.FullName);
        }

        private static IEnumerable<object> GetChildren(object parentItem)
        {
            var solution = parentItem as Solution;
            if (solution?.Projects != null)
            {
                return solution.Projects.Cast<Project>().Cast<object>().ToList();
            }

            var project = parentItem as Project;
            if (project?.ProjectItems != null)
            {
                return project.ProjectItems.Cast<ProjectItem>().Cast<object>().ToList();
            }

            if (parentItem is ProjectItem projectItem)
            {
                if (projectItem.ProjectItems != null)
                {
                    return projectItem.ProjectItems.Cast<ProjectItem>().Cast<object>().ToList();
                }

                if (projectItem.SubProject != null)
                {
                    return new[] { projectItem.SubProject };
                }
            }

            return new object[0];
        }

    }
}