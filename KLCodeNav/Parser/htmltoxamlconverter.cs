using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Xml;

namespace KLCodeNav
{
    public static class HtmlToXamlConverter
    {
        public static string ConvertHtmlToXaml(string htmlString, bool asFlowDocument)
        {
            XmlElement htmlElement = HtmlParser.ParseHtml(htmlString);

            string rootElementName = asFlowDocument ? HtmlToXamlConverter.Xaml_FlowDocument : HtmlToXamlConverter.Xaml_Section;

            XmlDocument xamlTree = new XmlDocument();
            XmlElement xamlFlowDocumentElement = xamlTree.CreateElement(null, rootElementName, _xamlNamespace);

            CssStylesheet stylesheet = new CssStylesheet(htmlElement);

            List<XmlElement> sourceContext = new List<XmlElement>(10);

            InlineFragmentParentElement = null;

            AddBlock(xamlFlowDocumentElement, htmlElement, new Hashtable(), stylesheet, sourceContext);

            if (!asFlowDocument)
            {
                xamlFlowDocumentElement = ExtractInlineFragment(xamlFlowDocumentElement);
            }

            xamlFlowDocumentElement.SetAttribute("xml:space", "preserve");
            string xaml = xamlFlowDocumentElement.OuterXml;

            return xaml;
        }

        public static string GetAttribute(XmlElement element, string attributeName)
        {
            attributeName = attributeName.ToLower();

            for (int i = 0; i < element.Attributes.Count; i++)
            {
                if (element.Attributes[i].Name.ToLower() == attributeName)
                {
                    return element.Attributes[i].Value;
                }
            }

            return null;
        }

        internal static string UnQuote(string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\"") || value.StartsWith("'") && value.EndsWith("'"))
            {
                value = value.Substring(1, value.Length - 2).Trim();
            }

            return value;
        }

        private static XmlNode AddBlock(XmlElement xamlParentElement, XmlNode htmlNode, Hashtable inheritedProperties, CssStylesheet stylesheet, List<XmlElement> sourceContext)
        {
            if (htmlNode is XmlComment)
            {
                DefineInlineFragmentParent((XmlComment) htmlNode, null);
            }
            else if (htmlNode is XmlText)
            {
                htmlNode = AddImplicitParagraph(xamlParentElement, htmlNode, inheritedProperties, stylesheet, sourceContext);
            }
            else if (htmlNode is XmlElement)
            {
                XmlElement htmlElement = (XmlElement) htmlNode;

                string htmlElementName = htmlElement.LocalName;         
                string htmlElementNamespace = htmlElement.NamespaceURI;

                if (htmlElementNamespace != HtmlParser.XhtmlNamespace)
                {
                    return htmlElement;
                }

                sourceContext.Add(htmlElement);

                htmlElementName = htmlElementName.ToLower();

                switch (htmlElementName)
                {
                    case "html":
                    case "body":
                    case "div":
                    case "form":        
                    case "pre":       
                    case "blockquote":
                    case "caption":
                    case "center":
                    case "cite":
                        AddSection(xamlParentElement, htmlElement, inheritedProperties, stylesheet, sourceContext);
                        break;

                    case "p":
                    case "h1":
                    case "h2":
                    case "h3":
                    case "h4":
                    case "h5":
                    case "h6":
                    case "nsrtitle":
                    case "textarea":
                    case "dd":  
                    case "dl":  
                    case "dt":  
                    case "tt":  
                        AddParagraph(xamlParentElement, htmlElement, inheritedProperties, stylesheet, sourceContext);
                        break;

                    case "ol":
                    case "ul":
                    case "dir":      
                    case "menu":      
                        AddList(xamlParentElement, htmlElement, inheritedProperties, stylesheet, sourceContext);
                        break;
                    case "li":
                        htmlNode = AddOrphanListItems(xamlParentElement, htmlElement, inheritedProperties, stylesheet, sourceContext);
                        break;

                    case "img":
                        AddImage(xamlParentElement, htmlElement, inheritedProperties, stylesheet, sourceContext);
                        break;

                    case "table":
                        AddTable(xamlParentElement, htmlElement, inheritedProperties, stylesheet, sourceContext);
                        break;

                    case "tbody":
                    case "tfoot":
                    case "thead":
                    case "tr":
                    case "td":
                    case "th":
                        goto default;              

                    case "style":          
                    case "meta":
                    case "head":
                    case "title":
                    case "script":
                        break;

                    default:
                        htmlNode = AddImplicitParagraph(xamlParentElement, htmlElement, inheritedProperties, stylesheet, sourceContext);
                        break;
                }

                Debug.Assert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1] == htmlElement);
                sourceContext.RemoveAt(sourceContext.Count - 1);
            }

            return htmlNode;
        }

        private static void AddBreak(XmlElement xamlParentElement, string htmlElementName)
        {
            XmlElement xamlLineBreak = xamlParentElement.OwnerDocument.CreateElement( null, HtmlToXamlConverter.Xaml_LineBreak, _xamlNamespace);
            xamlParentElement.AppendChild(xamlLineBreak);
            if (htmlElementName == "hr")
            {
                XmlText xamlHorizontalLine = xamlParentElement.OwnerDocument.CreateTextNode("----------------------");
                xamlParentElement.AppendChild(xamlHorizontalLine);
                xamlLineBreak = xamlParentElement.OwnerDocument.CreateElement( null, HtmlToXamlConverter.Xaml_LineBreak, _xamlNamespace);
                xamlParentElement.AppendChild(xamlLineBreak);
            }
        }

        private static void AddSection(XmlElement xamlParentElement, XmlElement htmlElement, Hashtable inheritedProperties, CssStylesheet stylesheet, List<XmlElement> sourceContext)
        {
            bool htmlElementContainsBlocks = false;
            for (XmlNode htmlChildNode = htmlElement.FirstChild; htmlChildNode != null; htmlChildNode = htmlChildNode.NextSibling)
            {
                if (htmlChildNode is XmlElement)
                {
                    string htmlChildName = ((XmlElement) htmlChildNode).LocalName.ToLower();
                    if (HtmlSchema.IsBlockElement(htmlChildName))
                    {
                        htmlElementContainsBlocks = true;
                        break;
                    }
                }
            }

            if (!htmlElementContainsBlocks)
            {
                AddParagraph(xamlParentElement, htmlElement, inheritedProperties, stylesheet, sourceContext);
            }
            else
            {
                Hashtable localProperties;
                Hashtable currentProperties = GetElementProperties(htmlElement, inheritedProperties, out localProperties, stylesheet, sourceContext);

                XmlElement xamlElement = xamlParentElement.OwnerDocument.CreateElement( null, HtmlToXamlConverter.Xaml_Section, _xamlNamespace);
                ApplyLocalProperties(xamlElement, localProperties, true);

                if (!xamlElement.HasAttributes)
                {
                    xamlElement = xamlParentElement;
                }

                for (XmlNode htmlChildNode = htmlElement.FirstChild; htmlChildNode != null; htmlChildNode = htmlChildNode != null ? htmlChildNode.NextSibling : null)
                {
                    htmlChildNode = AddBlock(xamlElement, htmlChildNode, currentProperties, stylesheet, sourceContext);
                }

                if (xamlElement != xamlParentElement)
                {
                    xamlParentElement.AppendChild(xamlElement);
                }
            }
        }

        private static void AddParagraph(XmlElement xamlParentElement, XmlElement htmlElement, Hashtable inheritedProperties, CssStylesheet stylesheet, List<XmlElement> sourceContext)
        {
            Hashtable localProperties;
            Hashtable currentProperties = GetElementProperties(htmlElement, inheritedProperties, out localProperties, stylesheet, sourceContext);

            XmlElement xamlElement = xamlParentElement.OwnerDocument.CreateElement( null, HtmlToXamlConverter.Xaml_Paragraph, _xamlNamespace);
            ApplyLocalProperties(xamlElement, localProperties, true);

            for (XmlNode htmlChildNode = htmlElement.FirstChild; htmlChildNode != null; htmlChildNode = htmlChildNode.NextSibling)
            {
                AddInline(xamlElement, htmlChildNode, currentProperties, stylesheet, sourceContext);
            }

            xamlParentElement.AppendChild(xamlElement);
        }

        private static XmlNode AddImplicitParagraph(XmlElement xamlParentElement, XmlNode htmlNode, Hashtable inheritedProperties, CssStylesheet stylesheet, List<XmlElement> sourceContext)
        {
            XmlElement xamlParagraph = xamlParentElement.OwnerDocument.CreateElement( null, HtmlToXamlConverter.Xaml_Paragraph, _xamlNamespace);
            XmlNode lastNodeProcessed = null;
            while (htmlNode != null)
            {
                if (htmlNode is XmlComment)
                {
                    DefineInlineFragmentParent((XmlComment) htmlNode, null);
                }
                else if (htmlNode is XmlText)
                {
                    if (htmlNode.Value.Trim().Length > 0)
                    {
                        AddTextRun(xamlParagraph, htmlNode.Value);
                    }
                }
                else if (htmlNode is XmlElement)
                {
                    string htmlChildName = ((XmlElement) htmlNode).LocalName.ToLower();
                    if (HtmlSchema.IsBlockElement(htmlChildName))
                    {
                        break;
                    }
                    else
                    {
                        AddInline(xamlParagraph, (XmlElement) htmlNode, inheritedProperties, stylesheet, sourceContext);
                    }
                }

                lastNodeProcessed = htmlNode;
                htmlNode = htmlNode.NextSibling;
            }

            if (xamlParagraph.FirstChild != null)
            {
                xamlParentElement.AppendChild(xamlParagraph);
            }

            return lastNodeProcessed;
        }

        private static void AddInline(XmlElement xamlParentElement, XmlNode htmlNode, Hashtable inheritedProperties, CssStylesheet stylesheet, List<XmlElement> sourceContext)
        {
            if (htmlNode is XmlComment)
            {
                DefineInlineFragmentParent((XmlComment) htmlNode, xamlParentElement);
            }
            else if (htmlNode is XmlText)
            {
                AddTextRun(xamlParentElement, htmlNode.Value);
            }
            else if (htmlNode is XmlElement)
            {
                XmlElement htmlElement = (XmlElement) htmlNode;

                if (htmlElement.NamespaceURI != HtmlParser.XhtmlNamespace)
                {
                    return;    
                }

                string htmlElementName = htmlElement.LocalName.ToLower();

                sourceContext.Add(htmlElement);

                switch (htmlElementName)
                {
                    case "a":
                        AddHyperlink(xamlParentElement, htmlElement, inheritedProperties, stylesheet, sourceContext);
                        break;
                    case "img":
                        AddImage(xamlParentElement, htmlElement, inheritedProperties, stylesheet, sourceContext);
                        break;
                    case "br":
                    case "hr":
                        AddBreak(xamlParentElement, htmlElementName);
                        break;
                    default:
                        if (HtmlSchema.IsInlineElement(htmlElementName) || HtmlSchema.IsBlockElement(htmlElementName))
                        {
                            AddSpanOrRun(xamlParentElement, htmlElement, inheritedProperties, stylesheet, sourceContext);
                        }

                        break;
                }
                Debug.Assert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1] == htmlElement);
                sourceContext.RemoveAt(sourceContext.Count - 1);
            }
        }

        private static void AddSpanOrRun(XmlElement xamlParentElement, XmlElement htmlElement, Hashtable inheritedProperties, CssStylesheet stylesheet, List<XmlElement> sourceContext)
        {
            bool elementHasChildren = false;
            for (XmlNode htmlNode = htmlElement.FirstChild; htmlNode != null; htmlNode = htmlNode.NextSibling)
            {
                if (htmlNode is XmlElement)
                {
                    string htmlChildName = ((XmlElement) htmlNode).LocalName.ToLower();
                    if (HtmlSchema.IsInlineElement(htmlChildName) || HtmlSchema.IsBlockElement(htmlChildName) ||
                        htmlChildName == "img" || htmlChildName == "br" || htmlChildName == "hr")
                    {
                        elementHasChildren = true;
                        break;
                    }
                }
            }

            string xamlElementName = elementHasChildren ? HtmlToXamlConverter.Xaml_Span : HtmlToXamlConverter.Xaml_Run;

            Hashtable localProperties;
            Hashtable currentProperties = GetElementProperties(htmlElement, inheritedProperties, out localProperties, stylesheet, sourceContext);

            XmlElement xamlElement = xamlParentElement.OwnerDocument.CreateElement( null, xamlElementName, _xamlNamespace);
            ApplyLocalProperties(xamlElement, localProperties, false);

            for (XmlNode htmlChildNode = htmlElement.FirstChild; htmlChildNode != null; htmlChildNode = htmlChildNode.NextSibling)
            {
                AddInline(xamlElement, htmlChildNode, currentProperties, stylesheet, sourceContext);
            }

            xamlParentElement.AppendChild(xamlElement);
        }

        private static void AddTextRun(XmlElement xamlElement, string textData)
        {
            for (int i = 0; i < textData.Length; i++)
            {
                if (Char.IsControl(textData[i]))
                {
                    textData = textData.Remove(i--, 1);        
                }
            }

            textData = textData.Replace((char) 160, ' ');

            if (textData.Length > 0)
            {
                xamlElement.AppendChild(xamlElement.OwnerDocument.CreateTextNode(textData));
            }
        }

        private static void AddHyperlink(XmlElement xamlParentElement, XmlElement htmlElement, Hashtable inheritedProperties, CssStylesheet stylesheet, List<XmlElement> sourceContext)
        {
            string href = GetAttribute(htmlElement, "href");
            if (href == null)
            {
                AddSpanOrRun(xamlParentElement, htmlElement, inheritedProperties, stylesheet, sourceContext);
            }
            else
            {
                Hashtable localProperties;
                Hashtable currentProperties = GetElementProperties(htmlElement, inheritedProperties, out localProperties, stylesheet, sourceContext);

                XmlElement xamlElement = xamlParentElement.OwnerDocument.CreateElement( null, HtmlToXamlConverter.Xaml_Hyperlink, _xamlNamespace);
                ApplyLocalProperties(xamlElement, localProperties, false);

                string[] hrefParts = href.Split(new char[] {'#'});
                if (hrefParts.Length > 0 && hrefParts[0].Trim().Length > 0)
                {
                    xamlElement.SetAttribute(HtmlToXamlConverter.Xaml_Hyperlink_NavigateUri, hrefParts[0].Trim());
                }

                if (hrefParts.Length == 2 && hrefParts[1].Trim().Length > 0)
                {
                    xamlElement.SetAttribute(HtmlToXamlConverter.Xaml_Hyperlink_TargetName, hrefParts[1].Trim());
                }

                for (XmlNode htmlChildNode = htmlElement.FirstChild; htmlChildNode != null; htmlChildNode = htmlChildNode.NextSibling)
                {
                    AddInline(xamlElement, htmlChildNode, currentProperties, stylesheet, sourceContext);
                }

                xamlParentElement.AppendChild(xamlElement);
            }
        }

        private static XmlElement InlineFragmentParentElement;

        private static void DefineInlineFragmentParent(XmlComment htmlComment, XmlElement xamlParentElement)
        {
            if (htmlComment.Value == "StartFragment")
            {
                InlineFragmentParentElement = xamlParentElement;
            }
            else if (htmlComment.Value == "EndFragment")
            {
                if (InlineFragmentParentElement == null && xamlParentElement != null)
                {
                    InlineFragmentParentElement = xamlParentElement;
                }
            }
        }

        private static XmlElement ExtractInlineFragment(XmlElement xamlFlowDocumentElement)
        {
            if (InlineFragmentParentElement != null)
            {
                if (InlineFragmentParentElement.LocalName == HtmlToXamlConverter.Xaml_Span)
                {
                    xamlFlowDocumentElement = InlineFragmentParentElement;
                }
                else
                {
                    xamlFlowDocumentElement = xamlFlowDocumentElement.OwnerDocument.CreateElement( null, HtmlToXamlConverter.Xaml_Span, _xamlNamespace);
                    while (InlineFragmentParentElement.FirstChild != null)
                    {
                        XmlNode copyNode = InlineFragmentParentElement.FirstChild;
                        InlineFragmentParentElement.RemoveChild(copyNode);
                        xamlFlowDocumentElement.AppendChild(copyNode);
                    }
                }
            }

            return xamlFlowDocumentElement;
        }

        private static void AddImage(XmlElement xamlParentElement, XmlElement htmlElement, Hashtable inheritedProperties, CssStylesheet stylesheet, List<XmlElement> sourceContext)
        {
        }

        private static void AddList(XmlElement xamlParentElement, XmlElement htmlListElement, Hashtable inheritedProperties, CssStylesheet stylesheet, List<XmlElement> sourceContext)
        {
            string htmlListElementName = htmlListElement.LocalName.ToLower();

            Hashtable localProperties;
            Hashtable currentProperties = GetElementProperties(htmlListElement, inheritedProperties, out localProperties, stylesheet, sourceContext);

            XmlElement xamlListElement = xamlParentElement.OwnerDocument.CreateElement(null, Xaml_List, _xamlNamespace);

            if (htmlListElementName == "ol")
            {
                xamlListElement.SetAttribute(HtmlToXamlConverter.Xaml_List_MarkerStyle, Xaml_List_MarkerStyle_Decimal);
            }
            else
            {
                xamlListElement.SetAttribute(HtmlToXamlConverter.Xaml_List_MarkerStyle, Xaml_List_MarkerStyle_Disc);
            }

            ApplyLocalProperties(xamlListElement, localProperties, true);

            for (XmlNode htmlChildNode = htmlListElement.FirstChild; htmlChildNode != null; htmlChildNode = htmlChildNode.NextSibling)
            {
                if (htmlChildNode is XmlElement && htmlChildNode.LocalName.ToLower() == "li")
                {
                    sourceContext.Add((XmlElement) htmlChildNode);
                    AddListItem(xamlListElement, (XmlElement) htmlChildNode, currentProperties, stylesheet, sourceContext);
                    Debug.Assert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1] == htmlChildNode);
                    sourceContext.RemoveAt(sourceContext.Count - 1);
                }
                else
                {
                }
            }

            if (xamlListElement.HasChildNodes)
            {
                xamlParentElement.AppendChild(xamlListElement);
            }
        }

        private static XmlElement AddOrphanListItems(XmlElement xamlParentElement, XmlElement htmlLIElement, Hashtable inheritedProperties, CssStylesheet stylesheet, List<XmlElement> sourceContext)
        {
            Debug.Assert(htmlLIElement.LocalName.ToLower() == "li");

            XmlElement lastProcessedListItemElement = null;

            XmlNode xamlListItemElementPreviousSibling = xamlParentElement.LastChild;
            XmlElement xamlListElement;
            if (xamlListItemElementPreviousSibling != null && xamlListItemElementPreviousSibling.LocalName == Xaml_List)
            {
                xamlListElement = (XmlElement) xamlListItemElementPreviousSibling;
            }
            else
            {
                xamlListElement = xamlParentElement.OwnerDocument.CreateElement(null, Xaml_List, _xamlNamespace);
                xamlParentElement.AppendChild(xamlListElement);
            }

            XmlNode htmlChildNode = htmlLIElement;
            string htmlChildNodeName = htmlChildNode == null ? null : htmlChildNode.LocalName.ToLower();

            while (htmlChildNode != null && htmlChildNodeName == "li")
            {
                AddListItem(xamlListElement, (XmlElement) htmlChildNode, inheritedProperties, stylesheet, sourceContext);
                lastProcessedListItemElement = (XmlElement) htmlChildNode;
                htmlChildNode = htmlChildNode.NextSibling;
                htmlChildNodeName = htmlChildNode == null ? null : htmlChildNode.LocalName.ToLower();
            }

            return lastProcessedListItemElement;
        }

        private static void AddListItem(XmlElement xamlListElement, XmlElement htmlLIElement, Hashtable inheritedProperties, CssStylesheet stylesheet, List<XmlElement> sourceContext)
        {
            Debug.Assert(xamlListElement != null);
            Debug.Assert(xamlListElement.LocalName == Xaml_List);
            Debug.Assert(htmlLIElement != null);
            Debug.Assert(htmlLIElement.LocalName.ToLower() == "li");
            Debug.Assert(inheritedProperties != null);

            Hashtable localProperties;
            Hashtable currentProperties = GetElementProperties(htmlLIElement, inheritedProperties, out localProperties, stylesheet, sourceContext);

            XmlElement xamlListItemElement = xamlListElement.OwnerDocument.CreateElement(null, Xaml_ListItem, _xamlNamespace);

            ApplyLocalProperties(xamlListItemElement, localProperties, true);

            for (XmlNode htmlChildNode = htmlLIElement.FirstChild; htmlChildNode != null; htmlChildNode = htmlChildNode != null ? htmlChildNode.NextSibling : null)
            {
                htmlChildNode = AddBlock(xamlListItemElement, htmlChildNode, currentProperties, stylesheet, sourceContext);
            }

            xamlListElement.AppendChild(xamlListItemElement);
        }

        private static void AddTable(XmlElement xamlParentElement, XmlElement htmlTableElement, Hashtable inheritedProperties, CssStylesheet stylesheet, List<XmlElement> sourceContext)
        {
            Debug.Assert(htmlTableElement.LocalName.ToLower() == "table");
            Debug.Assert(xamlParentElement != null);
            Debug.Assert(inheritedProperties != null);

            Hashtable localProperties;
            Hashtable currentProperties = GetElementProperties(htmlTableElement, inheritedProperties, out localProperties, stylesheet, sourceContext);

            XmlElement singleCell = GetCellFromSingleCellTable(htmlTableElement);

            if (singleCell != null)
            {
                sourceContext.Add(singleCell);

                for (XmlNode htmlChildNode = singleCell.FirstChild; htmlChildNode != null; htmlChildNode = htmlChildNode != null ? htmlChildNode.NextSibling : null)
                {
                    htmlChildNode = AddBlock(xamlParentElement, htmlChildNode, currentProperties, stylesheet, sourceContext);
                }

                Debug.Assert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1] == singleCell);
                sourceContext.RemoveAt(sourceContext.Count - 1);
            }
            else
            {
                XmlElement xamlTableElement = xamlParentElement.OwnerDocument.CreateElement(null, Xaml_Table, _xamlNamespace);
                ApplyLocalProperties(xamlTableElement, localProperties, true);
                ArrayList columnStarts = AnalyzeTableStructure(htmlTableElement, stylesheet);

                AddColumnInformation(htmlTableElement, xamlTableElement, columnStarts, currentProperties, stylesheet, sourceContext);

                XmlNode htmlChildNode = htmlTableElement.FirstChild;

                while (htmlChildNode != null)
                {
                    string htmlChildName = htmlChildNode.LocalName.ToLower();

                    if (htmlChildName == "tbody" || htmlChildName == "thead" || htmlChildName == "tfoot")
                    {
                        XmlElement xamlTableBodyElement = xamlTableElement.OwnerDocument.CreateElement(null, Xaml_TableRowGroup, _xamlNamespace);
                        xamlTableElement.AppendChild(xamlTableBodyElement);

                        sourceContext.Add((XmlElement) htmlChildNode);

                        Hashtable tbodyElementLocalProperties;
                        Hashtable tbodyElementCurrentProperties = GetElementProperties((XmlElement) htmlChildNode, currentProperties, out tbodyElementLocalProperties, stylesheet, sourceContext);
                        AddTableRowsToTableBody(xamlTableBodyElement, htmlChildNode.FirstChild, tbodyElementCurrentProperties, columnStarts, stylesheet, sourceContext);
                        if (xamlTableBodyElement.HasChildNodes)
                        {
                            xamlTableElement.AppendChild(xamlTableBodyElement);
                        }

                        Debug.Assert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1] == htmlChildNode);
                        sourceContext.RemoveAt(sourceContext.Count - 1);

                        htmlChildNode = htmlChildNode.NextSibling;
                    }
                    else if (htmlChildName == "tr")
                    {
                        XmlElement xamlTableBodyElement = xamlTableElement.OwnerDocument.CreateElement(null, Xaml_TableRowGroup, _xamlNamespace);

                        htmlChildNode = AddTableRowsToTableBody(xamlTableBodyElement, htmlChildNode, currentProperties, columnStarts, stylesheet, sourceContext);
                        if (xamlTableBodyElement.HasChildNodes)
                        {
                            xamlTableElement.AppendChild(xamlTableBodyElement);
                        }
                    }
                    else
                    {
                        htmlChildNode = htmlChildNode.NextSibling;
                    }
                }

                if (xamlTableElement.HasChildNodes)
                {
                    xamlParentElement.AppendChild(xamlTableElement);
                }
            }
        }

        private static XmlElement GetCellFromSingleCellTable(XmlElement htmlTableElement)
        {
            XmlElement singleCell = null;

            for (XmlNode tableChild = htmlTableElement.FirstChild; tableChild != null; tableChild = tableChild.NextSibling)
            {
                string elementName = tableChild.LocalName.ToLower();
                if (elementName == "tbody" || elementName == "thead" || elementName == "tfoot")
                {
                    if (singleCell != null)
                    {
                        return null;
                    }

                    for (XmlNode tbodyChild = tableChild.FirstChild; tbodyChild != null; tbodyChild = tbodyChild.NextSibling)
                    {
                        if (tbodyChild.LocalName.ToLower() == "tr")
                        {
                            if (singleCell != null)
                            {
                                return null;
                            }

                            for (XmlNode trChild = tbodyChild.FirstChild; trChild != null; trChild = trChild.NextSibling)
                            {
                                string cellName = trChild.LocalName.ToLower();
                                if (cellName == "td" || cellName == "th")
                                {
                                    if (singleCell != null)
                                    {
                                        return null;
                                    }

                                    singleCell = (XmlElement) trChild;
                                }
                            }
                        }
                    }
                }
                else if (tableChild.LocalName.ToLower() == "tr")
                {
                    if (singleCell != null)
                    {
                        return null;
                    }

                    for (XmlNode trChild = tableChild.FirstChild; trChild != null; trChild = trChild.NextSibling)
                    {
                        string cellName = trChild.LocalName.ToLower();
                        if (cellName == "td" || cellName == "th")
                        {
                            if (singleCell != null)
                            {
                                return null;
                            }

                            singleCell = (XmlElement) trChild;
                        }
                    }
                }
            }

            return singleCell;
        }

        private static void AddColumnInformation(XmlElement htmlTableElement, XmlElement xamlTableElement, ArrayList columnStartsAllRows, Hashtable currentProperties, CssStylesheet stylesheet, List<XmlElement> sourceContext)
        {
            var numberFormatInfo = new NumberFormatInfo();
            numberFormatInfo.NumberDecimalSeparator = ".";

            if (columnStartsAllRows != null)
            {
                for (int columnIndex = 0; columnIndex < columnStartsAllRows.Count - 1; columnIndex++)
                {
                    XmlElement xamlColumnElement;

                    xamlColumnElement = xamlTableElement.OwnerDocument.CreateElement(null, Xaml_TableColumn, _xamlNamespace);
                    xamlColumnElement.SetAttribute(Xaml_Width, ((double) columnStartsAllRows[columnIndex + 1] - (double) columnStartsAllRows[columnIndex]).ToString(numberFormatInfo));
                    xamlTableElement.AppendChild(xamlColumnElement);
                }
            }
            else
            {
                for (XmlNode htmlChildNode = htmlTableElement.FirstChild; htmlChildNode != null; htmlChildNode = htmlChildNode.NextSibling)
                {
                    if (htmlChildNode.LocalName.ToLower() == "colgroup")
                    {
                        AddTableColumnGroup(xamlTableElement, (XmlElement) htmlChildNode, currentProperties, stylesheet, sourceContext);
                    }
                    else if (htmlChildNode.LocalName.ToLower() == "col")
                    {
                        AddTableColumn(xamlTableElement, (XmlElement) htmlChildNode, currentProperties, stylesheet, sourceContext);
                    }
                    else if (htmlChildNode is XmlElement)
                    {
                        break;
                    }
                }
            }
        }

        private static void AddTableColumnGroup(XmlElement xamlTableElement, XmlElement htmlColgroupElement, Hashtable inheritedProperties, CssStylesheet stylesheet, List<XmlElement> sourceContext)
        {
            Hashtable localProperties;
            Hashtable currentProperties = GetElementProperties(htmlColgroupElement, inheritedProperties, out localProperties, stylesheet, sourceContext);

            XmlElement xamlTableColumnsElement = xamlTableElement.OwnerDocument.CreateElement(null, Xaml_TableColumns, _xamlNamespace);
            for (XmlNode htmlNode = htmlColgroupElement.FirstChild; htmlNode != null; htmlNode = htmlNode.NextSibling)
            {
                if (htmlNode is XmlElement && htmlNode.LocalName.ToLower() == "col")
                {
                    AddTableColumn(xamlTableColumnsElement, (XmlElement) htmlNode, currentProperties, stylesheet, sourceContext);
                }
            }

            xamlTableElement.AppendChild(xamlTableColumnsElement);
        }

        private static void AddTableColumn(XmlElement xamlTableElement, XmlElement htmlColElement, Hashtable inheritedProperties, CssStylesheet stylesheet, List<XmlElement> sourceContext)
        {
            Hashtable localProperties;
            Hashtable currentProperties = GetElementProperties(htmlColElement, inheritedProperties, out localProperties, stylesheet, sourceContext);

            XmlElement xamlTableColumnElement = xamlTableElement.OwnerDocument.CreateElement(null, Xaml_TableColumn, _xamlNamespace);

            var columnWidth = GetColumnWidth(htmlColElement);

            if (columnWidth != -1)
            {
                var numberFormatInfo = new NumberFormatInfo();
                numberFormatInfo.NumberDecimalSeparator = ".";
                xamlTableColumnElement.SetAttribute(Xaml_Width, columnWidth.ToString(numberFormatInfo));
            }

            xamlTableElement.AppendChild(xamlTableColumnElement);
        }

        private static XmlNode AddTableRowsToTableBody(XmlElement xamlTableBodyElement, XmlNode htmlTRStartNode, Hashtable currentProperties, ArrayList columnStarts, CssStylesheet stylesheet, List<XmlElement> sourceContext)
        {
            Debug.Assert(xamlTableBodyElement.LocalName == Xaml_TableRowGroup);
            Debug.Assert(currentProperties != null);

            XmlNode htmlChildNode = htmlTRStartNode;
            ArrayList activeRowSpans = null;
            if (columnStarts != null)
            {
                activeRowSpans = new ArrayList();
                InitializeActiveRowSpans(activeRowSpans, columnStarts.Count);
            }

            while (htmlChildNode != null && htmlChildNode.LocalName.ToLower() != "tbody")
            {
                if (htmlChildNode.LocalName.ToLower() == "tr")
                {
                    XmlElement xamlTableRowElement = xamlTableBodyElement.OwnerDocument.CreateElement(null, Xaml_TableRow, _xamlNamespace);

                    sourceContext.Add((XmlElement) htmlChildNode);

                    Hashtable trElementLocalProperties;
                    Hashtable trElementCurrentProperties = GetElementProperties((XmlElement) htmlChildNode, currentProperties, out trElementLocalProperties, stylesheet, sourceContext);
                    AddTableCellsToTableRow(xamlTableRowElement, htmlChildNode.FirstChild, trElementCurrentProperties, columnStarts, activeRowSpans, stylesheet, sourceContext);
                    if (xamlTableRowElement.HasChildNodes)
                    {
                        xamlTableBodyElement.AppendChild(xamlTableRowElement);
                    }

                    Debug.Assert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1] == htmlChildNode);
                    sourceContext.RemoveAt(sourceContext.Count - 1);

                    htmlChildNode = htmlChildNode.NextSibling;
                }
                else if (htmlChildNode.LocalName.ToLower() == "td")
                {
                    XmlElement xamlTableRowElement = xamlTableBodyElement.OwnerDocument.CreateElement(null, Xaml_TableRow, _xamlNamespace);

                    Debug.Assert(columnStarts == null);

                    htmlChildNode = AddTableCellsToTableRow(xamlTableRowElement, htmlChildNode, currentProperties, columnStarts, activeRowSpans, stylesheet, sourceContext);
                    if (xamlTableRowElement.HasChildNodes)
                    {
                        xamlTableBodyElement.AppendChild(xamlTableRowElement);
                    }
                }
                else
                {
                    htmlChildNode = htmlChildNode.NextSibling;
                }
            }

            return htmlChildNode;
        }

        private static XmlNode AddTableCellsToTableRow(XmlElement xamlTableRowElement, XmlNode htmlTDStartNode, Hashtable currentProperties, ArrayList columnStarts, ArrayList activeRowSpans, CssStylesheet stylesheet, List<XmlElement> sourceContext)
        {
            Debug.Assert(xamlTableRowElement.LocalName == Xaml_TableRow);
            Debug.Assert(currentProperties != null);
            if (columnStarts != null)
            {
                Debug.Assert(activeRowSpans.Count == columnStarts.Count);
            }

            XmlNode htmlChildNode = htmlTDStartNode;
            double columnStart = 0;
            double columnWidth = 0;
            int columnIndex = 0;
            int columnSpan = 0;

            while (htmlChildNode != null && htmlChildNode.LocalName.ToLower() != "tr" && htmlChildNode.LocalName.ToLower() != "tbody" && htmlChildNode.LocalName.ToLower() != "thead" && htmlChildNode.LocalName.ToLower() != "tfoot")
            {
                if (htmlChildNode.LocalName.ToLower() == "td" || htmlChildNode.LocalName.ToLower() == "th")
                {
                    XmlElement xamlTableCellElement = xamlTableRowElement.OwnerDocument.CreateElement(null, Xaml_TableCell, _xamlNamespace);

                    sourceContext.Add((XmlElement) htmlChildNode);

                    Hashtable tdElementLocalProperties;
                    Hashtable tdElementCurrentProperties = GetElementProperties((XmlElement) htmlChildNode, currentProperties, out tdElementLocalProperties, stylesheet, sourceContext);

                    ApplyPropertiesToTableCellElement((XmlElement) htmlChildNode, xamlTableCellElement, tdElementCurrentProperties);

                    if (columnStarts != null)
                    {
                        Debug.Assert(columnIndex < columnStarts.Count - 1);
                        while (columnIndex < activeRowSpans.Count && (int) activeRowSpans[columnIndex] > 0)
                        {
                            activeRowSpans[columnIndex] = (int) activeRowSpans[columnIndex] - 1;
                            Debug.Assert((int) activeRowSpans[columnIndex] >= 0);
                            columnIndex++;
                        }

                        Debug.Assert(columnIndex < columnStarts.Count - 1);
                        columnStart = (double) columnStarts[columnIndex];
                        columnWidth = GetColumnWidth((XmlElement) htmlChildNode);
                        columnSpan = CalculateColumnSpan(columnIndex, columnWidth, columnStarts);
                        int rowSpan = GetRowSpan((XmlElement) htmlChildNode);

                        Debug.Assert(columnSpan > 0);
                        Debug.Assert(columnIndex + columnSpan < columnStarts.Count);

                        xamlTableCellElement.SetAttribute(Xaml_TableCell_ColumnSpan, columnSpan.ToString());

                        for (int spannedColumnIndex = columnIndex; spannedColumnIndex < columnIndex + columnSpan; spannedColumnIndex++)
                        {
                            Debug.Assert(spannedColumnIndex < activeRowSpans.Count);
                            activeRowSpans[spannedColumnIndex] = (rowSpan - 1);
                            Debug.Assert((int) activeRowSpans[spannedColumnIndex] >= 0);
                        }

                        columnIndex = columnIndex + columnSpan;
                    }

                    AddDataToTableCell(xamlTableCellElement, htmlChildNode.FirstChild, tdElementCurrentProperties, stylesheet, sourceContext);
                    if (xamlTableCellElement.HasChildNodes)
                    {
                        xamlTableRowElement.AppendChild(xamlTableCellElement);
                    }

                    Debug.Assert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1] == htmlChildNode);
                    sourceContext.RemoveAt(sourceContext.Count - 1);

                    htmlChildNode = htmlChildNode.NextSibling;
                }
                else
                {
                    htmlChildNode = htmlChildNode.NextSibling;
                }
            }

            return htmlChildNode;
        }

        private static void AddDataToTableCell(XmlElement xamlTableCellElement, XmlNode htmlDataStartNode, Hashtable currentProperties, CssStylesheet stylesheet, List<XmlElement> sourceContext)
        {
            Debug.Assert(xamlTableCellElement.LocalName == Xaml_TableCell);
            Debug.Assert(currentProperties != null);

            for (XmlNode htmlChildNode = htmlDataStartNode; htmlChildNode != null; htmlChildNode = htmlChildNode != null ? htmlChildNode.NextSibling : null)
            {
                htmlChildNode = AddBlock(xamlTableCellElement, htmlChildNode, currentProperties, stylesheet, sourceContext);
            }
        }

        private static ArrayList AnalyzeTableStructure(XmlElement htmlTableElement, CssStylesheet stylesheet)
        {
            Debug.Assert(htmlTableElement.LocalName.ToLower() == "table");
            if (!htmlTableElement.HasChildNodes)
            {
                return null;
            }

            bool columnWidthsAvailable = true;

            ArrayList columnStarts = new ArrayList();
            ArrayList activeRowSpans = new ArrayList();
            Debug.Assert(columnStarts.Count == activeRowSpans.Count);

            XmlNode htmlChildNode = htmlTableElement.FirstChild;
            double tableWidth = 0;              

            while (htmlChildNode != null && columnWidthsAvailable)
            {
                Debug.Assert(columnStarts.Count == activeRowSpans.Count);

                switch (htmlChildNode.LocalName.ToLower())
                {
                    case "tbody":
                        double tbodyWidth = AnalyzeTbodyStructure((XmlElement) htmlChildNode, columnStarts, activeRowSpans, tableWidth, stylesheet);
                        if (tbodyWidth > tableWidth)
                        {
                            tableWidth = tbodyWidth;
                        }
                        else if (tbodyWidth == 0)
                        {
                            columnWidthsAvailable = false;    
                        }

                        break;
                    case "tr":
                        double trWidth = AnalyzeTRStructure((XmlElement) htmlChildNode, columnStarts, activeRowSpans, tableWidth, stylesheet);
                        if (trWidth > tableWidth)
                        {
                            tableWidth = trWidth;
                        }
                        else if (trWidth == 0)
                        {
                            columnWidthsAvailable = false;    
                        }

                        break;
                    case "td":
                        columnWidthsAvailable = false;    
                        break;
                    default:
                        break;
                }

                htmlChildNode = htmlChildNode.NextSibling;
            }

            if (columnWidthsAvailable)
            {
                columnStarts.Add(tableWidth);
                VerifyColumnStartsAscendingOrder(columnStarts);
            }
            else
            {
                columnStarts = null;
            }

            return columnStarts;
        }

        private static double AnalyzeTbodyStructure(XmlElement htmlTbodyElement, ArrayList columnStarts, ArrayList activeRowSpans, double tableWidth, CssStylesheet stylesheet)
        {
            Debug.Assert(htmlTbodyElement.LocalName.ToLower() == "tbody");
            Debug.Assert(columnStarts != null);

            double tbodyWidth = 0;
            bool columnWidthsAvailable = true;

            if (!htmlTbodyElement.HasChildNodes)
            {
                return tbodyWidth;
            }

            ClearActiveRowSpans(activeRowSpans);

            XmlNode htmlChildNode = htmlTbodyElement.FirstChild;

            while (htmlChildNode != null && columnWidthsAvailable)
            {
                switch (htmlChildNode.LocalName.ToLower())
                {
                    case "tr":
                        double trWidth = AnalyzeTRStructure((XmlElement) htmlChildNode, columnStarts, activeRowSpans, tbodyWidth, stylesheet);
                        if (trWidth > tbodyWidth)
                        {
                            tbodyWidth = trWidth;
                        }

                        break;
                    case "td":
                        columnWidthsAvailable = false;    
                        break;
                    default:
                        break;
                }

                htmlChildNode = htmlChildNode.NextSibling;
            }

            ClearActiveRowSpans(activeRowSpans);

            return columnWidthsAvailable ? tbodyWidth : 0;
        }

        private static double AnalyzeTRStructure(XmlElement htmlTRElement, ArrayList columnStarts, ArrayList activeRowSpans, double tableWidth, CssStylesheet stylesheet)
        {
            double columnWidth;

            Debug.Assert(htmlTRElement.LocalName.ToLower() == "tr");
            Debug.Assert(columnStarts != null);
            Debug.Assert(activeRowSpans != null);
            Debug.Assert(columnStarts.Count == activeRowSpans.Count);

            if (!htmlTRElement.HasChildNodes)
            {
                return 0;
            }

            bool columnWidthsAvailable = true;

            double columnStart = 0;      
            XmlNode htmlChildNode = htmlTRElement.FirstChild;
            int columnIndex = 0;
            double trWidth = 0;

            if (columnIndex < activeRowSpans.Count)
            {
                Debug.Assert((double) columnStarts[columnIndex] >= columnStart);
                if ((double) columnStarts[columnIndex] == columnStart)
                {
                    while (columnIndex < activeRowSpans.Count && (int) activeRowSpans[columnIndex] > 0)
                    {
                        activeRowSpans[columnIndex] = (int) activeRowSpans[columnIndex] - 1;
                        Debug.Assert((int) activeRowSpans[columnIndex] >= 0);
                        columnIndex++;
                        columnStart = (double) columnStarts[columnIndex];
                    }
                }
            }

            while (htmlChildNode != null && columnWidthsAvailable)
            {
                Debug.Assert(columnStarts.Count == activeRowSpans.Count);

                VerifyColumnStartsAscendingOrder(columnStarts);

                switch (htmlChildNode.LocalName.ToLower())
                {
                    case "td":
                        Debug.Assert(columnIndex <= columnStarts.Count);
                        if (columnIndex < columnStarts.Count)
                        {
                            Debug.Assert(columnStart <= (double) columnStarts[columnIndex]);
                            if (columnStart < (double) columnStarts[columnIndex])
                            {
                                columnStarts.Insert(columnIndex, columnStart);
                                activeRowSpans.Insert(columnIndex, 0);
                            }
                        }
                        else
                        {
                            columnStarts.Add(columnStart);
                            activeRowSpans.Add(0);
                        }

                        columnWidth = GetColumnWidth((XmlElement) htmlChildNode);
                        if (columnWidth != -1)
                        {
                            int nextColumnIndex;
                            int rowSpan = GetRowSpan((XmlElement) htmlChildNode);

                            nextColumnIndex = GetNextColumnIndex(columnIndex, columnWidth, columnStarts, activeRowSpans);
                            if (nextColumnIndex != -1)
                            {
                                Debug.Assert(nextColumnIndex <= columnStarts.Count);

                                for (int spannedColumnIndex = columnIndex; spannedColumnIndex < nextColumnIndex; spannedColumnIndex++)
                                {
                                    activeRowSpans[spannedColumnIndex] = rowSpan - 1;
                                    Debug.Assert((int) activeRowSpans[spannedColumnIndex] >= 0);
                                }

                                columnIndex = nextColumnIndex;

                                columnStart = columnStart + columnWidth;

                                if (columnIndex < activeRowSpans.Count)
                                {
                                    Debug.Assert((double) columnStarts[columnIndex] >= columnStart);
                                    if ((double) columnStarts[columnIndex] == columnStart)
                                    {
                                        while (columnIndex < activeRowSpans.Count && (int) activeRowSpans[columnIndex] > 0)
                                        {
                                            activeRowSpans[columnIndex] = (int) activeRowSpans[columnIndex] - 1;
                                            Debug.Assert((int) activeRowSpans[columnIndex] >= 0);
                                            columnIndex++;
                                            columnStart = (double) columnStarts[columnIndex];
                                        }
                                    }

                                }
                            }
                            else
                            {
                                columnWidthsAvailable = false;
                            }
                        }
                        else
                        {
                            columnWidthsAvailable = false;
                        }

                        break;
                    default:
                        break;
                }

                htmlChildNode = htmlChildNode.NextSibling;
            }

            if (columnWidthsAvailable)
            {
                trWidth = columnStart;
            }
            else
            {
                trWidth = 0;
            }

            return trWidth;
        }

        private static int GetRowSpan(XmlElement htmlTDElement)
        {
            string rowSpanAsString;
            int rowSpan;

            rowSpanAsString = GetAttribute((XmlElement) htmlTDElement, "rowspan");
            if (rowSpanAsString != null)
            {
                if (!Int32.TryParse(rowSpanAsString, out rowSpan))
                {
                    rowSpan = 1;
                }
            }
            else
            {
                rowSpan = 1;
            }

            return rowSpan;
        }

        private static int GetNextColumnIndex(int columnIndex, double columnWidth, ArrayList columnStarts, ArrayList activeRowSpans)
        {
            double columnStart;
            int spannedColumnIndex;

            Debug.Assert(columnStarts != null);
            Debug.Assert(0 <= columnIndex && columnIndex <= columnStarts.Count);
            Debug.Assert(columnWidth > 0);

            columnStart = (double) columnStarts[columnIndex];
            spannedColumnIndex = columnIndex + 1;

            while (spannedColumnIndex < columnStarts.Count && (double) columnStarts[spannedColumnIndex] < columnStart + columnWidth && spannedColumnIndex != -1)
            {
                if ((int) activeRowSpans[spannedColumnIndex] > 0)
                {
                    spannedColumnIndex = -1;
                }
                else
                {
                    spannedColumnIndex++;
                }
            }

            return spannedColumnIndex;
        }


        private static void ClearActiveRowSpans(ArrayList activeRowSpans)
        {
            for (int columnIndex = 0; columnIndex < activeRowSpans.Count; columnIndex++)
            {
                activeRowSpans[columnIndex] = 0;
            }
        }

        private static void InitializeActiveRowSpans(ArrayList activeRowSpans, int count)
        {
            for (int columnIndex = 0; columnIndex < count; columnIndex++)
            {
                activeRowSpans.Add(0);
            }
        }


        private static double GetNextColumnStart(XmlElement htmlTDElement, double columnStart)
        {
            double columnWidth;
            double nextColumnStart;

            Debug.Assert(htmlTDElement.LocalName.ToLower() == "td" || htmlTDElement.LocalName.ToLower() == "th");
            Debug.Assert(columnStart >= 0);

            nextColumnStart = -1;        

            columnWidth = GetColumnWidth(htmlTDElement);

            if (columnWidth == -1)
            {
                nextColumnStart = -1;
            }
            else
            {
                nextColumnStart = columnStart + columnWidth;
            }

            return nextColumnStart;
        }


        private static double GetColumnWidth(XmlElement htmlTDElement)
        {
            string columnWidthAsString;
            double columnWidth;

            columnWidthAsString = null;
            columnWidth = -1;

            columnWidthAsString = GetAttribute(htmlTDElement, "width");
            if (columnWidthAsString == null)
            {
                columnWidthAsString = GetCssAttribute(GetAttribute(htmlTDElement, "style"), "width");
            }

            if (!TryGetLengthValue(columnWidthAsString, out columnWidth) || columnWidth == 0)
            {
                columnWidth = -1;
            }

            return columnWidth;
        }

        private static int CalculateColumnSpan(int columnIndex, double columnWidth, ArrayList columnStarts)
        {
            double columnSpanningValue;
            int columnSpanningIndex;
            int columnSpan;
            double subColumnWidth;         

            Debug.Assert(columnStarts != null);
            Debug.Assert(columnIndex < columnStarts.Count - 1);
            Debug.Assert((double) columnStarts[columnIndex] >= 0);
            Debug.Assert(columnWidth > 0);

            columnSpanningIndex = columnIndex;
            columnSpanningValue = 0;
            columnSpan = 0;
            subColumnWidth = 0;

            while (columnSpanningValue < columnWidth && columnSpanningIndex < columnStarts.Count - 1)
            {
                subColumnWidth = (double) columnStarts[columnSpanningIndex + 1] - (double) columnStarts[columnSpanningIndex];
                Debug.Assert(subColumnWidth > 0);
                columnSpanningValue += subColumnWidth;
                columnSpanningIndex++;
            }

            columnSpan = columnSpanningIndex - columnIndex;
            Debug.Assert(columnSpan > 0);

            return columnSpan;
        }

        private static void VerifyColumnStartsAscendingOrder(ArrayList columnStarts)
        {
            Debug.Assert(columnStarts != null);

            double columnStart;

            columnStart = -0.01;

            for (int columnIndex = 0; columnIndex < columnStarts.Count; columnIndex++)
            {
                Debug.Assert(columnStart < (double) columnStarts[columnIndex]);
                columnStart = (double) columnStarts[columnIndex];
            }
        }

        private static void ApplyLocalProperties(XmlElement xamlElement, Hashtable localProperties, bool isBlock, bool isTableCell = false)
        {
            bool marginSet = false;
            string marginTop = "0";
            string marginBottom = "0";
            string marginLeft = "0";
            string marginRight = "0";

            bool paddingSet = false;
            string paddingTop = "0";
            string paddingBottom = "0";
            string paddingLeft = "0";
            string paddingRight = "0";

            string borderColor = null;

            bool borderThicknessSet = false;
            string borderThicknessTop = "0";
            string borderThicknessBottom = "0";
            string borderThicknessLeft = "0";
            string borderThicknessRight = "0";

            IDictionaryEnumerator propertyEnumerator = localProperties.GetEnumerator();
            while (propertyEnumerator.MoveNext())
            {
                switch ((string) propertyEnumerator.Key)
                {
                    case "font-family":
                        xamlElement.SetAttribute(Xaml_FontFamily, (string) propertyEnumerator.Value);
                        break;
                    case "font-style":
                        xamlElement.SetAttribute(Xaml_FontStyle, (string) propertyEnumerator.Value);
                        break;
                    case "font-variant":
                        break;
                    case "font-weight":
                        xamlElement.SetAttribute(Xaml_FontWeight, (string) propertyEnumerator.Value);
                        break;
                    case "font-size":
                        xamlElement.SetAttribute(Xaml_FontSize, (string) propertyEnumerator.Value);
                        break;
                    case "color":
                        SetPropertyValue(xamlElement, TextElement.ForegroundProperty, (string) propertyEnumerator.Value);
                        break;
                    case "background-color":
                        SetPropertyValue(xamlElement, TextElement.BackgroundProperty, (string) propertyEnumerator.Value);
                        break;
                    case "text-decoration-underline":
                        if (!isBlock)
                        {
                            if ((string) propertyEnumerator.Value == "true")
                            {
                                xamlElement.SetAttribute(Xaml_TextDecorations, Xaml_TextDecorations_Underline);
                            }
                        }

                        break;
                    case "text-decoration-line-through":
                        if (!isBlock)
                        {
                            if ((string) propertyEnumerator.Value == "true")
                            {
                                xamlElement.SetAttribute(Xaml_TextDecorations, Xaml_TextDecorations_Strikethrough);
                            }
                        }

                        break;
                    case "text-decoration-none":
                    case "text-decoration-overline":
                    case "text-decoration-blink":
                        if (!isBlock)
                        {
                        }

                        break;
                    case "text-transform":
                        break;

                    case "text-indent":
                        if (isBlock)
                        {
                            xamlElement.SetAttribute(Xaml_TextIndent, (string) propertyEnumerator.Value);
                        }

                        break;

                    case "text-align":
                        if (isBlock)
                        {
                            xamlElement.SetAttribute(Xaml_TextAlignment, (string) propertyEnumerator.Value);
                        }

                        break;

                    case "width":
                    case "height":
                        break;

                    case "margin-top":
                        marginSet = true;
                        marginTop = (string) propertyEnumerator.Value;
                        break;
                    case "margin-right":
                        marginSet = true;
                        marginRight = (string) propertyEnumerator.Value;
                        break;
                    case "margin-bottom":
                        marginSet = true;
                        marginBottom = (string) propertyEnumerator.Value;
                        break;
                    case "margin-left":
                        marginSet = true;
                        marginLeft = (string) propertyEnumerator.Value;
                        break;

                    case "padding-top":
                        paddingSet = true;
                        paddingTop = (string) propertyEnumerator.Value;
                        break;
                    case "padding-right":
                        paddingSet = true;
                        paddingRight = (string) propertyEnumerator.Value;
                        break;
                    case "padding-bottom":
                        paddingSet = true;
                        paddingBottom = (string) propertyEnumerator.Value;
                        break;
                    case "padding-left":
                        paddingSet = true;
                        paddingLeft = (string) propertyEnumerator.Value;
                        break;

                    case "border-color-top":
                        borderColor = (string) propertyEnumerator.Value;
                        break;
                    case "border-color-right":
                        borderColor = (string) propertyEnumerator.Value;
                        break;
                    case "border-color-bottom":
                        borderColor = (string) propertyEnumerator.Value;
                        break;
                    case "border-color-left":
                        borderColor = (string) propertyEnumerator.Value;
                        break;
                    case "border-style-top":
                    case "border-style-right":
                    case "border-style-bottom":
                    case "border-style-left":
                        break;
                    case "border-width-top":
                        borderThicknessSet = true;
                        borderThicknessTop = (string) propertyEnumerator.Value;
                        break;
                    case "border-width-right":
                        borderThicknessSet = true;
                        borderThicknessRight = (string) propertyEnumerator.Value;
                        break;
                    case "border-width-bottom":
                        borderThicknessSet = true;
                        borderThicknessBottom = (string) propertyEnumerator.Value;
                        break;
                    case "border-width-left":
                        borderThicknessSet = true;
                        borderThicknessLeft = (string) propertyEnumerator.Value;
                        break;

                    case "list-style-type":
                        if (xamlElement.LocalName == Xaml_List)
                        {
                            string markerStyle;
                            switch (((string) propertyEnumerator.Value).ToLower())
                            {
                                case "disc":
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_Disc;
                                    break;
                                case "circle":
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_Circle;
                                    break;
                                case "none":
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_None;
                                    break;
                                case "square":
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_Square;
                                    break;
                                case "box":
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_Box;
                                    break;
                                case "lower-latin":
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_LowerLatin;
                                    break;
                                case "upper-latin":
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_UpperLatin;
                                    break;
                                case "lower-roman":
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_LowerRoman;
                                    break;
                                case "upper-roman":
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_UpperRoman;
                                    break;
                                case "decimal":
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_Decimal;
                                    break;
                                default:
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_Disc;
                                    break;
                            }

                            xamlElement.SetAttribute(HtmlToXamlConverter.Xaml_List_MarkerStyle, markerStyle);
                        }

                        break;

                    case "float":
                    case "clear":
                        if (isBlock)
                        {
                        }

                        break;

                    case "display":
                        break;
                }
            }

            if (isBlock)
            {
                if (marginSet)
                {
                    ComposeThicknessProperty(xamlElement, Xaml_Margin, marginLeft, marginRight, marginTop, marginBottom);
                }

                if (paddingSet)
                {
                    ComposeThicknessProperty(xamlElement, Xaml_Padding, paddingLeft, paddingRight, paddingTop, paddingBottom);
                }
            }

            if (isBlock || isTableCell)
            {
                if (borderColor != null)
                {
                    xamlElement.SetAttribute(Xaml_BorderBrush, borderColor);
                }

                if (borderThicknessSet)
                {
                    ComposeThicknessProperty(xamlElement, Xaml_BorderThickness, borderThicknessLeft, borderThicknessRight, borderThicknessTop, borderThicknessBottom);
                }
            }
        }

        private static void ComposeThicknessProperty(XmlElement xamlElement, string propertyName, string left, string right, string top, string bottom)
        {
            string thickness;

            if (left[0] == '0' || left[0] == '-') left = "0";
            if (right[0] == '0' || right[0] == '-') right = "0";
            if (top[0] == '0' || top[0] == '-') top = "0";
            if (bottom[0] == '0' || bottom[0] == '-') bottom = "0";

            if (left == right && top == bottom)
            {
                if (left == top)
                {
                    thickness = left;
                }
                else
                {
                    thickness = left + "," + top;
                }
            }
            else
            {
                thickness = left + "," + top + "," + right + "," + bottom;
            }

            xamlElement.SetAttribute(propertyName, thickness);
        }

        private static void SetPropertyValue(XmlElement xamlElement, DependencyProperty property, string stringValue)
        {
            System.ComponentModel.TypeConverter typeConverter = System.ComponentModel.TypeDescriptor.GetConverter(property.PropertyType);
            try
            {
                object convertedValue = typeConverter.ConvertFromInvariantString(stringValue);
                if (convertedValue != null)
                {
                    xamlElement.SetAttribute(property.Name, stringValue);
                }
            }
            catch (Exception)
            {
            }
        }

        private static Hashtable GetElementProperties(XmlElement htmlElement, Hashtable inheritedProperties, out Hashtable localProperties, CssStylesheet stylesheet, List<XmlElement> sourceContext)
        {
            Hashtable currentProperties = new Hashtable();
            IDictionaryEnumerator propertyEnumerator = inheritedProperties.GetEnumerator();
            while (propertyEnumerator.MoveNext())
            {
                currentProperties[propertyEnumerator.Key] = propertyEnumerator.Value;
            }

            string elementName = htmlElement.LocalName.ToLower();
            string elementNamespace = htmlElement.NamespaceURI;

            var numberFormatInfo = new NumberFormatInfo();
            numberFormatInfo.NumberDecimalSeparator = ".";

            localProperties = new Hashtable();
            switch (elementName)
            {
                case "i":
                case "italic":
                case "em":
                    localProperties["font-style"] = "italic";
                    break;
                case "b":
                case "bold":
                case "strong":
                case "dfn":
                    localProperties["font-weight"] = "bold";
                    break;
                case "u":
                case "underline":
                case "ins":
                    localProperties["text-decoration-underline"] = "true";
                    break;
                case "s":
                case "strike":
                case "del":
                    localProperties["text-decoration-line-through"] = "true";
                    break;
                case "font":
                    string attributeValue = GetAttribute(htmlElement, "face");
                    if (attributeValue != null)
                    {
                        localProperties["font-family"] = attributeValue;
                    }

                    attributeValue = GetAttribute(htmlElement, "size");
                    if (attributeValue != null)
                    {
                        double fontSize = double.Parse(attributeValue) * (12.0 / 3.0);
                        if (fontSize < 1.0)
                        {
                            fontSize = 1.0;
                        }
                        else if (fontSize > 1000.0)
                        {
                            fontSize = 1000.0;
                        }

                        localProperties["font-size"] = fontSize.ToString(numberFormatInfo);
                    }

                    attributeValue = GetAttribute(htmlElement, "color");
                    if (attributeValue != null)
                    {
                        localProperties["color"] = attributeValue;
                    }

                    break;
                case "samp":
                    localProperties["font-family"] = "Courier New";   
                    localProperties["font-size"] = Xaml_FontSize_XXSmall;
                    localProperties["text-align"] = "Left";
                    break;
                case "sub":
                    break;
                case "sup":
                    break;

                case "a":        
                    break;
                case "acronym":
                    break;

                case "p":
                    localProperties["text-align"] = "Left";
                    break;
                case "div":
                    break;
                case "pre":
                    localProperties["font-family"] = "Courier New";       
                    localProperties["font-size"] = Xaml_FontSize_XXSmall;
                    localProperties["text-align"] = "Left";
                    break;
                case "blockquote":
                    localProperties["margin-left"] = "16";
                    break;

                case "h1":
                    localProperties["font-size"] = Xaml_FontSize_XXLarge;
                    break;
                case "h2":
                    localProperties["font-size"] = Xaml_FontSize_XLarge;
                    break;
                case "h3":
                    localProperties["font-size"] = Xaml_FontSize_Large;
                    break;
                case "h4":
                    localProperties["font-size"] = Xaml_FontSize_Medium;
                    break;
                case "h5":
                    localProperties["font-size"] = Xaml_FontSize_Small;
                    break;
                case "h6":
                    localProperties["font-size"] = Xaml_FontSize_XSmall;
                    break;
                case "ul":
                    localProperties["list-style-type"] = "disc";
                    break;
                case "ol":
                    attributeValue = GetAttribute(htmlElement, "type");
                    if (attributeValue == null || attributeValue == "1")
                    {
                        localProperties["list-style-type"] = "decimal";
                    }
                    else if (attributeValue == "a")
                    {
                        localProperties["list-style-type"] = "lower-latin";
                    }
                    else if (attributeValue == "A")
                    {
                        localProperties["list-style-type"] = "upper-latin";
                    }
                    else if (attributeValue == "i")
                    {
                        localProperties["list-style-type"] = "lower-roman";
                    }
                    else if (attributeValue == "I")
                    {
                        localProperties["list-style-type"] = "upper-roman";
                    }

                    break;
                case "table":
                    attributeValue = GetAttribute(htmlElement, "border");
                    if (attributeValue != null)
                    {
                        localProperties["border-width-top"] = attributeValue;
                        localProperties["border-width-right"] = attributeValue;
                        localProperties["border-width-bottom"] = attributeValue;
                        localProperties["border-width-left"] = attributeValue;
                    }

                    break;
                case "body":
                case "html":
                    break;
            }

            HtmlCssParser.GetElementPropertiesFromCssAttributes(htmlElement, elementName, stylesheet, localProperties, sourceContext);

            propertyEnumerator = localProperties.GetEnumerator();
            while (propertyEnumerator.MoveNext())
            {
                currentProperties[propertyEnumerator.Key] = propertyEnumerator.Value;
            }

            return currentProperties;
        }

        private static string GetCssAttribute(string cssStyle, string attributeName)
        {
            if (cssStyle != null)
            {
                string[] styleValues;

                attributeName = attributeName.ToLower();

                styleValues = cssStyle.Split(';');

                for (int styleValueIndex = 0; styleValueIndex < styleValues.Length; styleValueIndex++)
                {
                    string[] styleNameValue;

                    styleNameValue = styleValues[styleValueIndex].Split(':');
                    if (styleNameValue.Length == 2)
                    {
                        if (styleNameValue[0].Trim().ToLower() == attributeName)
                        {
                            return styleNameValue[1].Trim();
                        }
                    }
                }
            }

            return null;
        }

        private static bool TryGetLengthValue(string lengthAsString, out double length)
        {
            length = Double.NaN;

            if (lengthAsString != null)
            {
                lengthAsString = lengthAsString.Trim().ToLower();

                if (lengthAsString.EndsWith("pt"))
                {
                    lengthAsString = lengthAsString.Substring(0, lengthAsString.Length - 2);
                    if (Double.TryParse(lengthAsString, NumberStyles.Any, CultureInfo.InvariantCulture, out length))
                    {
                        length = (length * 96.0) / 72.0;      
                    }
                    else
                    {
                        length = Double.NaN;
                    }
                }
                else if (lengthAsString.EndsWith("px"))
                {
                    lengthAsString = lengthAsString.Substring(0, lengthAsString.Length - 2);
                    if (!Double.TryParse(lengthAsString, NumberStyles.Any, CultureInfo.InvariantCulture, out length))
                    {
                        length = Double.NaN;
                    }
                }
                else
                {
                    if (!Double.TryParse(lengthAsString, NumberStyles.Any, CultureInfo.InvariantCulture, out length))   
                    {
                        length = Double.NaN;
                    }
                }
            }

            return !Double.IsNaN(length);
        }

        private static string GetColorValue(string colorValue)
        {
            return colorValue;
        }

        private static void ApplyPropertiesToTableCellElement(XmlElement htmlChildNode, XmlElement xamlTableCellElement, Hashtable localProperties)
        {
            Debug.Assert(htmlChildNode.LocalName.ToLower() == "td" || htmlChildNode.LocalName.ToLower() == "th");
            Debug.Assert(xamlTableCellElement.LocalName == Xaml_TableCell);

            xamlTableCellElement.SetAttribute(Xaml_TableCell_BorderThickness, "1,1,1,1");
            xamlTableCellElement.SetAttribute(Xaml_TableCell_BorderBrush, Xaml_Brushes_Gray);
            string rowSpanString = GetAttribute((XmlElement) htmlChildNode, "rowspan");
            if (rowSpanString != null)
            {
                xamlTableCellElement.SetAttribute(Xaml_TableCell_RowSpan, rowSpanString);
            }

            ApplyLocalProperties(xamlTableCellElement, localProperties, false, true);
        }

        public const string Xaml_FlowDocument = "FlowDocument";

        public const string Xaml_Run = "Run";
        public const string Xaml_Span = "Span";
        public const string Xaml_Hyperlink = "Hyperlink";
        public const string Xaml_Hyperlink_NavigateUri = "NavigateUri";
        public const string Xaml_Hyperlink_TargetName = "TargetName";

        public const string Xaml_Section = "Section";

        public const string Xaml_List = "List";

        public const string Xaml_List_MarkerStyle = "MarkerStyle";
        public const string Xaml_List_MarkerStyle_None = "None";
        public const string Xaml_List_MarkerStyle_Decimal = "Decimal";
        public const string Xaml_List_MarkerStyle_Disc = "Disc";
        public const string Xaml_List_MarkerStyle_Circle = "Circle";
        public const string Xaml_List_MarkerStyle_Square = "Square";
        public const string Xaml_List_MarkerStyle_Box = "Box";
        public const string Xaml_List_MarkerStyle_LowerLatin = "LowerLatin";
        public const string Xaml_List_MarkerStyle_UpperLatin = "UpperLatin";
        public const string Xaml_List_MarkerStyle_LowerRoman = "LowerRoman";
        public const string Xaml_List_MarkerStyle_UpperRoman = "UpperRoman";

        public const string Xaml_ListItem = "ListItem";

        public const string Xaml_LineBreak = "LineBreak";

        public const string Xaml_Paragraph = "Paragraph";

        public const string Xaml_Margin = "Margin";
        public const string Xaml_Padding = "Padding";
        public const string Xaml_BorderBrush = "BorderBrush";
        public const string Xaml_BorderThickness = "BorderThickness";

        public const string Xaml_Table = "Table";

        public const string Xaml_TableColumns = "Table.Columns";
        public const string Xaml_TableColumn = "TableColumn";
        public const string Xaml_TableRowGroup = "TableRowGroup";
        public const string Xaml_TableRow = "TableRow";

        public const string Xaml_TableCell = "TableCell";
        public const string Xaml_TableCell_BorderThickness = "BorderThickness";
        public const string Xaml_TableCell_BorderBrush = "BorderBrush";

        public const string Xaml_TableCell_ColumnSpan = "ColumnSpan";
        public const string Xaml_TableCell_RowSpan = "RowSpan";

        public const string Xaml_Width = "Width";
        public const string Xaml_Brushes_Gray = "Gray";
        public const string Xaml_FontFamily = "FontFamily";

        public const string Xaml_FontSize = "FontSize";
        public const string Xaml_FontSize_XXLarge = "22pt";  
        public const string Xaml_FontSize_XLarge = "20pt";  
        public const string Xaml_FontSize_Large = "18pt";  
        public const string Xaml_FontSize_Medium = "16pt";  
        public const string Xaml_FontSize_Small = "12pt";  
        public const string Xaml_FontSize_XSmall = "10pt";  
        public const string Xaml_FontSize_XXSmall = "8pt";  

        public const string Xaml_FontWeight = "FontWeight";
        public const string Xaml_FontWeight_Bold = "Bold";

        public const string Xaml_FontStyle = "FontStyle";

        public const string Xaml_Foreground = "Foreground";
        public const string Xaml_Background = "Background";
        public const string Xaml_TextDecorations = "TextDecorations";
        public const string Xaml_TextDecorations_Underline = "Underline";
        public const string Xaml_TextDecorations_Strikethrough = "Strikethrough";

        public const string Xaml_TextIndent = "TextIndent";
        public const string Xaml_TextAlignment = "TextAlignment";

        static string _xamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    }
}