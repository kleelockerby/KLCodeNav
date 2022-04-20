using EnvDTE;
using System.Collections.Generic;

namespace KLCodeNav
{
    public interface ICodeItemParameters : ICodeItem
    {
        IEnumerable<CodeParameter> Parameters { get; }
    }
}