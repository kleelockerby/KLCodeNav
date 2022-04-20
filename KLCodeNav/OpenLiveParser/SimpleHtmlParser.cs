using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace KLCodeNav
{
    public class SimpleHtmlParser : IElementSource
    {
        private bool supportTrailingEnd = false;

        private readonly Stack<Element> elementStack = new Stack<Element>(5);
        private readonly List<Element> peekElements = new List<Element>();

        private readonly string data;       
        private int pos = 0;      

        private static readonly Regex comment = new Regex(@"<!--.*?--\s*>", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex directive = new Regex(@"<!(?!--).*?>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex endScript = new Regex(@"</script\s*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex endStyle = new Regex(@"</style\s*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex begin = new Regex(@"<(?<tagname>[a-z][a-z0-9\.\-_:]*)", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
        private static readonly Regex attrName = new Regex(@"\s*([a-z][a-z0-9\.\-_:]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex quotedAttrValue = new Regex(@"\s*=\s*([""'])(.*?)\1", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex unquotedAttrValue = new Regex(@"\s*=\s*([^\s>]+)", RegexOptions.Compiled);
        private static readonly Regex endBeginTag = new Regex(@"\s*(/)?>", RegexOptions.Compiled);
        private static readonly Regex end = new Regex(@"</([a-z][a-z0-9\.\-_:]*)\s*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly StatefulMatcher commentMatcher;
        private readonly StatefulMatcher directiveMatcher;
        private readonly StatefulMatcher beginMatcher;
        private readonly StatefulMatcher attrNameMatcher;
        private readonly StatefulMatcher quotedAttrValueMatcher;
        private readonly StatefulMatcher unquotedAttrValueMatcher;
        private readonly StatefulMatcher endBeginTagMatcher;
        private readonly StatefulMatcher endMatcher;

        public SimpleHtmlParser(string data)
        {
            this.data = data;

            commentMatcher = new StatefulMatcher(data, comment);
            directiveMatcher = new StatefulMatcher(data, directive);
            beginMatcher = new StatefulMatcher(data, begin);
            endMatcher = new StatefulMatcher(data, end);
            attrNameMatcher = new StatefulMatcher(data, attrName);
            quotedAttrValueMatcher = new StatefulMatcher(data, quotedAttrValue);
            unquotedAttrValueMatcher = new StatefulMatcher(data, unquotedAttrValue);
            endBeginTagMatcher = new StatefulMatcher(data, endBeginTag);
        }

        public int Position
        {
            get
            {
                if (peekElements.Count != 0)
                    return peekElements[0].Offset;
                if (elementStack.Count != 0)
                    return elementStack.Peek().Offset;
                else
                    return pos;
            }
        }

        public Element Peek(int offset)
        {
            Element e;
            while (peekElements.Count <= offset && (e = Next(false)) != null)
                peekElements.Add(e);

            if (peekElements.Count > offset)
                return peekElements[offset];
            else
                return null;
        }

        public Element Next()
        {
            return Next(true);
        }
        private Element Next(bool allowPeekElement)
        {
            if (allowPeekElement && peekElements.Count > 0)
            {
                Element peekElement = peekElements[0];
                peekElements.RemoveAt(0);
                return peekElement;
            }

            if (elementStack.Count != 0)
            {
                return elementStack.Pop();
            }

            int dataLen = data.Length;
            if (dataLen == pos)
            {
                return null;
            }

            int tokenStart = pos;

            while (true)
            {
                while (pos < dataLen && data[pos] != '<')
                    pos++;

                if (pos >= dataLen)
                {
                    if (tokenStart != pos)
                        return new Text(data, tokenStart, pos - tokenStart);
                    else
                        return null;
                }

                int oldPos = pos;

                Element element;
                EndTag trailingEnd;
                int len = ParseMarkup(out element, out trailingEnd);
                if (len >= 0)
                {
                    pos += len;

                    if (trailingEnd != null)
                    {
                        elementStack.Push(trailingEnd);
                    }
                    else if (element is BeginTag)
                    {
                        Regex consumeTextUntil = null;

                        BeginTag tag = (BeginTag)element;
                        if (tag.NameEquals("script"))
                            consumeTextUntil = endScript;
                        else if (tag.NameEquals("style"))
                            consumeTextUntil = endStyle;

                        if (consumeTextUntil != null)
                        {
                            int structuredTextLen = ConsumeStructuredText(data, pos, consumeTextUntil);
                            pos += structuredTextLen;
                        }
                    }

                    elementStack.Push(element);
                    if (oldPos != tokenStart)
                    {
                        elementStack.Push(new Text(data, tokenStart, oldPos - tokenStart));
                    }

                    return elementStack.Pop();
                }
                else
                {
                    pos++;
                    continue;
                }
            }
        }

        public string CollectTextUntil(string endTagName)
        {
            int tagCount = 1;
            StringBuilder buf = new StringBuilder();

            while (true)
            {
                Element el = Next();

                if (el == null)
                {
                    break;
                }
                if (el is BeginTag && ((BeginTag)el).NameEquals(endTagName))
                {
                    tagCount++;
                }
                else if (el is EndTag && ((EndTag)el).NameEquals(endTagName))
                {
                    if (--tagCount == 0)
                        break;
                }
                else if (el is Text)
                {
                    if (buf.Length != 0)
                        buf.Append(' ');
                    buf.Append(((Text)el).ToString());
                }
            }

            return buf.ToString();
        }

        public string CollectHtmlUntil(string endTagName)
        {
            int tagCount = 1;
            StringBuilder buf = new StringBuilder();

            while (true)
            {
                Element el = Next();

                if (el == null)
                {
                    break;
                }
                if (el is BeginTag && ((BeginTag)el).NameEquals(endTagName))
                {
                    tagCount++;
                }
                else if (el is EndTag && ((EndTag)el).NameEquals(endTagName))
                {
                    if (--tagCount == 0)
                        break;
                }
                buf.Append(data, el.Offset, el.Length);
            }

            return buf.ToString();
        }

        private int ParseMarkup(out Element element, out EndTag trailingEnd)
        {
            trailingEnd = null;

            Match m;

            m = commentMatcher.Match(pos);
            if (m != null)
            {
                element = new Comment(data, pos, m.Length);
                return m.Length;
            }

            m = directiveMatcher.Match(pos);
            if (m != null)
            {
                element = new MarkupDirective(data, pos, m.Length);
                return m.Length;
            }

            m = endMatcher.Match(pos);
            if (m != null)
            {
                element = new EndTag(data, pos, m.Length, m.Groups[1].Value);
                return m.Length;
            }

            m = beginMatcher.Match(pos);
            if (m != null)
            {
                return ParseBeginTag(m, out element, out trailingEnd);
            }

            element = null;
            return -1;
        }

        private int ParseBeginTag(Match beginMatch, out Element element, out EndTag trailingEnd)
        {
            trailingEnd = null;

            Group tagNameGroup = beginMatch.Groups["tagname"];
            string tagName = tagNameGroup.Value;

            int tagPos = tagNameGroup.Index + tagNameGroup.Length;

            ArrayList attributes = null;
            LazySubstring extraResidue = null;
            bool isComplete = false;

            while (true)
            {
                Match match = endBeginTagMatcher.Match(tagPos);
                if (match != null)
                {
                    tagPos += match.Length;
                    if (match.Groups[1].Success)
                    {
                        isComplete = true;
                        if (supportTrailingEnd)
                            trailingEnd = new EndTag(data, tagPos, 0, tagName, true);
                    }
                    break;
                }

                match = attrNameMatcher.Match(tagPos);
                if (match == null)
                {
                    int residueStart = tagPos;
                    int residueEnd;

                    residueEnd = tagPos = data.IndexOfAny(new char[] { '<', '>' }, tagPos);
                    if (tagPos == -1)
                    {
                        residueEnd = tagPos = data.Length;
                    }
                    else if (data[tagPos] == '>')
                    {
                        tagPos++;
                    }
                    else
                    {
                        Debug.Assert(data[tagPos] == '<');
                    }

                    extraResidue = residueStart < residueEnd ? new LazySubstring(data, residueStart, residueEnd - residueStart) : null;
                    break;
                }
                else
                {
                    tagPos += match.Length;
                    LazySubstring attrName = new LazySubstring(data, match.Groups[1].Index, match.Groups[1].Length);
                    LazySubstring attrValue = null;
                    match = quotedAttrValueMatcher.Match(tagPos);
                    if (match != null)
                    {
                        attrValue = new LazySubstring(data, match.Groups[2].Index, match.Groups[2].Length);
                        tagPos += match.Length;
                    }
                    else
                    {
                        match = unquotedAttrValueMatcher.Match(tagPos);
                        if (match != null)
                        {
                            attrValue = new LazySubstring(data, match.Groups[1].Index, match.Groups[1].Length);
                            tagPos += match.Length;
                        }
                    }

                    if (attributes == null)
                        attributes = new ArrayList();
                    attributes.Add(new Attr(attrName, attrValue));
                }
            }

            int len = tagPos - beginMatch.Index;
            element = new BeginTag(data, beginMatch.Index, len, tagName, attributes == null ? null : (Attr[])attributes.ToArray(typeof(Attr)), isComplete, extraResidue);
            return len;
        }

        private int ConsumeStructuredText(string data, int offset, Regex stopAt)
        {
            Match match = stopAt.Match(data, offset);
            int end = match.Success ? match.Index : data.Length;

            IElementSource source = (stopAt == endScript) ? (IElementSource)new JavascriptParser(data, offset, end - offset) : (IElementSource)new CssParser(data, offset, end - offset);
            Stack stack = new Stack();
            Element element;
            int last = pos;
            while (null != (element = source.Next()))
            {
                stack.Push(element);
            }
            foreach (Element el in stack)
            {
                elementStack.Push(el);
            }

            return end - offset;
        }

        private class StatefulMatcher
        {
#if DEBUG
            bool warned;
#endif

            private readonly string input;
            private readonly Regex regex;
            private int lastStartOffset;
            private Match lastMatch;

            public StatefulMatcher(string input, Regex regex)
            {
                this.input = input;
                this.regex = regex;
                this.lastStartOffset = int.MaxValue;
                this.lastMatch = null;
#if DEBUG
                this.warned = false;
#endif
            }

            public Match Match(int pos)
            {
                if (lastMatch == null || (lastMatch.Success && lastMatch.Index < pos) || lastStartOffset > pos)
                {
#if DEBUG
                    if (lastStartOffset > pos && lastStartOffset != int.MaxValue)
                    {
                        Debug.Assert(!warned, "StatefulMatcher moving backwards; this will work but is inefficient");
                        warned = true;
                    }
#endif
                    PerformMatch(pos);
                }

                if (lastMatch.Success && pos == lastMatch.Index)
                    return lastMatch;
                else
                    return null;
            }

            private void PerformMatch(int pos)
            {
                lastStartOffset = pos;
                lastMatch = regex.Match(input, pos);
            }
        }

        public static void Create()
        {
            if (comment == null)
            {

            }
        }
    }

    internal class LazySubstring
    {
        private readonly string baseString;
        private readonly int offset;
        private readonly int length;

        private string substring;

        public LazySubstring(string baseString, int offset, int length)
        {
            this.baseString = baseString;
            this.offset = offset;
            this.length = length;
        }

        public string Value
        {
            get
            {
                if (substring == null)
                    substring = baseString.Substring(offset, length);
                return substring;
            }
        }

        public int Offset
        {
            get { return offset; }
        }

        public int Length
        {
            get { return length; }
        }

        public static LazySubstring MaybeCreate(string val)
        {
            if (val == null)
                return null;
            else
                return new LazySubstring(val, 0, val.Length);
        }
    }

}
