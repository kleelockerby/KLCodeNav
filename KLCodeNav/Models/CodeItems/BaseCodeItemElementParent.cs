using EnvDTE;
using System;

namespace KLCodeNav
{
    public abstract class BaseCodeItemElementParent : BaseCodeItemElement
    {
        protected Lazy<string> _Namespace;

        private bool _isExpanded = true;

        protected BaseCodeItemElementParent()
        {
            Children = new CodeItems();

            _Namespace = new Lazy<string>(() => null);
        }

        public override void LoadLazyInitializedValues()
        {
            base.LoadLazyInitializedValues();

            var ns = Namespace;
        }

        public event EventHandler IsExpandedChanged;

        public CodeItems Children { get; private set; }

        public EditPoint InsertPoint => CodeElement?.GetStartPoint(vsCMPart.vsCMPartBody).CreateEditPoint();

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    //RaisePropertyChanged();

                    IsExpandedChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string Namespace => _Namespace.Value;

    }
}