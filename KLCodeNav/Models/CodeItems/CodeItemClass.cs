using EnvDTE;
using EnvDTE80;
using System;

namespace KLCodeNav
{
    public class CodeItemClass : BaseCodeItemElementParent
    {
        public CodeItemClass()
        {
            _Access = LazyTryDefault(
                () => CodeClass?.Access ?? vsCMAccess.vsCMAccessPublic);

            _Attributes = LazyTryDefault(
                () => CodeClass?.Attributes);

            _DocComment = LazyTryDefault(
                () => CodeClass?.DocComment);

            _IsStatic = LazyTryDefault(
                () => CodeClass != null && CodeClass.IsShared);

            _Namespace = LazyTryDefault(
                () => CodeClass?.Namespace?.Name);

            _TypeString = new Lazy<string>(
                () => "class");
        }

        public override KindCodeItem Kind => KindCodeItem.Class;

        public CodeClass2 CodeClass { get; set; }

    }
}