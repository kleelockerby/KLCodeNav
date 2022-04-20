using System;
using System.IO;
using EnvDTE;
using VSLangProj;
using System.Collections;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.VisualStudio.OLE.Interop;
using System.ComponentModel.Design;
using EnvDTE80;
using System.Collections.Generic;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;

namespace KLCodeNav
{
    public static class DteHelper2
    {
		public static string BuildPath(object toElement)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (toElement is SelectedItem)
			{
				return BuildPath((SelectedItem)toElement);
			}
			else if (toElement is SolutionFolder)
			{
				return BuildPath(((SolutionFolder)toElement).Parent);
			}
			else if (toElement is Project)
			{
				return BuildPath((Project)toElement);
			}
			else if (toElement is ProjectItem)
			{
				return BuildPath((ProjectItem)toElement);
			}
			else
			{
				throw new NotSupportedException(toElement.ToString());
			}
		}

		public static string BuildPath(SelectedItem toSelectedItem)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (toSelectedItem.ProjectItem != null)
			{
				return BuildPath(toSelectedItem.ProjectItem);
			}
			else if (toSelectedItem.Project != null)
			{
				return BuildPath(toSelectedItem.Project);
			}

			return toSelectedItem.Name;
		}

		public static string BuildPath(Project toProject)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			string path = "";

			if (toProject.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
			{
				string folder = "";
				foreach (Project project in toProject.DTE.Solution.Projects)
				{
					folder = project.Name;
					if (project == toProject)
					{
						break;
					}
					else if (BuildPathToFolder(project, toProject, ref folder))
					{
						break;
					}
				}

				path = folder + path;
			}
			else
			{
				try
				{
					if (toProject.ParentProjectItem == null)
					{
						return toProject.Name;
					}
				}
				catch (NotImplementedException) { }

				string folder = "";
				foreach (Project project in toProject.DTE.Solution.Projects)
				{
					folder = project.Name;
					if (project == toProject)
					{
						break;
					}
					else if (BuildPathToFolder(project, toProject, ref folder))
					{
						break;
					}
				}

				path = folder + path;
			}

			return path;
		}

		public static string BuildPath(ProjectItem toItem)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			string path = "";

			if (toItem.ContainingProject != null)
			{
				if (!BuildPathFromCollection(toItem.ContainingProject.ProjectItems, toItem, ref path))
				{
					return "";
				}
				else
				{
					path = Path.Combine(BuildPath(toItem.ContainingProject), path);
				}
			}
			else
			{
				path = toItem.Name;
			}

			return path;
		}

		public static bool BuildPathFromCollection(ProjectItems items, ProjectItem target, ref string path)
		{
			if (items == null) return false;
			
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem item in items)
			{
				if (item == target)
				{
					path = path + target.Name;
					return true;
				}
				else
				{
					string tmp = path + item.Name + Path.DirectorySeparatorChar;
					ProjectItems childitems = item.ProjectItems;
					if (childitems == null && item.Object is Project)
						childitems = ((Project)item.Object).ProjectItems;

					bool found = BuildPathFromCollection(childitems, target, ref tmp);
					if (found)
					{
						path = tmp;
						return true;
					}
				}
			}

			return false;
		}

		private static bool BuildPathToFolder(Project parent, Project target, ref string path)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (parent == null || parent.ProjectItems == null) return false;

			foreach (ProjectItem item in parent.ProjectItems)
			{
				try
				{
					if (item.Object == target)
					{
						path = path + Path.DirectorySeparatorChar + target.Name;
						return true;
					}
					else if (item.Kind == EnvDTE.Constants.vsProjectItemKindSolutionItems)
					{
						string tmp = path + Path.DirectorySeparatorChar + item.Name;
						bool found = BuildPathToFolder(item.Object as Project, target, ref tmp);
						if (found)
						{
							path = tmp;
							return true;
						}
					}
				}
				catch
				{
					continue;
				}
			}

			return false;
		}

		public static Project FindProject(_DTE vs, Predicate<Project> match)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			foreach (Project project in vs.Solution.Projects)
			{
				if (match(project))
				{
					return project;
				}
				else if (project.ProjectItems != null)
				{
					Project child = FindProjectInternal(project.ProjectItems, match);
					if (child != null)
					{
						return child;
					}
				}
			}

			return null;
		}

		private static Project FindProjectInternal(ProjectItems items, Predicate<Project> match)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem item in items)
			{
				Project project = item.Object as Project;

				if (project == null && item.SubProject != null)
				{
					project = item.SubProject as Project;
				}

				if (project != null && match(project))
				{
					return project;
				}
				else if (item.ProjectItems != null)
				{
					Project child = FindProjectInternal(item.ProjectItems, match);
					if (child != null)
					{
						return child;
					}
				}
				else if (project != null && project.ProjectItems != null)
				{
					Project child = FindProjectInternal(project.ProjectItems, match);
					if (child != null)
					{
						return child;
					}
				}
			}

			return null;
		}

		public static Project FindProjectByAssemblyName(_DTE vs, string name)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			return FindProject(vs, delegate (Project project)
			{
				Property prop = project.Properties.Item("AssemblyName");
				return prop != null && prop.Value != null &&
					prop.Value.ToString() == name;
			});
		}

		public static Project FindProjectByName(_DTE vs, string name)
		{
			return FindProject(vs, delegate (Project project)
			{
				return project.Name == name;
			});
		}

		public static EnvDTE80.SolutionFolder FindSolutionFolderByPath(EnvDTE.Solution root, string path)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			Project prj = FindProjectByPath(root, path);
			if (prj != null)
			{
				return prj.Object as EnvDTE80.SolutionFolder;
			}
			return null;
		}

		public static Project FindProjectByPath(EnvDTE.Solution root, string path)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			string[] allpaths = path.Split(System.IO.Path.DirectorySeparatorChar,
				System.IO.Path.AltDirectorySeparatorChar);

			if (allpaths.Length == 0)
			{
				return null;
			}

			Project prj = null;
			foreach (Project p in root.Projects)
			{
				if (p.Name == allpaths[0])
				{
					prj = p;
					break;
				}
			}

			if (prj == null) return null;

			string[] paths = new string[allpaths.Length - 1];
			if (paths.Length == 0)
			{
				return prj;
			}

			Array.Copy(allpaths, 1, paths, 0, paths.Length);

			ProjectItem item = FindInCollectionRecursive(prj.ProjectItems, paths, 0);
			if (item == null)
			{
				return null;
			}
			{
				return item.Object as Project;
			}
		}

		public static ProjectItem FindItemByName(ProjectItems collection, string name, bool recursive)
		{
			if (collection != null)
			{
				ThreadHelper.ThrowIfNotOnUIThread();
				foreach (ProjectItem item in collection)
				{
					if (item.Name == name)
						return item;

					if (recursive)
					{
						ProjectItem child = FindItemByName(item.ProjectItems, name, recursive);
						if (child != null)
							return child;
					}
				}
			}
			return null;
		}

		public static uint FindItemByName(IVsHierarchy projectHierarchy, string name)
		{
			
			uint foundItemID = VSConstants.VSITEMID_NIL;
			EnumHierarchyItems(projectHierarchy, delegate (IVsHierarchy hierarchy, uint itemid, int recursionLevel)
				{
					ThreadHelper.ThrowIfNotOnUIThread();
					string itemName = null;
					hierarchy.GetCanonicalName(itemid, out itemName);
					if (itemName!=null)
					{
						itemName = new FileInfo(itemName).Name;
						if (itemName.Equals(name))
						{
							foundItemID = itemid;
							return false;
						}
					}
					return true;
				});

			return foundItemID;
		}

		public static uint FindItemInProject(IVsHierarchy projectHierarchy, string name)
		{
			uint foundItemId = VSConstants.VSITEMID_NIL;
			EnumHierarchyItems(projectHierarchy,
			delegate (IVsHierarchy hierarchy, uint itemid, int recursionLevel)
			{
				ThreadHelper.ThrowIfNotOnUIThread();
				object itemName = null;
				hierarchy.GetProperty(itemid, (int)__VSHPROPID.VSHPROPID_Name, out itemName);

				if (itemName!=null && itemName.ToString().Equals(name))
				{
					foundItemId = itemid;
					return false;
				}
				return true;
			});
			return foundItemId;
		}

		public static ProjectItem FindItemByPath(EnvDTE.Solution root, string path)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			string[] allpaths = path.Split( new char[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

			if (allpaths.Length == 0)
			{
				return null;
			}

			Project prj = null;
			foreach (Project p in root.Projects)
			{
				if (p.Name == allpaths[0])
				{
					prj = p;
					break;
				}
			}

			if (prj == null)
			{
				return null;
			}

			string[] paths = new string[allpaths.Length - 1];
			if (paths.Length == 0)
			{
				return null;
			}

			Array.Copy(allpaths, 1, paths, 0, paths.Length);

			ProjectItem item = FindInCollectionRecursive(prj.ProjectItems, paths, 0);
			if ((item != null) && !(item.Object is Project || item.Object is EnvDTE80.SolutionFolder))
			{
				return item;
			}
			return null;
		}

		public static ProjectItem FindInCollection(ProjectItems collection, string path)
		{
			string[] allpaths = path.Split( new char[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

			return FindInCollectionRecursive(collection, allpaths, 0);
		}

		private static ProjectItem FindInCollectionRecursive(ProjectItems collection, string[] paths, int index)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			foreach (ProjectItem item in collection)
			{
				/*if (item.Name == paths[index] || MatchesWebProjectName(item, paths[index]))
				{
					if (index == paths.Length - 1)
					{
						return item;
					}
					else
					{
						if (item.Object is Project)
						{
							return FindInCollectionRecursive(
								((Project)item.Object).ProjectItems,
								paths, ++index);
						}
						else
						{
							return FindInCollectionRecursive(item.ProjectItems, paths, ++index);
						}
					}
				}*/
			}

			return null;
		}

		/*private static bool MatchesWebProjectName(ProjectItem item, string name)
		{
			Project project = item.Object as Project;
			if (project != null && project.Kind == VsWebSite.PrjKind.prjKindVenusProject)
			{
				string simpleName = Path.GetDirectoryName(item.Name);
				simpleName = simpleName.Substring(simpleName.LastIndexOf(Path.DirectorySeparatorChar) + 1);
				if (name == simpleName)
				{
					return true;
				}
			}

			return false;
		}*/

		private static UIHierarchyItem FindHierarchyItemByPath(UIHierarchyItems items, string[] paths, int index, Dictionary<UIHierarchyItems, bool> expandedItems)
		{
			foreach (UIHierarchyItem item in items)
			{
				if (item.Name == paths[index])
				{
					if (index == paths.Length - 1)
					{
						return item;
					}
					else
					{
						return FindHierarchyItemByPath(item.UIHierarchyItems, paths, ++index, expandedItems);
					}
				}
			}

			expandedItems.Add(items, items.Expanded);

			return null;
		}

		public static Project FindProjectByGuid(EnvDTE.Solution solution, Guid projectGuid)
		{
			ServiceProvider serviceProvider = new ServiceProvider(solution.DTE as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
			IVsSolution vsSolution = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
			IVsHierarchy hierarchy;
			vsSolution.GetProjectOfGuid(ref projectGuid, out hierarchy);

			object prjObject = null;

			if (hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out prjObject) >= 0)
			{
				return (Project)prjObject;
			}
			else
			{
				throw new ArgumentException("Hierarchy is not a project.");
			}
		}

		public static object GetTarget(_DTE vs)
		{
			if (vs.SelectedItems != null && vs.SelectedItems.Count > 0)
			{
				if (vs.SelectedItems.Count == 1)
				{
					EnvDTE.SelectedItem item = vs.SelectedItems.Item(1);
					if (item.Project != null)
					{
						return item.Project;
					}
					else if (item.ProjectItem != null)
					{
						return item.ProjectItem;
					}
					else if (vs.Solution.Properties.Item("Name").Value.Equals(item.Name))
					{
						return vs.Solution;
					}
					return item;
				}
				else
				{
					return vs.SelectedItems;
				}
			}
			throw new Exception();
		}

		public static string GetDefaultExtension(Project project)
		{
			if (project.Kind == PrjKind.prjKindCSharpProject)
			{
				return ".cs";
			}
			else if (project.Kind == PrjKind.prjKindVBProject)
			{
				return ".vb";
			}
			else
			{
				throw new Exception();
			}
		}

		public static string GetProjectNamespace(Project project)
		{
			string ns = project.Properties.Item("DefaultNamespace").Value.ToString();
			if (string.IsNullOrEmpty(ns))
			{
				ns = project.Properties.Item("RootNamespace").Value.ToString();
			}
			if (string.IsNullOrEmpty(ns))
			{
				ns = project.Properties.Item("AssemblyName").Value.ToString();
			}
			return ns;
		}

		public static Project GetSelectedProject(_DTE vs)
		{
			foreach (object obj in (object[])vs.ActiveSolutionProjects)
			{
				if (obj is Project) return obj as Project;
			}

			return null;
		}

		public static string GetFilePathRelative(ProjectItem item)
		{
			return GetFilePathRelative(item.DTE, item.get_FileNames(1));
		}

		public static string GetFilePathRelative(_DTE vs, string file)
		{
			if (!file.StartsWith(Path.GetDirectoryName(vs.Solution.FullName)))
			{
				throw new Exception();
			}
			string relative = file.Replace(Path.GetDirectoryName(vs.Solution.FullName), "");
			if (relative.StartsWith(Path.DirectorySeparatorChar.ToString()))
			{
				relative = relative.Substring(1);
			}
			return relative;
		}

		public static string GetPathFull(_DTE vs, string file)
		{
			if (Path.IsPathRooted(file) &&
				!file.StartsWith(Path.GetDirectoryName(vs.Solution.FullName)))
			{
				throw new Exception();
			}
			return Path.Combine(Path.GetDirectoryName(vs.Solution.FullName), file);
		}

		public static Guid GetProjectGuid(System.IServiceProvider serviceProvider, Project project)
		{
			IVsHierarchy nestedHierarchy = GetVsHierarchy(serviceProvider, project);
			Guid nestedHiererachyInstanceGuid;

			nestedHierarchy.GetGuidProperty(
				VSConstants.VSITEMID_ROOT,
				(int)__VSHPROPID.VSHPROPID_ProjectIDGuid,
				out nestedHiererachyInstanceGuid);

			return nestedHiererachyInstanceGuid;
		}
		public static bool SelectSolution(_DTE vs)
		{
			UIHierarchy hier;
			UIHierarchyItem sol;

			try
			{
				GetAndSelectSolutionExplorerHierarchy(vs, out hier, out sol);

			}
			catch (Exception)
			{
				return false;
			}

			return true;
		}

		public static UIHierarchyItem SelectItem(_DTE vs, string path)
		{
			UIHierarchy hier;
			UIHierarchyItem sol;
			GetAndSelectSolutionExplorerHierarchy(vs, out hier, out sol);

			UIHierarchyItem item = null;
			try
			{
				string slnpath = Path.Combine(sol.Name, path);
				item = hier.GetItem(slnpath);
			}
			catch (ArgumentException)
			{
				Dictionary<UIHierarchyItems, bool> expandedItems = new Dictionary<UIHierarchyItems, bool>();

				item = FindHierarchyItemByPath(sol.UIHierarchyItems,
					path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), 0, expandedItems);
				RestoreExpandedItems(expandedItems);
			}

			if (item != null)
			{
				item.UIHierarchyItems.Expanded = true;
				item.Select(vsUISelectionType.vsUISelectionTypeSelect);
			}

			return item;
		}

		public static UIHierarchyItem SelectItem(_DTE vs, object target)
		{
			UIHierarchy hier;
			UIHierarchyItem sol;
			GetAndSelectSolutionExplorerHierarchy(vs, out hier, out sol);

			Dictionary<UIHierarchyItems, bool> expandedItems = new Dictionary<UIHierarchyItems, bool>();

			UIHierarchyItem locatedItem = LocateInUICollection(sol.UIHierarchyItems, target, expandedItems);
			RestoreExpandedItems(expandedItems);

			return locatedItem;
		}

		private static void RestoreExpandedItems(Dictionary<UIHierarchyItems, bool> expandedItems)
		{
			foreach (KeyValuePair<UIHierarchyItems, bool> pair in expandedItems)
			{
				pair.Key.Expanded = pair.Value;
			}
		}

		private static bool IsVsSetupProject(UIHierarchyItem item)
		{
			ProjectItem prjItem = item.Object as ProjectItem;
			Project prj;
			if (prjItem != null)
			{
				prj = prjItem.Object as Project;
				if (prj != null)
				{
					if (prj.Kind == "{54435603-DBB4-11D2-8724-00A0C9A8B90C}")
					{
						return true;
					}
				}
			}
			prj = item.Object as Project;
			if (prj != null)
			{
				if (prj.Kind == "{54435603-DBB4-11D2-8724-00A0C9A8B90C}")
				{
					return true;
				}
			}
			return false;
		}
		private static UIHierarchyItem LocateInUICollection(UIHierarchyItems items, object target, Dictionary<UIHierarchyItems, bool> expandedItems)
		{
			if (items == null) return null;

			foreach (UIHierarchyItem item in items)
			{
				if (IsVsSetupProject(item))
				{
					continue;
				}
				ProjectItem prjItem = item.Object as ProjectItem;
				if (item.Object == target ||
					(prjItem != null && prjItem.Object == target))
				{
					item.Select(vsUISelectionType.vsUISelectionTypeSelect);
					return item;
				}

				UIHierarchyItem child = LocateInUICollection(item.UIHierarchyItems, target, expandedItems);
				if (child != null) return child;
			}

			expandedItems.Add(items, items.Expanded);

			return null;
		}

		private static void GetAndSelectSolutionExplorerHierarchy(_DTE vs, out UIHierarchy hier, out UIHierarchyItem sol)
		{
			if (vs == null)
			{
				throw new ArgumentNullException("vs");
			}
			Window win = vs.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer);
			if (win == null)
			{
				throw new Exception();
			}
			win.Activate();
			win.SetFocus();
			hier = win.Object as UIHierarchy;
			sol = hier.UIHierarchyItems.Item(1);
			if (sol == null)
			{
				throw new Exception();
			}
			sol.Select(vsUISelectionType.vsUISelectionTypeSelect);
		}

		/*public static bool IsWebProject(Project project)
		{
			return project.Kind == VsWebSite.PrjKind.prjKindVenusProject;
		}*/

		public static bool IsWebReference(ProjectItem item)
		{
			if (item.ContainingProject.Object is VSProject)
			{
				ProjectItem webrefs = ((VSProject)item.ContainingProject.Object).WebReferencesFolder;
				if (webrefs != null && webrefs.ProjectItems != null)
				{
					foreach (ProjectItem webref in webrefs.ProjectItems)
					{
						if (webref == item)
						{
							return true;
						}
					}
					return false;
				}
				return false;
			}
			return false;
		}

		public static IVsHierarchy GetCurrentSelection(System.IServiceProvider provider)
		{
			uint pitemid = 0;
			return GetCurrentSelection(provider, out pitemid);
		}

		internal sealed class __VSITEMID
		{
			public const uint NIL = 0xFFFFFFFF;
			public const uint ROOT = 0xFFFFFFFE;
			public const uint SELECTION = 0xFFFFFFFD;
		}

		public static IVsHierarchy GetCurrentSelection(System.IServiceProvider provider, out uint pitemid)
		{
			IVsMonitorSelection pSelection =
				(IVsMonitorSelection)provider.GetService(typeof(SVsShellMonitorSelection));
			IntPtr ptrHierchary = IntPtr.Zero;
			IVsMultiItemSelect ppMIS = null;
			IntPtr ppSC = IntPtr.Zero;
			pSelection.GetCurrentSelection(out ptrHierchary, out pitemid, out ppMIS, out ppSC);
			if (ptrHierchary != IntPtr.Zero)
			{
				return (IVsHierarchy)Marshal.GetObjectForIUnknown(ptrHierchary);
			}
			else          
			{
				IVsHierarchy solution = (IVsHierarchy)provider.GetService(typeof(SVsSolution));
				pitemid = __VSITEMID.ROOT;
				return solution;
			}
		}

		public static IVsHierarchy GetVsHierarchy(System.IServiceProvider provider, EnvDTE.Project project)
		{
			IVsSolution solution = (IVsSolution)provider.GetService(typeof(SVsSolution));
			Debug.Assert(solution != null, "couldn't get the solution service");
			if (solution != null)
			{
				if (project != null)
				{
					IVsHierarchy vsHierarchy = null;
					solution.GetProjectOfUniqueName(project.UniqueName, out vsHierarchy);

					return vsHierarchy;
				}
			}
			return null;
		}

		public static object CoCreateInstance(System.IServiceProvider provider, Type type, Type interfaceType)
		{
			ILocalRegistry localRegistry = (ILocalRegistry)provider.GetService(typeof(SLocalRegistry));
			if (localRegistry != null)
			{
				Guid interfaceGuid = interfaceType.GUID;
				IntPtr pObject = IntPtr.Zero;
				localRegistry.CreateInstance(type.GUID,
					null,
					ref interfaceGuid,
					(uint)CLSCTX.CLSCTX_INPROC_SERVER,
					out pObject);
				if (pObject != IntPtr.Zero)
				{
					return Marshal.GetObjectForIUnknown(pObject);
				}
			}
			return null;
		}


		public static void ForEachProject(EnvDTE.Solution solution, Predicate<Project> processAndBreak)
		{
			foreach (Project project in solution.Projects)
			{
				bool shouldBreak = false;
				if (!(project.Object is SolutionFolder))
				{
					shouldBreak = processAndBreak(project);
					if (shouldBreak)
					{
						return;
					}
				}
				shouldBreak = ForEachProjectInternal(project.ProjectItems, processAndBreak);
				if (shouldBreak)
				{
					return;
				}
			}
		}

		private static bool ForEachProjectInternal(ProjectItems items, Predicate<Project> processAndBreak)
		{
			bool shouldBreak = false;
			if (items != null)
			{
				foreach (ProjectItem item in items)
				{
					Project project = item.Object as Project;
					if (project != null)
					{
						if (!(project.Object is SolutionFolder))
						{
							shouldBreak = processAndBreak(project);
							if (shouldBreak)
							{
								break;
							}
						}
					}

					shouldBreak = ForEachProjectInternal(item.ProjectItems, processAndBreak);
					if (shouldBreak)
					{
						break;
					}
				}

			}

			return shouldBreak;
		}

		public static string ReplaceParameters(string value, IDictionaryService dictionary)
		{
			if (dictionary != null)
			{
				int begin = value.IndexOf("$");
				int end = value.IndexOf("$", begin + 1);
				if (begin != -1 && end != -1 && begin != end)
				{
					string key = value.Substring(begin + 1, (end - begin) - 1);
					object newvalue = dictionary.GetValue(key);
					if (newvalue != null)
					{
						return ReplaceParameters(value.Replace("$" + key + "$", newvalue.ToString()), dictionary);
					}
				}
			}
			return value;
		}

		public delegate bool ProcessHierarchyNode(IVsHierarchy hierarchy, uint itemid, int recursionLevel);

		public static void EnumHierarchyItems(IVsHierarchy hierarchy, ProcessHierarchyNode func)
		{
			if (null != hierarchy)
			{
				EnumHierarchyItems(hierarchy, VSConstants.VSITEMID_ROOT, 0, false, func);
			}
		}

		public static void EnumHierarchyItems(IVsSolution solution, ProcessHierarchyNode func)
		{
			if (null != solution)
			{
				IVsHierarchy solutionHierarchy = solution as IVsHierarchy;
				if (null != solutionHierarchy)
				{
					EnumHierarchyItems(solutionHierarchy, VSConstants.VSITEMID_ROOT, 0, true, func);
				}
			}
		}

		private static uint GetItemId(object pvar)
		{
			if (pvar == null) return VSConstants.VSITEMID_NIL;
			if (pvar is int) return (uint)(int)pvar;
			if (pvar is uint) return (uint)pvar;
			if (pvar is short) return (uint)(short)pvar;
			if (pvar is ushort) return (uint)(ushort)pvar;
			if (pvar is long) return (uint)(long)pvar;
			return VSConstants.VSITEMID_NIL;
		}

		private static bool EnumHierarchyItems(IVsHierarchy hierarchy, uint itemid, int recursionLevel, bool hierIsSolution, ProcessHierarchyNode processNodeFunc)
		{
			int hr;
			IntPtr nestedHierarchyObj;
			uint nestedItemId;
			Guid hierGuid = typeof(IVsHierarchy).GUID;

			hr = hierarchy.GetNestedHierarchy(itemid, ref hierGuid, out nestedHierarchyObj, out nestedItemId);
			if (VSConstants.S_OK == hr && IntPtr.Zero != nestedHierarchyObj)
			{
				IVsHierarchy nestedHierarchy = Marshal.GetObjectForIUnknown(nestedHierarchyObj) as IVsHierarchy;
				Marshal.Release(nestedHierarchyObj);                
				if (nestedHierarchy != null)
				{
					if (!EnumHierarchyItems(nestedHierarchy, nestedItemId, recursionLevel, false, processNodeFunc))
					{
						return false;
					}
				}
			}
			else
			{
				object pVar;

				if (!processNodeFunc(hierarchy, itemid, recursionLevel))
				{
					return false;
				}

				recursionLevel++;

				hr = hierarchy.GetProperty(itemid,
						(((hierIsSolution && recursionLevel == 1) ?
								(int)__VSHPROPID.VSHPROPID_FirstVisibleChild : (int)__VSHPROPID.VSHPROPID_FirstChild)),
						out pVar);
				Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(hr);
				if (VSConstants.S_OK == hr)
				{
					uint childId = GetItemId(pVar);
					while (childId != VSConstants.VSITEMID_NIL)
					{
						if (!EnumHierarchyItems(hierarchy, childId, recursionLevel, false, processNodeFunc))
						{
							return false;
						}
						hr = hierarchy.GetProperty(childId,
								(((hierIsSolution && recursionLevel == 1)) ?
										(int)__VSHPROPID.VSHPROPID_NextVisibleSibling : (int)__VSHPROPID.VSHPROPID_NextSibling),
								out pVar);
						if (VSConstants.S_OK == hr)
						{
							childId = GetItemId(pVar);
						}
						else
						{
							Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(hr);
							break;
						}
					}
				}
			}
			return true;
		}
	}
}
