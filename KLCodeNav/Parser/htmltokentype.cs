namespace KLCodeNav
{
    internal enum HtmlTokenType
    {
        OpeningTagStart,
        ClosingTagStart,
        TagEnd,
        EmptyTagEnd,
        EqualSign,
        Name,
        Atom,       
        Text,     
        Comment,
        EOF,
    }
}