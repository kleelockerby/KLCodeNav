using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ComponentModelHost;

namespace KLCodeNav
{
    public partial class WinformsToolWindowControl : UserControl
    {
        private string RegionPattern => @"^[ \t]*#([Rr]egion|endregion|End Region)";
        
        public Document ActiveDocument { get; set; }
        public DTE Dte { get; set; }
        public DTE2 Dte2 { get; set; }
        public IComponentModel ComponentModel { get; set; }

        public WinformsToolWindowControl()
        {
            InitializeComponent();
        }

        public void GetCodeItems()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if(ActiveDocument != null)
            {
                //ProjectItem projectItem = ActiveDocument.ProjectItem;
                //CreateProjectItems();

                //GetDocumentText(ActiveDocument);

                SetCodeItems codeItems = RetrieveAllCodeItems(ActiveDocument);
                //SetCodeItems filteredCodeItems = new SetCodeItems(codeItems.Where(x => !(x is CodeItemUsingStatement || x is CodeItemNamespace)));
                SetCodeItems filteredCodeItems = codeItems;

                foreach (var codeItem in filteredCodeItems)
                {
                    lbItems.Items.Add($"CodeItems: Name: {codeItem.Name},  Kind: {codeItem.Kind}");
                    lbItems.Items.Add($"\t StartLine: {codeItem.StartLine}");
                    lbItems.Items.Add($"\t EndLine: {codeItem.EndLine}");
                    lbItems.Items.Add($"\t StartOffset: {codeItem.StartOffset}");
                    lbItems.Items.Add($"\t  EndOffset: {codeItem.EndOffset}");
                    lbItems.Items.Add($"\t StartPoint: {codeItem.StartPoint.GetText(10)}");
                    lbItems.Items.Add($"\t EndPoint: {codeItem.EndPoint.GetText(10)}");
                }

            }
        }

        internal SetCodeItems RetrieveAllCodeItems(Document document)
        {
            var codeModel = new CodeModel(document) { IsStale = true };

            BuildCodeItems(codeModel);
            LoadLazyInitializedValues(codeModel);
            return codeModel.CodeItems;
        }

        private void BuildCodeItems(CodeModel codeModel)
        {
            try
            {
                codeModel.IsBuilding = true;
                codeModel.IsStale = false;

                var codeItems = RetrieveCodeItems(codeModel.Document);

                if (codeModel.IsStale)
                {
                    BuildCodeItems(codeModel);
                    return;
                }

                codeModel.CodeItems = codeItems;
                codeModel.IsBuilding = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                codeModel.CodeItems = new SetCodeItems();
                codeModel.IsBuilding = false;
            }
        }

        internal SetCodeItems RetrieveCodeItems(Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var codeItems = new SetCodeItems();

            FileCodeModel fileCodeModel = RetrieveFileCodeModel(document.ProjectItem);
            RetrieveCodeItems(codeItems, fileCodeModel);

            //codeItems.AddRange(RetrieveCodeRegions(document.GetTextDocument()));

            return codeItems;
        }

        private static void RetrieveCodeItems(SetCodeItems codeItems, FileCodeModel fileCodeModel)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (fileCodeModel != null && fileCodeModel.CodeElements != null)
            {
                RetrieveCodeItemsFromElements(codeItems, fileCodeModel.CodeElements);
            }
        }

        private static void RetrieveCodeItemsFromElements(SetCodeItems codeItems, CodeElements codeElements)
        {
            foreach (CodeElement child in codeElements)
            {
                RetrieveCodeItemsRecursively(codeItems, child);
            }
        }

        private static void RetrieveCodeItemsRecursively(SetCodeItems codeItems, CodeElement codeElement)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var parentCodeItem = FactoryCodeItems.CreateCodeItemElement(codeElement);
            if (parentCodeItem != null)
            {
                codeItems.Add(parentCodeItem);
            }

            if (codeElement.Children != null)
            {
                RetrieveCodeItemsFromElements(codeItems, codeElement.Children);
            }
        }

        internal IEnumerable<CodeItemRegion> RetrieveCodeRegions(TextDocument textDocument)
        {
            var editPoints = TextDocumentHelper.FindMatches(textDocument, RegionPattern);
            return RetrieveCodeRegions(editPoints);
        }

        private static IEnumerable<CodeItemRegion> RetrieveCodeRegions(IEnumerable<EditPoint> editPoints)
        {
            var regionStack = new Stack<CodeItemRegion>();
            var codeItems = new List<CodeItemRegion>();

            foreach (var cursor in editPoints)
            {
                EditPoint eolCursor = cursor.CreateEditPoint();
                eolCursor.EndOfLine();
                string regionText = cursor.GetText(eolCursor).TrimStart(' ', '\t');

                if (regionText.StartsWith(RegionHelper.GetRegionTagText(cursor)))
                {
                    string regionName = RegionHelper.GetRegionName(cursor, regionText);

                    regionStack.Push(new CodeItemRegion
                    {
                        Name = regionName,
                        StartLine = cursor.Line,
                        StartOffset = cursor.AbsoluteCharOffset,
                        StartPoint = cursor.CreateEditPoint()
                    });
                }
                else if (regionText.StartsWith(RegionHelper.GetEndRegionTagText(cursor)))
                {
                    if (regionStack.Count > 0)
                    {
                        CodeItemRegion region = regionStack.Pop();
                        region.EndLine = eolCursor.Line;
                        region.EndOffset = eolCursor.AbsoluteCharOffset;
                        region.EndPoint = eolCursor.CreateEditPoint();

                        codeItems.Add(region);
                    }
                    else
                    {
                        return Enumerable.Empty<CodeItemRegion>();
                    }
                }
            }
            return codeItems;
        }

        private void LoadLazyInitializedValues(CodeModel codeModel)
        {
            try
            {
                foreach (var codeItem in codeModel.CodeItems)
                {
                    codeItem.LoadLazyInitializedValues();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to load lazy initialized values for {codeModel.Document.FullName}");
            }
        }

        private FileCodeModel RetrieveFileCodeModel(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (projectItem == null)
            {
                return null;
            }

            if (projectItem.FileCodeModel != null)
            {
                return projectItem.FileCodeModel;
            }
            return null;
         
        }

        public void CreateProjectItems()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            EnvDTE.Solution sln = Dte.Solution;

            foreach (EnvDTE.Project project in sln.Projects)
            {
                lbItems.Items.Add($"Projct Name: {project.Name}");

                ProjectItems projectItems = project.ProjectItems;
                ScanProjectItems(projectItems, 1);

                
            }
        }

        private void ScanProjectItems(ProjectItems projectItems, int level)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (ProjectItem projectItem in projectItems)
            {
                
                
                string itemName = "Name: " + projectItem.Name + " " + level.ToString();
                lbItems.Items.Add($"\t {itemName}");

                ProjectItems projectItems2 = projectItem.ProjectItems;
                if (projectItems2 != null)
                {
                    ScanProjectItems(projectItems2, level++);
                }
            }
        }

        private void GetDocumentText(Document document)
        {
            string documentText = DocumentHelper.GetText(document);
            Console.WriteLine(documentText);

        }

    }
}
