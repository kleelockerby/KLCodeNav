using EnvDTE;
using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace KLCodeNav
{
    public static class DocumentHelper
    {
        public static string GetName(Window window)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var name = string.Empty;
            try
            {
                name = window.Document.Name;
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine("Error getting Name for document", e);
            }
            return name;
        }

        public static string GetFullName(Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var name = string.Empty;
            if (document == null) return name;

            try
            {
                name = document.FullName;
            }
            catch (COMException)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine("Error getting FullName for document", e);
            }
            return name;
        }

        public static string GetText(Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var text = string.Empty;
            try
            {
                var textDocument = (TextDocument)document.Object("TextDocument");
                var startPoint = textDocument?.StartPoint?.CreateEditPoint();

                if (startPoint == null)
                {
                    Console.WriteLine("Error during mapping: Unable to find TextDocument StartPoint");
                    return null;
                };

                text = startPoint.GetText(textDocument.EndPoint);
            }
            catch (COMException e)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine("Error getting Text for document", e);
            }
            return text;
        }
    }
}
