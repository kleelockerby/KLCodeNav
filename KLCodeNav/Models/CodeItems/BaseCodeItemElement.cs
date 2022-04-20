using EnvDTE;
using System;

namespace KLCodeNav
{
    public abstract class BaseCodeItemElement : BaseCodeItem
    {
        protected Lazy<vsCMAccess> _Access;
        protected Lazy<CodeElements> _Attributes;
        protected Lazy<string> _DocComment;
        protected Lazy<bool> _IsStatic;
        protected Lazy<string> _TypeString;

        protected BaseCodeItemElement()
        {
            _Access = new Lazy<vsCMAccess>();
            _Attributes = new Lazy<CodeElements>(() => null);
            _DocComment = new Lazy<string>(() => null);
            _IsStatic = new Lazy<bool>();
            _TypeString = new Lazy<string>(() => null);
        }

        public override EditPoint StartPoint => CodeElement != null ? GetStartPointAdjustedForComments(CodeElement.GetStartPoint()) : null;

        public override EditPoint EndPoint => CodeElement?.GetEndPoint().CreateEditPoint();

        public override void LoadLazyInitializedValues()
        {
            base.LoadLazyInitializedValues();

            var ac = Access;
            var at = Attributes;
            var dc = DocComment;
            var isS = IsStatic;
            var ts = TypeString;
        }

        public override void RefreshCachedPositionAndName()
        {
            var startPoint = CodeElement.GetStartPoint();
            var endPoint = CodeElement.GetEndPoint();

            StartLine = startPoint.Line;
            StartOffset = startPoint.AbsoluteCharOffset;
            EndLine = endPoint.Line;
            EndOffset = endPoint.AbsoluteCharOffset;
            Name = CodeElement.Name;
        }

        public CodeElement CodeElement { get; set; }

        public vsCMAccess Access => _Access.Value;

        public CodeElements Attributes => _Attributes.Value;

        public string DocComment => _DocComment.Value;

        public bool IsStatic => _IsStatic.Value;

        public string TypeString => _TypeString.Value;

        protected static Lazy<T> LazyTryDefault<T>(Func<T> func)
        {
            return new Lazy<T>(() => TryDefault(func));
        }

        protected static T TryDefault<T>(Func<T> func)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                //OutputWindowHelper.ExceptionWriteLine($"TryDefault caught an exception on '{func}'", ex);

                return default;
            }
        }

        private static EditPoint GetStartPointAdjustedForComments(TextPoint originalPoint)
        {
            var commentPrefix = CodeCommentHelper.GetCommentPrefix(originalPoint.Parent);
            var point = originalPoint.CreateEditPoint();

            while (point.Line > 1)
            {
                string text = point.GetLines(point.Line - 1, point.Line);

                if (RegexNullSafe.IsMatch(text, @"^\s*" + commentPrefix))
                {
                    point.LineUp();
                    point.StartOfLine();
                }
                else
                {
                    break;
                }
            }

            return point;
        }

    }
}