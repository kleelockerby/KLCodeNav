using System;
using System.IO;
using System.Collections.Generic;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;

namespace KLCodeNav
{
    public static class DTEHelper
    {
        private static readonly Regex GenericPartRegex = new Regex(@"(<.*>)|(\(Of .*\))", RegexOptions.Compiled);


    }
}
