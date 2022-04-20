using EnvDTE;
using System;
using System.Linq;

namespace KLCodeNav
{
    public class CodeItemRegion : BaseCodeItem
    {
        private bool _isExpanded = true;

        public CodeItemRegion()
        {
            Children = new CodeItems();
        }

        public override KindCodeItem Kind => KindCodeItem.Region;

        public event EventHandler IsExpandedChanged;

        public CodeItems Children { get; private set; }

        public EditPoint InsertPoint
        {
            get
            {
                var startPoint = StartPoint;
                if (startPoint != null)
                {
                    var insertPoint = startPoint.CreateEditPoint();
                    insertPoint.LineDown();
                    return insertPoint;
                }

                return null;
            }
        }

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

        public bool IsEmpty
        {
            get
            {
                if (Children.Any())
                {
                    return false;
                }

                var start = StartPoint.CreateEditPoint();
                start.EndOfLine();

                var end = EndPoint.CreateEditPoint();
                end.StartOfLine();

                var text = start.GetText(end);

                return string.IsNullOrWhiteSpace(text);
            }
        }

        public bool IsInvalidated { get; set; }

        public bool IsPseudoGroup { get; set; }

    }
}