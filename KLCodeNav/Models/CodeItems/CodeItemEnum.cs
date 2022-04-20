using EnvDTE;
using System;

namespace KLCodeNav
{
    public class CodeItemEnum : BaseCodeItemElementParent
    {
        public CodeItemEnum()
        {
            _Access = LazyTryDefault( () => CodeEnum?.Access ?? vsCMAccess.vsCMAccessPublic);
            _Attributes = LazyTryDefault( () => CodeEnum?.Attributes);
            _DocComment = LazyTryDefault( () => CodeEnum?.DocComment);
            _Namespace = LazyTryDefault( () => CodeEnum?.Namespace?.Name);
            _TypeString = new Lazy<string>( () => "enum");
        }

        public override KindCodeItem Kind => KindCodeItem.Enum;

        public CodeEnum CodeEnum { get; set; }

    }
}