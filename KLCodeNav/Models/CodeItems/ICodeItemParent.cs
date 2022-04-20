using EnvDTE;
using System;

namespace KLCodeNav
{
    public interface ICodeItemParent : ICodeItem
    {
        event EventHandler IsExpandedChanged;

        SetCodeItems Children { get; }

        EditPoint InsertPoint { get; }

        bool IsExpanded { get; set; }
    }
}