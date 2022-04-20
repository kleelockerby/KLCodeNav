using EnvDTE;

namespace KLCodeNav
{
    public interface ICodeItem
    {
        KindCodeItem Kind { get; }

        string Name { get; set; }

        int StartLine { get; set; }

        int StartOffset { get; set; }

        EditPoint StartPoint { get; set; }

        int EndLine { get; set; }

        int EndOffset { get; set; }

        EditPoint EndPoint { get; set; }

        void LoadLazyInitializedValues();

        void RefreshCachedPositionAndName();

    }
}