using System.Collections;
using System.Globalization;
using System.Text;

namespace KLCodeNav
{
    public abstract class ReplaceOperation
    {
        public ReplaceOperation()
        {
        }

        protected virtual string Replace(Element el)
        {
            return el.RawText;
        }

        public virtual string Execute(string html)
        {
            SimpleHtmlParser parser = new SimpleHtmlParser(html);
            StringBuilder output = new StringBuilder(html.Length);
            Element next;
            while (null != (next = parser.Next()))
                output.Append(Replace(next));
            return output.ToString();
        }
    }

    public abstract class AttributeAndLiteralReplaceOperation : ReplaceOperation
    {
        private Hashtable elements = new Hashtable();

        public AttributeAndLiteralReplaceOperation()
        {
        }

        public void AddPattern(string tagName, string attrName)
        {
            tagName = tagName.ToLowerInvariant();
            if (!elements.ContainsKey(tagName))
                elements[tagName] = new Hashtable();
            ((Hashtable)elements[tagName.ToLowerInvariant()]).Add(attrName.ToLowerInvariant(), null);
        }

        protected abstract string OnMatchingAttr(BeginTag tag, Attr attr);
        protected abstract string OnScriptLiteral(ScriptLiteral literal);

        protected override string Replace(Element el)
        {
            if (el is BeginTag)
            {
                BeginTag tag = (BeginTag)el;
                string lowerName = tag.Name.ToLowerInvariant();
                if (elements.ContainsKey(lowerName))
                {
                    Hashtable attrs = elements[lowerName] as Hashtable;
                    if (attrs != null)
                    {
                        foreach (Attr attr in tag.Attributes)
                        {
                            if (attrs.Contains(attr.Name.ToLowerInvariant()))
                                return OnMatchingAttr(tag, attr);
                        }
                    }

                }
            }
            else if (el is ScriptLiteral)
            {
                return OnScriptLiteral((ScriptLiteral)el);
            }

            return base.Replace(el);
        }

    }

}
