using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;

namespace KLCodeNav
{
    public abstract class BaseCodeItem
    {
        public abstract KindCodeItem Kind { get; }
        public string Name { get; set; }
        public int StartLine { get; set; }
        public int StartOffset { get; set; }
        public virtual EditPoint StartPoint { get; set; }
        public int EndLine { get; set; }
        public int EndOffset { get; set; }
        public virtual EditPoint EndPoint { get; set; }
        public bool IsMultiLine => StartPoint != null && EndPoint != null && StartPoint.Line != EndPoint.Line;

        public virtual void LoadLazyInitializedValues() { }

        public virtual void RefreshCachedPositionAndName()
        {
            StartLine = StartPoint.Line;
            StartOffset = StartPoint.AbsoluteCharOffset;
            EndLine = EndPoint.Line;
            EndOffset = EndPoint.AbsoluteCharOffset;
        }

    }
}
