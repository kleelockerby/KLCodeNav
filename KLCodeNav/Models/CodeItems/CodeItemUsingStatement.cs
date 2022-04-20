using System;

namespace KLCodeNav
{
    public class CodeItemUsingStatement : BaseCodeItemElement
    {
        public CodeItemUsingStatement()
        {
            _TypeString = new Lazy<string>( () => "using");
        }

        public override KindCodeItem Kind => KindCodeItem.Using;

        public override void RefreshCachedPositionAndName()
        {
            var startPoint = CodeElement.GetStartPoint();
            var endPoint = CodeElement.GetEndPoint();

            StartLine = startPoint.Line;
            StartOffset = startPoint.AbsoluteCharOffset;
            EndLine = endPoint.Line;
            EndOffset = endPoint.AbsoluteCharOffset;
        }

    }
}