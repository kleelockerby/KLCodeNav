using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KLCodeNav
{
    public static class FileSystemHelper
    {
        public static void CopyFileToFolder(string filePath, string folderName)
        {
            File.Copy(filePath, Path.Combine(folderName, Path.GetFileName(filePath)));
        }

        public static string GetRelativePath(string path, string basePath)
        {
            path = Path.GetFullPath(path);
            basePath = Path.GetFullPath(basePath);
            if (String.Equals(path, basePath, StringComparison.OrdinalIgnoreCase))
                return ".";    

            if (path.StartsWith(basePath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return path.Substring(basePath.Length + 1);

            string pathRoot = Path.GetPathRoot(path);
            if (!String.Equals(pathRoot, Path.GetPathRoot(basePath), StringComparison.OrdinalIgnoreCase))
                return path;

            string[] pathParts = path.Substring(pathRoot.Length).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string[] basePathParts = basePath.Substring(pathRoot.Length).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            int commonFolderCount = 0;
            while (commonFolderCount < pathParts.Length && commonFolderCount < basePathParts.Length &&
                   String.Equals(pathParts[commonFolderCount], basePathParts[commonFolderCount], StringComparison.OrdinalIgnoreCase))
                commonFolderCount++;

            StringBuilder result = new StringBuilder();
            for (int i = 0 ; i < basePathParts.Length - commonFolderCount ; i++)
            {
                result.Append("..");
                result.Append(Path.DirectorySeparatorChar);
            }

            if (pathParts.Length - commonFolderCount == 0)
                return result.ToString().TrimEnd(Path.DirectorySeparatorChar);

            result.Append(String.Join(Path.DirectorySeparatorChar.ToString(), pathParts, commonFolderCount, pathParts.Length - commonFolderCount));
            return result.ToString();
        }

        public static bool FileCompare(string filePath1, string filePath2)
        {
            int file1byte;
            int file2byte;

            if (String.Equals(filePath1, filePath2, StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            using (FileStream fs1 = new FileStream(filePath1, FileMode.Open, FileAccess.Read))
            using (FileStream fs2 = new FileStream(filePath2, FileMode.Open, FileAccess.Read))
            {
                if (fs1.Length != fs2.Length)
                {
                    return false;
                }

                do
                {
                    file1byte = fs1.ReadByte();
                    file2byte = fs2.ReadByte();
                } while ((file1byte == file2byte) && (file1byte != -1));
            }

            return ((file1byte - file2byte) == 0);
        }
    }
}
