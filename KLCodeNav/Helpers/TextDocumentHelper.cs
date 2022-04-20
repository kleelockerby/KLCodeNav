using EnvDTE;
using System;
using System.Collections.Generic;

namespace KLCodeNav
{
    public static class TextDocumentHelper
    {
        internal const int StandardFindOptions = (int)(vsFindOptions.vsFindOptionsRegularExpression |
                                                       vsFindOptions.vsFindOptionsMatchInHiddenText);

        internal static IEnumerable<EditPoint> FindMatches(TextDocument textDocument, string patternString)
        {
            var matches = new List<EditPoint>();
            var cursor = textDocument.StartPoint.CreateEditPoint();
            EditPoint end = null;
            TextRanges dummy = null;

            // Exception Here
            while (cursor != null && cursor.FindPattern(patternString, StandardFindOptions, ref end, ref dummy))
            {
                matches.Add(cursor.CreateEditPoint());
                cursor = end;
            }

            return matches;
        }

        internal static IEnumerable<EditPoint> FindMatches(TextSelection textSelection, string patternString)
        {
            var matches = new List<EditPoint>();
            var cursor = textSelection.TopPoint.CreateEditPoint();
            EditPoint end = null;
            TextRanges dummy = null;

            while (cursor != null && cursor.FindPattern(patternString, StandardFindOptions, ref end, ref dummy))
            {
                if (end.AbsoluteCharOffset > textSelection.BottomPoint.AbsoluteCharOffset)
                {
                    break;
                }

                matches.Add(cursor.CreateEditPoint());
                cursor = end;
            }

            return matches;
        }

        internal static EditPoint FirstOrDefaultMatch(TextDocument textDocument, string patternString)
        {
            var cursor = textDocument.StartPoint.CreateEditPoint();
            EditPoint end = null;
            TextRanges dummy = null;

            if (cursor != null && cursor.FindPattern(patternString, StandardFindOptions, ref end, ref dummy))
            {
                return cursor.CreateEditPoint();
            }

            return null;
        }

        internal static string GetTextToFirstMatch(TextPoint startPoint, string matchString)
        {
            var startEditPoint = startPoint.CreateEditPoint();
            var endEditPoint = startEditPoint.CreateEditPoint();
            TextRanges subGroupMatches = null;       

            if (endEditPoint.FindPattern(matchString, StandardFindOptions, ref endEditPoint, ref subGroupMatches))
            {
                return startEditPoint.GetText(endEditPoint);
            }

            return null;
        }

        internal static void InsertBlankLineBeforePoint(EditPoint point)
        {
            if (point.Line <= 1) return;

            point.LineUp(1);
            point.StartOfLine();

            string text = point.GetLine();
            if (RegexNullSafe.IsMatch(text, @"^\s*[^\s\{]"))          
            {
                point.EndOfLine();
                point.Insert(Environment.NewLine);
            }
        }

        internal static void InsertBlankLineAfterPoint(EditPoint point)
        {
            if (point.AtEndOfDocument) return;

            point.LineDown(1);
            point.StartOfLine();

            string text = point.GetLine();
            if (RegexNullSafe.IsMatch(text, @"^\s*[^\s\}]"))
            {
                point.Insert(Environment.NewLine);
            }
        }

        internal static void MoveToCodeItem(Document document, BaseCodeItem codeItem, bool centerOnWhole)
        {
            var textDocument = document.GetTextDocument();
            if (textDocument == null) return;

            try
            {
                object viewRangeEnd = null;
                TextPoint navigatePoint = null;

                codeItem.RefreshCachedPositionAndName();
                textDocument.Selection.MoveToPoint(codeItem.StartPoint, false);

                if (centerOnWhole)
                {
                    viewRangeEnd = codeItem.EndPoint;
                }

                var codeItemElement = codeItem as BaseCodeItemElement;
                if (codeItemElement != null)
                {
                    navigatePoint = codeItemElement.CodeElement.GetStartPoint(vsCMPart.vsCMPartNavigate);
                }

                textDocument.Selection.AnchorPoint.TryToShow(vsPaneShowHow.vsPaneShowCentered, viewRangeEnd);

                if (navigatePoint != null)
                {
                    textDocument.Selection.MoveToPoint(navigatePoint, false);
                }
                else
                {
                    textDocument.Selection.FindText(codeItem.Name, (int)vsFindOptions.vsFindOptionsMatchInHiddenText);
                    textDocument.Selection.MoveToPoint(textDocument.Selection.AnchorPoint, false);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                document.Activate();
            }
        }

        internal static void SelectCodeItem(Document document, BaseCodeItem codeItem)
        {
            var textDocument = document.GetTextDocument();
            if (textDocument == null) return;

            try
            {
                codeItem.RefreshCachedPositionAndName();
                textDocument.Selection.MoveToPoint(codeItem.StartPoint, false);
                textDocument.Selection.MoveToPoint(codeItem.EndPoint, true);

                textDocument.Selection.SwapAnchor();
            }
            catch (Exception)
            {
            }
            finally
            {
                document.Activate();
            }
        }

        internal static void SubstituteAllStringMatches(TextDocument textDocument, string patternString, string replacementString)
        {
            TextRanges dummy = null;
            int lastCount = -1;
            while (textDocument.ReplacePattern(patternString, replacementString, StandardFindOptions, ref dummy))
            {
                if (lastCount == dummy.Count)
                {
                   // OutputWindowHelper.WarningWriteLine("Forced a break out of TextDocumentHelper's SubstituteAllStringMatches for a document.");
                    break;
                }
                lastCount = dummy.Count;
            }
        }

        internal static void SubstituteAllStringMatches(TextSelection textSelection, string patternString, string replacementString)
        {
            TextRanges dummy = null;
            int lastCount = -1;
            while (textSelection.ReplacePattern(patternString, replacementString, StandardFindOptions, ref dummy))
            {
                if (lastCount == dummy.Count)
                {
                    //OutputWindowHelper.WarningWriteLine("Forced a break out of TextDocumentHelper's SubstituteAllStringMatches for a selection.");
                    break;
                }
                lastCount = dummy.Count;
            }
        }

        internal static void SubstituteAllStringMatches(EditPoint startPoint, EditPoint endPoint, string patternString, string replacementString)
        {
            TextRanges dummy = null;
            int lastCount = -1;
            while (startPoint.ReplacePattern(endPoint, patternString, replacementString, StandardFindOptions, ref dummy))
            {
                if (lastCount == dummy.Count)
                {
                   // OutputWindowHelper.WarningWriteLine("Forced a break out of TextDocumentHelper's SubstituteAllStringMatches for a pair of points.");
                    break;
                }
                lastCount = dummy.Count;
            }
        }

    }
}
