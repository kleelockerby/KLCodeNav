using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;

namespace KLCodeNav
{  
    internal static class HtmlCssParser
    {
        internal static void GetElementPropertiesFromCssAttributes(XmlElement htmlElement, string elementName, CssStylesheet stylesheet, Hashtable localProperties, List<XmlElement> sourceContext)
        {
            string styleFromStylesheet = stylesheet.GetStyle(elementName, sourceContext);

            string styleInline = HtmlToXamlConverter.GetAttribute(htmlElement, "style");

            string style = styleFromStylesheet != null ? styleFromStylesheet : null;
            if (styleInline != null)
            {
                style = style == null ? styleInline : (style + ";" + styleInline);
            }

            if (style != null)
            {
                string[] styleValues = style.Split(';');
                for (int i = 0; i < styleValues.Length; i++)
                {
                    string[] styleNameValue;

                    styleNameValue = styleValues[i].Split(':');
                    if (styleNameValue.Length == 2)
                    {
                        string styleName = styleNameValue[0].Trim().ToLower();
                        string styleValue = HtmlToXamlConverter.UnQuote(styleNameValue[1].Trim()).ToLower();
                        int nextIndex = 0;

                        switch (styleName)
                        {
                            case "font":
                                ParseCssFont(styleValue, localProperties);
                                break;
                            case "font-family":
                                ParseCssFontFamily(styleValue, ref nextIndex, localProperties);
                                break;
                            case "font-size":
                                ParseCssSize(styleValue, ref nextIndex, localProperties, "font-size", true);
                                break;
                            case "font-style":
                                ParseCssFontStyle(styleValue, ref nextIndex, localProperties);
                                break;
                            case "font-weight":
                                ParseCssFontWeight(styleValue, ref nextIndex, localProperties);
                                break;
                            case "font-variant":
                                ParseCssFontVariant(styleValue, ref nextIndex, localProperties);
                                break;
                            case "line-height":
                                ParseCssSize(styleValue, ref nextIndex, localProperties, "line-height", true);
                                break;
                            case "word-spacing":
                                break;
                            case "letter-spacing":
                                break;
                            case "color":
                                ParseCssColor(styleValue, ref nextIndex, localProperties, "color");
                                break;

                            case "text-decoration":
                                ParseCssTextDecoration(styleValue, ref nextIndex, localProperties);
                                break;

                            case "text-transform":
                                ParseCssTextTransform(styleValue, ref nextIndex, localProperties);
                                break;

                            case "background-color":
                                ParseCssColor(styleValue, ref nextIndex, localProperties, "background-color");
                                break;
                            case "background":
                                ParseCssBackground(styleValue, ref nextIndex, localProperties);
                                break;

                            case "text-align":
                                ParseCssTextAlign(styleValue, ref nextIndex, localProperties);
                                break;
                            case "vertical-align":
                                ParseCssVerticalAlign(styleValue, ref nextIndex, localProperties);
                                break;
                            case "text-indent":
                                ParseCssSize(styleValue, ref nextIndex, localProperties, "text-indent", false);
                                break;

                            case "width":
                            case "height":
                                ParseCssSize(styleValue, ref nextIndex, localProperties, styleName, true);
                                break;

                            case "margin":  
                                ParseCssRectangleProperty(styleValue, ref nextIndex, localProperties, styleName);
                                break;
                            case "margin-top":
                            case "margin-right":
                            case "margin-bottom":
                            case "margin-left":
                                ParseCssSize(styleValue, ref nextIndex, localProperties, styleName, true);
                                break;

                            case "padding":
                                ParseCssRectangleProperty(styleValue, ref nextIndex, localProperties, styleName);
                                break;
                            case "padding-top":
                            case "padding-right":
                            case "padding-bottom":
                            case "padding-left":
                                ParseCssSize(styleValue, ref nextIndex, localProperties, styleName, true);
                                break;

                            case "border":
                                ParseCssBorder(styleValue, ref nextIndex, localProperties);
                                break;
                            case "border-style":
                            case "border-width":
                            case "border-color":
                                ParseCssRectangleProperty(styleValue, ref nextIndex, localProperties, styleName);
                                break;
                            case "border-top":
                            case "border-right":
                            case "border-left":
                            case "border-bottom":
                                break;

                            case "border-top-style":
                            case "border-right-style":
                            case "border-left-style":
                            case "border-bottom-style":
                            case "border-top-color":
                            case "border-right-color":
                            case "border-left-color":
                            case "border-bottom-color":
                            case "border-top-width":
                            case "border-right-width":
                            case "border-left-width":
                            case "border-bottom-width":
                                break;

                            case "display":
                                break;

                            case "float":
                                ParseCssFloat(styleValue, ref nextIndex, localProperties);
                                break;
                            case "clear":
                                ParseCssClear(styleValue, ref nextIndex, localProperties);
                                break;

                            default:
                                break;
                        }
                    }
                }
            }
        }

        private static void ParseWhiteSpace(string styleValue, ref int nextIndex)
        {
            while (nextIndex < styleValue.Length && Char.IsWhiteSpace(styleValue[nextIndex]))
            {
                nextIndex++;
            }
        }

        private static bool ParseWord(string word, string styleValue, ref int nextIndex)
        {
            ParseWhiteSpace(styleValue, ref nextIndex);

            for (int i = 0; i < word.Length; i++)
            {
                if (!(nextIndex + i < styleValue.Length && word[i] == styleValue[nextIndex + i]))
                {
                    return false;
                }
            }

            if (nextIndex + word.Length < styleValue.Length && Char.IsLetterOrDigit(styleValue[nextIndex + word.Length]))
            {
                return false;
            }

            nextIndex += word.Length;
            return true;
        }

        private static string ParseWordEnumeration(string[] words, string styleValue, ref int nextIndex)
        {
            for (int i = 0; i < words.Length; i++)
            {
                if (ParseWord(words[i], styleValue, ref nextIndex))
                {
                    return words[i];
                }
            }

            return null;
        }

        private static void ParseWordEnumeration(string[] words, string styleValue, ref int nextIndex, Hashtable localProperties, string attributeName)
        {
            string attributeValue = ParseWordEnumeration(words, styleValue, ref nextIndex);
            if (attributeValue != null)
            {
                localProperties[attributeName] = attributeValue;
            }
        }

        private static string ParseCssSize(string styleValue, ref int nextIndex, bool mustBeNonNegative)
        {
            ParseWhiteSpace(styleValue, ref nextIndex);

            int startIndex = nextIndex;

            if (nextIndex < styleValue.Length && styleValue[nextIndex] == '-')
            {
                nextIndex++;
            }

            if (nextIndex < styleValue.Length && Char.IsDigit(styleValue[nextIndex]))
            {
                while (nextIndex < styleValue.Length && (Char.IsDigit(styleValue[nextIndex]) || styleValue[nextIndex] == '.'))
                {
                    nextIndex++;
                }

                string number = styleValue.Substring(startIndex, nextIndex - startIndex);

                string unit = ParseWordEnumeration(_fontSizeUnits, styleValue, ref nextIndex);
                if (unit == null)
                {
                    unit = "px";     
                }

                if (mustBeNonNegative && styleValue[startIndex] == '-')
                {
                    return "0";
                }
                else
                {
                    return number + unit;
                }
            }

            return null;
        }

        private static void ParseCssSize(string styleValue, ref int nextIndex, Hashtable localValues, string propertyName, bool mustBeNonNegative)
        {
            string length = ParseCssSize(styleValue, ref nextIndex, mustBeNonNegative);
            if (length != null)
            {
                localValues[propertyName] = length;
            }
        }

        private static readonly string[] _colors = new string[]
        {
            "aliceblue", "antiquewhite", "aqua", "aquamarine", "azure", "beige", "bisque", "black", "blanchedalmond",
            "blue", "blueviolet", "brown", "burlywood", "cadetblue", "chartreuse", "chocolate", "coral",
            "cornflowerblue", "cornsilk", "crimson", "cyan", "darkblue", "darkcyan", "darkgoldenrod", "darkgray",
            "darkgreen", "darkkhaki", "darkmagenta", "darkolivegreen", "darkorange", "darkorchid", "darkred",
            "darksalmon", "darkseagreen", "darkslateblue", "darkslategray", "darkturquoise", "darkviolet", "deeppink",
            "deepskyblue", "dimgray", "dodgerblue", "firebrick", "floralwhite", "forestgreen", "fuchsia", "gainsboro",
            "ghostwhite", "gold", "goldenrod", "gray", "green", "greenyellow", "honeydew", "hotpink", "indianred",
            "indigo", "ivory", "khaki", "lavender", "lavenderblush", "lawngreen", "lemonchiffon", "lightblue", "lightcoral",
            "lightcyan", "lightgoldenrodyellow", "lightgreen", "lightgrey", "lightpink", "lightsalmon", "lightseagreen",
            "lightskyblue", "lightslategray", "lightsteelblue", "lightyellow", "lime", "limegreen", "linen", "magenta",
            "maroon", "mediumaquamarine", "mediumblue", "mediumorchid", "mediumpurple", "mediumseagreen", "mediumslateblue",
            "mediumspringgreen", "mediumturquoise", "mediumvioletred", "midnightblue", "mintcream", "mistyrose", "moccasin",
            "navajowhite", "navy", "oldlace", "olive", "olivedrab", "orange", "orangered", "orchid", "palegoldenrod",
            "palegreen", "paleturquoise", "palevioletred", "papayawhip", "peachpuff", "peru", "pink", "plum", "powderblue",
            "purple", "red", "rosybrown", "royalblue", "saddlebrown", "salmon", "sandybrown", "seagreen", "seashell",
            "sienna", "silver", "skyblue", "slateblue", "slategray", "snow", "springgreen", "steelblue", "tan", "teal",
            "thistle", "tomato", "turquoise", "violet", "wheat", "white", "whitesmoke", "yellow", "yellowgreen",
        };

        private static readonly string[] _systemColors = new string[]
        {
            "activeborder", "activecaption", "appworkspace", "background", "buttonface", "buttonhighlight", "buttonshadow",
            "buttontext", "captiontext", "graytext", "highlight", "highlighttext", "inactiveborder", "inactivecaption",
            "inactivecaptiontext", "infobackground", "infotext", "menu", "menutext", "scrollbar", "threeddarkshadow",
            "threedface", "threedhighlight", "threedlightshadow", "threedshadow", "window", "windowframe", "windowtext",
        };

        private static string ParseCssColor(string styleValue, ref int nextIndex)
        {
            ParseWhiteSpace(styleValue, ref nextIndex);

            string color = null;

            if (nextIndex < styleValue.Length)
            {
                int startIndex = nextIndex;
                char character = styleValue[nextIndex];

                if (character == '#')
                {
                    nextIndex++;
                    while (nextIndex < styleValue.Length)
                    {
                        character = Char.ToUpper(styleValue[nextIndex]);
                        if (!('0' <= character && character <= '9' || 'A' <= character && character <= 'F'))
                        {
                            break;
                        }

                        nextIndex++;
                    }

                    if (nextIndex > startIndex + 1)
                    {
                        color = styleValue.Substring(startIndex, nextIndex - startIndex);
                    }
                }
                else if (styleValue.Substring(nextIndex, 3).ToLower() == "rgb")
                {
                    startIndex = 4;
                    string temp_color = styleValue.Substring(startIndex, styleValue.Length - 1 - startIndex);
                    string[] rgb = temp_color.Split(',');
                    color = "#" + (byte.Parse(rgb[0])).ToString("X2") + (byte.Parse(rgb[1])).ToString("X2") + (byte.Parse(rgb[2])).ToString("X2");
                }
                else if (Char.IsLetter(character))
                {
                    color = ParseWordEnumeration(_colors, styleValue, ref nextIndex);
                    if (color == null)
                    {
                        color = ParseWordEnumeration(_systemColors, styleValue, ref nextIndex);
                        if (color != null)
                        {
                            color = "black";
                        }
                    }
                }
            }

            return color;
        }

        private static void ParseCssColor(string styleValue, ref int nextIndex, Hashtable localValues, string propertyName)
        {
            string color = ParseCssColor(styleValue, ref nextIndex);
            if (color != null)
            {
                localValues[propertyName] = color;
            }
        }

        private static readonly string[] _fontGenericFamilies = new string[] {"serif", "sans-serif", "monospace", "cursive", "fantasy"};
        private static readonly string[] _fontStyles = new string[] {"normal", "italic", "oblique"};
        private static readonly string[] _fontVariants = new string[] {"normal", "small-caps"};
        private static readonly string[] _fontWeights = new string[] {"normal", "bold", "bolder", "lighter", "100", "200", "300", "400", "500", "600", "700", "800", "900"};
        private static readonly string[] _fontAbsoluteSizes = new string[] {"xx-small", "x-small", "small", "medium", "large", "x-large", "xx-large"};
        private static readonly string[] _fontRelativeSizes = new string[] {"larger", "smaller"};
        private static readonly string[] _fontSizeUnits = new string[] {"px", "mm", "cm", "in", "pt", "pc", "em", "ex", "%"};

        private static void ParseCssFont(string styleValue, Hashtable localProperties)
        {
            int nextIndex = 0;

            ParseCssFontStyle(styleValue, ref nextIndex, localProperties);
            ParseCssFontVariant(styleValue, ref nextIndex, localProperties);
            ParseCssFontWeight(styleValue, ref nextIndex, localProperties);

            ParseCssSize(styleValue, ref nextIndex, localProperties, "font-size", true);

            ParseWhiteSpace(styleValue, ref nextIndex);
            if (nextIndex < styleValue.Length && styleValue[nextIndex] == '/')
            {
                nextIndex++;
                ParseCssSize(styleValue, ref nextIndex, localProperties, "line-height", true);
            }

            ParseCssFontFamily(styleValue, ref nextIndex, localProperties);
        }

        private static void ParseCssFontStyle(string styleValue, ref int nextIndex, Hashtable localProperties)
        {
            ParseWordEnumeration(_fontStyles, styleValue, ref nextIndex, localProperties, "font-style");
        }

        private static void ParseCssFontVariant(string styleValue, ref int nextIndex, Hashtable localProperties)
        {
            ParseWordEnumeration(_fontVariants, styleValue, ref nextIndex, localProperties, "font-variant");
        }

        private static void ParseCssFontWeight(string styleValue, ref int nextIndex, Hashtable localProperties)
        {
            ParseWordEnumeration(_fontWeights, styleValue, ref nextIndex, localProperties, "font-weight");
        }

        private static void ParseCssFontFamily(string styleValue, ref int nextIndex, Hashtable localProperties)
        {
            string fontFamilyList = null;

            while (nextIndex < styleValue.Length)
            {
                string fontFamily = ParseWordEnumeration(_fontGenericFamilies, styleValue, ref nextIndex);

                if (fontFamily == null)
                {
                    if (nextIndex < styleValue.Length && (styleValue[nextIndex] == '"' || styleValue[nextIndex] == '\''))
                    {
                        char quote = styleValue[nextIndex];

                        nextIndex++;

                        int startIndex = nextIndex;

                        while (nextIndex < styleValue.Length && styleValue[nextIndex] != quote)
                        {
                            nextIndex++;
                        }

                        fontFamily = '"' + styleValue.Substring(startIndex, nextIndex - startIndex) + '"';
                    }

                    if (fontFamily == null)
                    {
                        int startIndex = nextIndex;
                        while (nextIndex < styleValue.Length && styleValue[nextIndex] != ',' && styleValue[nextIndex] != ';')
                        {
                            nextIndex++;
                        }

                        if (nextIndex > startIndex)
                        {
                            fontFamily = styleValue.Substring(startIndex, nextIndex - startIndex).Trim();
                            if (fontFamily.Length == 0)
                            {
                                fontFamily = null;
                            }
                        }
                    }
                }

                ParseWhiteSpace(styleValue, ref nextIndex);
                if (nextIndex < styleValue.Length && styleValue[nextIndex] == ',')
                {
                    nextIndex++;
                }

                if (fontFamily != null)
                {
                    if (fontFamilyList == null && fontFamily.Length > 0)
                    {
                        if (fontFamily[0] == '"' || fontFamily[0] == '\'')
                        {
                            fontFamily = fontFamily.Substring(1, fontFamily.Length - 2);
                        }
                        else
                        {
                        }

                        fontFamilyList = fontFamily;
                    }
                }
                else
                {
                    break;
                }
            }

            if (fontFamilyList != null)
            {
                localProperties["font-family"] = fontFamilyList;
            }
        }

        private static readonly string[] _listStyleTypes = new string[] {"disc", "circle", "square", "decimal", "lower-roman", "upper-roman", "lower-alpha", "upper-alpha", "none"};
        private static readonly string[] _listStylePositions = new string[] {"inside", "outside"};

        private static void ParseCssListStyle(string styleValue, Hashtable localProperties)
        {
            int nextIndex = 0;

            while (nextIndex < styleValue.Length)
            {
                string listStyleType = ParseCssListStyleType(styleValue, ref nextIndex);
                if (listStyleType != null)
                {
                    localProperties["list-style-type"] = listStyleType;
                }
                else
                {
                    string listStylePosition = ParseCssListStylePosition(styleValue, ref nextIndex);
                    if (listStylePosition != null)
                    {
                        localProperties["list-style-position"] = listStylePosition;
                    }
                    else
                    {
                        string listStyleImage = ParseCssListStyleImage(styleValue, ref nextIndex);
                        if (listStyleImage != null)
                        {
                            localProperties["list-style-image"] = listStyleImage;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        private static string ParseCssListStyleType(string styleValue, ref int nextIndex)
        {
            return ParseWordEnumeration(_listStyleTypes, styleValue, ref nextIndex);
        }

        private static string ParseCssListStylePosition(string styleValue, ref int nextIndex)
        {
            return ParseWordEnumeration(_listStylePositions, styleValue, ref nextIndex);
        }

        private static string ParseCssListStyleImage(string styleValue, ref int nextIndex)
        {
            return null;
        }

        private static readonly string[] _textDecorations = new string[] {"none", "underline", "overline", "line-through", "blink"};

        private static void ParseCssTextDecoration(string styleValue, ref int nextIndex, Hashtable localProperties)
        {
            for (int i = 1; i < _textDecorations.Length; i++)
            {
                localProperties["text-decoration-" + _textDecorations[i]] = "false";
            }

            while (nextIndex < styleValue.Length)
            {
                string decoration = ParseWordEnumeration(_textDecorations, styleValue, ref nextIndex);
                if (decoration == null || decoration == "none")
                {
                    break;
                }

                localProperties["text-decoration-" + decoration] = "true";
            }
        }

        private static readonly string[] _textTransforms = new string[] {"none", "capitalize", "uppercase", "lowercase"};

        private static void ParseCssTextTransform(string styleValue, ref int nextIndex, Hashtable localProperties)
        {
            ParseWordEnumeration(_textTransforms, styleValue, ref nextIndex, localProperties, "text-transform");
        }

        private static readonly string[] _textAligns = new string[] {"left", "right", "center", "justify"};

        private static void ParseCssTextAlign(string styleValue, ref int nextIndex, Hashtable localProperties)
        {
            ParseWordEnumeration(_textAligns, styleValue, ref nextIndex, localProperties, "text-align");
        }

        private static readonly string[] _verticalAligns = new string[] {"baseline", "sub", "super", "top", "text-top", "middle", "bottom", "text-bottom"};

        private static void ParseCssVerticalAlign(string styleValue, ref int nextIndex, Hashtable localProperties)
        {
            ParseWordEnumeration(_verticalAligns, styleValue, ref nextIndex, localProperties, "vertical-align");
        }

        private static readonly string[] _floats = new string[] {"left", "right", "none"};

        private static void ParseCssFloat(string styleValue, ref int nextIndex, Hashtable localProperties)
        {
            ParseWordEnumeration(_floats, styleValue, ref nextIndex, localProperties, "float");
        }

        private static readonly string[] _clears = new string[] {"none", "left", "right", "both"};

        private static void ParseCssClear(string styleValue, ref int nextIndex, Hashtable localProperties)
        {
            ParseWordEnumeration(_clears, styleValue, ref nextIndex, localProperties, "clear");
        }

        private static bool ParseCssRectangleProperty(string styleValue, ref int nextIndex, Hashtable localProperties, string propertyName)
        {
            Debug.Assert(propertyName == "margin" || propertyName == "padding" || propertyName == "border-width" || propertyName == "border-style" || propertyName == "border-color");

            string value = propertyName == "border-color" ? ParseCssColor(styleValue, ref nextIndex) : propertyName == "border-style" ? ParseCssBorderStyle(styleValue, ref nextIndex) : ParseCssSize(styleValue, ref nextIndex, true);
            if (value != null)
            {
                localProperties[propertyName + "-top"] = value;
                localProperties[propertyName + "-bottom"] = value;
                localProperties[propertyName + "-right"] = value;
                localProperties[propertyName + "-left"] = value;
                value = propertyName == "border-color" ? ParseCssColor(styleValue, ref nextIndex) : propertyName == "border-style" ? ParseCssBorderStyle(styleValue, ref nextIndex) : ParseCssSize(styleValue, ref nextIndex, true);
                if (value != null)
                {
                    localProperties[propertyName + "-right"] = value;
                    localProperties[propertyName + "-left"] = value;
                    value = propertyName == "border-color" ? ParseCssColor(styleValue, ref nextIndex) : propertyName == "border-style" ? ParseCssBorderStyle(styleValue, ref nextIndex) : ParseCssSize(styleValue, ref nextIndex, true);
                    if (value != null)
                    {
                        localProperties[propertyName + "-bottom"] = value;
                        value = propertyName == "border-color" ? ParseCssColor(styleValue, ref nextIndex) : propertyName == "border-style" ? ParseCssBorderStyle(styleValue, ref nextIndex) : ParseCssSize(styleValue, ref nextIndex, true);
                        if (value != null)
                        {
                            localProperties[propertyName + "-left"] = value;
                        }
                    }
                }

                return true;
            }

            return false;
        }

        private static void ParseCssBorder(string styleValue, ref int nextIndex, Hashtable localProperties)
        {
            while (
                ParseCssRectangleProperty(styleValue, ref nextIndex, localProperties, "border-width") ||
                ParseCssRectangleProperty(styleValue, ref nextIndex, localProperties, "border-style") ||
                ParseCssRectangleProperty(styleValue, ref nextIndex, localProperties, "border-color"))
            {
            }
        }

        private static readonly string[] _borderStyles = new string[] {"none", "dotted", "dashed", "solid", "double", "groove", "ridge", "inset", "outset"};

        private static string ParseCssBorderStyle(string styleValue, ref int nextIndex)
        {
            return ParseWordEnumeration(_borderStyles, styleValue, ref nextIndex);
        }


        private static string[] _blocks = new string[] {"block", "inline", "list-item", "none"};

        private static void ParseCssBackground(string styleValue, ref int nextIndex, Hashtable localValues)
        {
        }
    }


    internal class CssStylesheet
    {
        public CssStylesheet(XmlElement htmlElement)
        {
            if (htmlElement != null)
            {
                this.DiscoverStyleDefinitions(htmlElement);
            }
        }

        public void DiscoverStyleDefinitions(XmlElement htmlElement)
        {
            if (htmlElement.LocalName.ToLower() == "link")
            {
                return;
            }

            if (htmlElement.LocalName.ToLower() != "style")
            {
                for (XmlNode htmlChildNode = htmlElement.FirstChild; htmlChildNode != null; htmlChildNode = htmlChildNode.NextSibling)
                {
                    if (htmlChildNode is XmlElement)
                    {
                        this.DiscoverStyleDefinitions((XmlElement) htmlChildNode);
                    }
                }

                return;
            }

            StringBuilder stylesheetBuffer = new StringBuilder();

            for (XmlNode htmlChildNode = htmlElement.FirstChild; htmlChildNode != null; htmlChildNode = htmlChildNode.NextSibling)
            {
                if (htmlChildNode is XmlText || htmlChildNode is XmlComment)
                {
                    stylesheetBuffer.Append(RemoveComments(htmlChildNode.Value));
                }
            }

            int nextCharacterIndex = 0;
            while (nextCharacterIndex < stylesheetBuffer.Length)
            {
                int selectorStart = nextCharacterIndex;
                while (nextCharacterIndex < stylesheetBuffer.Length && stylesheetBuffer[nextCharacterIndex] != '{')
                {
                    if (stylesheetBuffer[nextCharacterIndex] == '@')
                    {
                        while (nextCharacterIndex < stylesheetBuffer.Length && stylesheetBuffer[nextCharacterIndex] != ';')
                        {
                            nextCharacterIndex++;
                        }

                        selectorStart = nextCharacterIndex + 1;
                    }

                    nextCharacterIndex++;
                }

                if (nextCharacterIndex < stylesheetBuffer.Length)
                {
                    int definitionStart = nextCharacterIndex;
                    while (nextCharacterIndex < stylesheetBuffer.Length && stylesheetBuffer[nextCharacterIndex] != '}')
                    {
                        nextCharacterIndex++;
                    }

                    if (nextCharacterIndex - definitionStart > 2)
                    {
                        this.AddStyleDefinition(
                            stylesheetBuffer.ToString(selectorStart, definitionStart - selectorStart),
                            stylesheetBuffer.ToString(definitionStart + 1, nextCharacterIndex - definitionStart - 2));
                    }

                    if (nextCharacterIndex < stylesheetBuffer.Length)
                    {
                        Debug.Assert(stylesheetBuffer[nextCharacterIndex] == '}');
                        nextCharacterIndex++;
                    }
                }
            }
        }

        private string RemoveComments(string text)
        {
            int commentStart = text.IndexOf("/*");
            if (commentStart < 0)
            {
                return text;
            }

            int commentEnd = text.IndexOf("*/", commentStart + 2);
            if (commentEnd < 0)
            {
                return text.Substring(0, commentStart);
            }

            return text.Substring(0, commentStart) + " " + RemoveComments(text.Substring(commentEnd + 2));
        }


        public void AddStyleDefinition(string selector, string definition)
        {
            selector = selector.Trim().ToLower();
            definition = definition.Trim().ToLower();
            if (selector.Length == 0 || definition.Length == 0)
            {
                return;
            }

            if (_styleDefinitions == null)
            {
                _styleDefinitions = new List<StyleDefinition>();
            }

            string[] simpleSelectors = selector.Split(',');

            for (int i = 0; i < simpleSelectors.Length; i++)
            {
                string simpleSelector = simpleSelectors[i].Trim();
                if (simpleSelector.Length > 0)
                {
                    _styleDefinitions.Add(new StyleDefinition(simpleSelector, definition));
                }
            }
        }

        public string GetStyle(string elementName, List<XmlElement> sourceContext)
        {
            Debug.Assert(sourceContext.Count > 0);
            Debug.Assert(elementName == sourceContext[sourceContext.Count - 1].LocalName);

            if (_styleDefinitions != null)
            {
                for (int i = _styleDefinitions.Count - 1; i >= 0; i--)
                {
                    string selector = _styleDefinitions[i].Selector;

                    string[] selectorLevels = selector.Split(' ');

                    int indexInSelector = selectorLevels.Length - 1;
                    int indexInContext = sourceContext.Count - 1;
                    string selectorLevel = selectorLevels[indexInSelector].Trim();

                    if (MatchSelectorLevel(selectorLevel, sourceContext[sourceContext.Count - 1]))
                    {
                        return _styleDefinitions[i].Definition;
                    }
                }
            }

            return null;
        }

        private bool MatchSelectorLevel(string selectorLevel, XmlElement xmlElement)
        {
            if (selectorLevel.Length == 0)
            {
                return false;
            }

            int indexOfDot = selectorLevel.IndexOf('.');
            int indexOfPound = selectorLevel.IndexOf('#');

            string selectorClass = null;
            string selectorId = null;
            string selectorTag = null;
            if (indexOfDot >= 0)
            {
                if (indexOfDot > 0)
                {
                    selectorTag = selectorLevel.Substring(0, indexOfDot);
                }

                selectorClass = selectorLevel.Substring(indexOfDot + 1);
            }
            else if (indexOfPound >= 0)
            {
                if (indexOfPound > 0)
                {
                    selectorTag = selectorLevel.Substring(0, indexOfPound);
                }

                selectorId = selectorLevel.Substring(indexOfPound + 1);
            }
            else
            {
                selectorTag = selectorLevel;
            }

            if (selectorTag != null && selectorTag != xmlElement.LocalName)
            {
                return false;
            }

            if (selectorId != null && HtmlToXamlConverter.GetAttribute(xmlElement, "id") != selectorId)
            {
                return false;
            }

            if (selectorClass != null && HtmlToXamlConverter.GetAttribute(xmlElement, "class") != selectorClass)
            {
                return false;
            }

            return true;
        }

        private class StyleDefinition
        {
            public StyleDefinition(string selector, string definition)
            {
                this.Selector = selector;
                this.Definition = definition;
            }

            public string Selector;

            public string Definition;
        }

        private List<StyleDefinition> _styleDefinitions;
    }
}