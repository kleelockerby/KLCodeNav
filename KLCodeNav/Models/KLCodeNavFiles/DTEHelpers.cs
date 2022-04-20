﻿using System;
using System.IO;
using System.Collections.Generic;
using EnvDTE;
using EnvDTE80;
using Msiler.AssemblyParser;
using Microsoft.VisualStudio.Shell;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.Build.Evaluation;
using Project = Microsoft.Build.Evaluation.Project;

namespace Msiler.Helpers
{
    public static class DteHelpers
    {
        private static readonly Regex GenericPartRegex = new Regex(@"(<.*>)|(\(Of .*\))", RegexOptions.Compiled);

        public static DTE2 GetDte() {
            var provider = ServiceProvider.GlobalProvider;
            var vs = (DTE2)provider.GetService(typeof(DTE));

            if (vs == null) {
                throw new InvalidOperationException("DTE not found.");
            }
            return vs;
        }

        private static string GetFullPath(string path, string basePath) {
            bool isAbsolute = Path.IsPathRooted(path);
            if (isAbsolute) {
                return path;
            }
            string saved = Environment.CurrentDirectory;
            Environment.CurrentDirectory = basePath;
            try {
                return Path.GetFullPath(path);
            } finally {
                Environment.CurrentDirectory = saved;
            }
        }

        public static List<EnvDTE.Project> GetProjects(DTE2 dte)
        {
            var projects = dte.Solution.Projects;
            var list = new List<EnvDTE.Project>();
            var item = projects.GetEnumerator();

            while (item.MoveNext())
            {
                if (!(item.Current is EnvDTE.Project project))
                    continue;

                if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                    list.AddRange(GetSolutionFolderProjects(project));
                else
                    list.Add(project);
            }

            return list;
        }
        private static IEnumerable<EnvDTE.Project> GetSolutionFolderProjects(EnvDTE.Project solutionFolder)
        {
            var list = new List<EnvDTE.Project>();
            for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
            {
                var subProject = solutionFolder.ProjectItems.Item(i).SubProject;
                if (subProject == null)
                    continue;
 
                if (subProject.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                    list.AddRange(GetSolutionFolderProjects(subProject));
                else
                    list.Add(subProject);
            }
 
            return list;
        }


        private static string GetPropertyValueFrom(string projectFile, string propertyName, string solutionDir, string configurationName)
        {
            using (var projectCollection = new ProjectCollection())
            {
                var p = new Project(projectFile, null, null,  projectCollection, ProjectLoadSettings.Default);

                p.SetProperty("Configuration", configurationName);
                p.SetProperty("SolutionDir", solutionDir);
                p.SetGlobalProperty("SolutionDir", solutionDir);
                p.ReevaluateIfNecessary();
                return p.Properties.Where(x => x.Name == propertyName).Select(x => x.EvaluatedValue).SingleOrDefault();
            }
        }

        public static string GetOutputAssemblyFileName()
        {
            var dte = GetDte();
            var sb = (SolutionBuild2)dte.Solution.SolutionBuild;

            if (!(sb.StartupProjects is Array projects))
                return null;

            if (dte.Solution?.FullName == null)
                return null;

            var activeProjectPath = projects.GetValue(0);
            var activeProject = GetProjects(dte).FirstOrDefault(proj => proj.UniqueName == (string)activeProjectPath);

            if (!(activeProject is EnvDTE.Project project))
                return null; 

            var activeConf = project.ConfigurationManager.ActiveConfiguration;
            var solutionDir = Path.GetDirectoryName(dte.Solution.FullName) + Path.DirectorySeparatorChar;
            var outputFileName = new DirectoryInfo(GetPropertyValueFrom(project.FileName, "TargetPath", solutionDir, activeConf.ConfigurationName));
            // normalize output path
            return outputFileName.FullName;
        }

        public static AssemblyMethodSignature GetSignature(VirtualPoint point, FileCodeModel2 fcm) {
            var codeFunction = GetCodeFunction(fcm, point);

            if (codeFunction == null) {
                return null;
            }
            // init and remove generic part
            string funcName = GenericPartRegex.Replace(codeFunction.FullName, String.Empty);
            IEnumerable<CodeTypeRef> paramsList;

            switch (codeFunction.FunctionKind)
            {
                case vsCMFunction.vsCMFunctionPropertyGet:
                case vsCMFunction.vsCMFunctionPropertySet:

                    string prefix = codeFunction.FunctionKind == vsCMFunction.vsCMFunctionPropertyGet ? "get_" : "set_";

                    int lastDot = funcName.LastIndexOf(".", StringComparison.Ordinal);
                    funcName = funcName.Substring(0, lastDot + 1) + prefix + funcName.Substring(lastDot + 1);

                    paramsList = codeFunction.FunctionKind == vsCMFunction.vsCMFunctionPropertyGet
                        ? new List<CodeTypeRef>()
                        : new List<CodeTypeRef> { codeFunction.Type };
                    break;
                default:
                    if (codeFunction.FunctionKind == vsCMFunction.vsCMFunctionConstructor)
                    {
                        int lastIndex = funcName.LastIndexOf(codeFunction.Name, StringComparison.Ordinal);
                        funcName = funcName.Substring(0, lastIndex) + ".ctor";
                    }
                    paramsList = codeFunction.Parameters.OfType<CodeParameter>().Select(p => p.Type);
                    break;
            }
            return new AssemblyMethodSignature(funcName, paramsList.Select(ProcessTypeRef).ToList());
        }

        private static string ProcessTypeRef(CodeTypeRef typeRef) {
            if (typeRef.TypeKind != vsCMTypeRef.vsCMTypeRefArray)
                return typeRef.AsFullName;

            int rank = typeRef.Rank;
            string fullType = typeRef.ElementType.AsFullName + $"[{new String(',', rank - 1)}]";
            return fullType;
        }

        private static CodeFunction GetCodeFunction(FileCodeModel2 fcm, TextPoint point) {
            try {
                var element = fcm.CodeElementFromPoint(point, vsCMElement.vsCMElementFunction);
                return (CodeFunction)element;
            } catch {
                return null;
            }
        }
    }
}
