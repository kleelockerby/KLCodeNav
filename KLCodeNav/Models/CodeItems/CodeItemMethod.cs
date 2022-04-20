using EnvDTE;
using EnvDTE80;

using System;
using System.Collections.Generic;
using System.Linq;

namespace KLCodeNav
{
    public class CodeItemMethod : BaseCodeItemElement
    {
        private readonly Lazy<int> _complexity;
        private readonly Lazy<bool> _isConstructor;
        private readonly Lazy<bool> _isDestructor;
        private readonly Lazy<bool> _isExplicitInterfaceImplementation;
        private readonly Lazy<vsCMOverrideKind> _overrideKind;
        private readonly Lazy<IEnumerable<CodeParameter>> _parameters;

        public CodeItemMethod()
        {
            _Access = LazyTryDefault(
                () => CodeFunction != null && !(IsStatic && IsConstructor) && !IsExplicitInterfaceImplementation ? CodeFunction.Access : vsCMAccess.vsCMAccessPublic);

            _Attributes = LazyTryDefault(
                () => CodeFunction?.Attributes);

            _complexity = LazyTryDefault(
                () => CodeElementHelper.CalculateComplexity(CodeElement));

            _DocComment = LazyTryDefault(
                () => CodeFunction?.DocComment);

            _isConstructor = LazyTryDefault(
                () => CodeFunction != null && CodeFunction.FunctionKind == vsCMFunction.vsCMFunctionConstructor);

            _isDestructor = LazyTryDefault(
                () => CodeFunction != null && CodeFunction.FunctionKind == vsCMFunction.vsCMFunctionDestructor);

            _isExplicitInterfaceImplementation = LazyTryDefault(
                () => CodeFunction != null && ExplicitInterfaceImplementationHelper.IsExplicitInterfaceImplementation(CodeFunction));

            _IsStatic = LazyTryDefault(
                () => CodeFunction != null && CodeFunction.IsShared);

            _overrideKind = LazyTryDefault(
                () => CodeFunction?.OverrideKind ?? vsCMOverrideKind.vsCMOverrideKindNone);

            _parameters = LazyTryDefault(
                () => CodeFunction?.Parameters?.Cast<CodeParameter>().ToList() ?? Enumerable.Empty<CodeParameter>());

            _TypeString = LazyTryDefault(
                () => CodeFunction?.Type?.AsString);
        }

        public override KindCodeItem Kind
        {
            get
            {
                if (IsConstructor)
                {
                    return KindCodeItem.Constructor;
                }

                if (IsDestructor)
                {
                    return KindCodeItem.Destructor;
                }

                return KindCodeItem.Method;
            }
        }

        public override void LoadLazyInitializedValues()
        {
            base.LoadLazyInitializedValues();

            var c = Complexity;
            var ic = IsConstructor;
            var id = IsDestructor;
            var ieii = IsExplicitInterfaceImplementation;
            var ok = OverrideKind;
            var p = Parameters;
        }

        public CodeFunction2 CodeFunction { get; set; }

        public int Complexity => _complexity.Value;

        public bool IsConstructor => _isConstructor.Value;

        public bool IsDestructor => _isDestructor.Value;

        public bool IsExplicitInterfaceImplementation => _isExplicitInterfaceImplementation.Value;

        public vsCMOverrideKind OverrideKind => _overrideKind.Value;

        public IEnumerable<CodeParameter> Parameters => _parameters.Value;

    }
}