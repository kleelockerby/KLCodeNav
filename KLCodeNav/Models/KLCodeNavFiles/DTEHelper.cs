// Decompiled with JetBrains decompiler
// Type: Microsoft.PowerCommands.Common.DTEHelper
// Assembly: PowerCommands, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// MVID: 286D8100-8082-4070-9DAE-865C6C9E58E8
// Assembly location: C:\Users\klockerby\Downloads\PowerCommands\PowerCommands.dll

using EnvDTE;
using EnvDTE80;
using Microsoft.PowerCommands.Linq;
using Microsoft.PowerCommands.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.PowerCommands.Common
{
  public class DTEHelper
  {
    public static void RestartVS(DTE dte)
    {
      System.Diagnostics.Process process = new System.Diagnostics.Process();
      string[] commandLineArgs = Environment.GetCommandLineArgs();
      process.StartInfo.FileName = Path.GetFullPath(commandLineArgs[0]);
      process.StartInfo.Arguments = string.Join(" ", commandLineArgs, 1, commandLineArgs.Length - 1);
      process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
      process.Start();
      dte.Quit();
    }

    public static int CompileProject(Project project) => DTEHelper.CompileProject(project, out string _);

    public static int CompileProject(Project project, out string assemblyFile)
    {
      project.DTE.Solution.SolutionBuild.BuildProject(project.DTE.Solution.SolutionBuild.ActiveConfiguration.Name, project.UniqueName, true);
      if (project.DTE.Solution.SolutionBuild.LastBuildInfo == 0)
      {
        string path1_1 = project.ConfigurationManager.ActiveConfiguration.Properties.Item((object) "OutputPath").Value.ToString();
        string path1_2 = project.Properties.Item((object) "LocalPath").Value.ToString();
        string path2 = project.Properties.Item((object) "OutputFileName").Value.ToString();
        assemblyFile = Path.Combine(path1_2, Path.Combine(path1_1, path2));
      }
      else
        assemblyFile = (string) null;
      return project.DTE.Solution.SolutionBuild.LastBuildInfo;
    }

    public static IEnumerable<UIHierarchyItem> GetUIProjectAndSolutionFoldersNodes(
      Solution solution)
    {
      string Names = solution.Properties.Item((object) "Name").Value.ToString();
      return new UIHierarchyItemIterator(((DTE2) solution.DTE).ToolWindows.SolutionExplorer.GetItem(Names).UIHierarchyItems).Where<UIHierarchyItem>((Func<UIHierarchyItem, bool>) (item =>
      {
        if (item.Object is Project)
          return true;
        return item.Object is ProjectItem && ((ProjectItem) item.Object).Object is Project;
      })).Select<UIHierarchyItem, UIHierarchyItem>((Func<UIHierarchyItem, UIHierarchyItem>) (item => item));
    }

    public static bool IsUISolutionNode(UIHierarchyItem item)
    {
      if (item.Object is Project && ((Project) item.Object).Kind == "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}")
        return true;
      return item.Object is ProjectItem && ((ProjectItem) item.Object).Object is Project && ((Project) ((ProjectItem) item.Object).Object).Kind == "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";
    }

    public static bool IsProjectNode(UIHierarchyItem item)
    {
      if (item.Object is Project && ((Project) item.Object).Kind != "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}")
        return true;
      return item.Object is ProjectItem && ((ProjectItem) item.Object).Object is Project && ((Project) ((ProjectItem) item.Object).Object).Kind != "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";
    }

    public static IEnumerable<UIHierarchyItem> GetUIProjectNodes(
      Solution solution)
    {
      string Names = solution.Properties.Item((object) "Name").Value.ToString();
      return DTEHelper.GetUIProjectNodes(((DTE2) solution.DTE).ToolWindows.SolutionExplorer.GetItem(Names).UIHierarchyItems);
    }

    public static IEnumerable<UIHierarchyItem> GetUIProjectNodes(
      UIHierarchyItems root)
    {
      return new UIHierarchyItemIterator(root).Where<UIHierarchyItem>((Func<UIHierarchyItem, bool>) (item =>
      {
        if (item.Object is Project && ((Project) item.Object).Kind != "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}")
          return true;
        return item.Object is ProjectItem && ((ProjectItem) item.Object).Object is Project && ((Project) ((ProjectItem) item.Object).Object).Kind != "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";
      })).Select<UIHierarchyItem, UIHierarchyItem>((Func<UIHierarchyItem, UIHierarchyItem>) (item => item));
    }

    public static void OpenDocument(DTE dte, IDocumentInfo docInfo)
    {
      if (!File.Exists(docInfo.DocumentPath))
        return;
      Window window = dte.OpenFile(docInfo.DocumentViewKind, docInfo.DocumentPath);
      if (window == null)
        return;
      window.Visible = true;
      window.Activate();
      if (docInfo.CursorLine <= 1 && docInfo.CursorColumn <= 1 || !(window.Document.Selection is TextSelection selection))
        return;
      selection.MoveTo(docInfo.CursorLine, docInfo.CursorColumn, true);
      selection.Cancel();
    }
  }
}
