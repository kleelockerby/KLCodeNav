using EnvDTE;
using EnvDTE80;
using System;

namespace KLCodeNav
{
    public class CodeItemEvent : BaseCodeItemElement
    {
        private readonly Lazy<bool> _isExplicitInterfaceImplementation;

        public CodeItemEvent()
        {
            _Access = LazyTryDefault( () => CodeEvent != null && !IsExplicitInterfaceImplementation ? CodeEvent.Access : vsCMAccess.vsCMAccessPublic);
            _Attributes = LazyTryDefault( () => CodeEvent?.Attributes);
            _DocComment = LazyTryDefault( () => CodeEvent?.DocComment);
            _isExplicitInterfaceImplementation = LazyTryDefault( () => CodeEvent != null && ExplicitInterfaceImplementationHelper.IsExplicitInterfaceImplementation(CodeEvent));
            _IsStatic = LazyTryDefault( () => CodeEvent != null && CodeEvent.IsShared);
            _TypeString = LazyTryDefault( () => CodeEvent?.Type?.AsString);
        }

        public override KindCodeItem Kind => KindCodeItem.Event;

        public override void LoadLazyInitializedValues()
        {
            base.LoadLazyInitializedValues();

            var ieii = IsExplicitInterfaceImplementation;
        }

        public CodeEvent CodeEvent { get; set; }

        public bool IsExplicitInterfaceImplementation => _isExplicitInterfaceImplementation.Value;

    }
}