using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace KLCodeNav
{
    public static class StringLiteralHelper
    {
        public static string StringFromCSharpLiteral(string source)
        {
            var sb = new StringBuilder(source.Length);
            int pos = 0;
            while (pos < source.Length)
            {
                char c = source[pos];
                if (c == '\\')
                {
                    pos++;
                    if (pos >= source.Length) throw new ArgumentException("Missing escape sequence");
                    switch (source[pos])
                    {
                        case '\'':
                            c = '\'';
                            break;
                        case '\"':
                            c = '\"';
                            break;
                        case '\\':
                            c = '\\';
                            break;
                        case '0':
                            c = '\0';
                            break;
                        case 'a':
                            c = '\a';
                            break;
                        case 'b':
                            c = '\b';
                            break;
                        case 'f':
                            c = '\f';
                            break;
                        case 'n':
                            c = ' ';
                            break;
                        case 'r':
                            c = ' ';
                            break;
                        case 't':
                            c = '\t';
                            break;
                        case 'v':
                            c = '\v';
                            break;
                        case 'x':
                            var hexa = new StringBuilder(10);
                            pos++;
                            if (pos >= source.Length)
                                throw new ArgumentException("Missing escape sequence");
                            c = source[pos];
                            if (Char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))
                            {
                                hexa.Append(c);
                                pos++;
                                if (pos < source.Length)
                                {
                                    c = source[pos];
                                    if (Char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))
                                    {
                                        hexa.Append(c);
                                        pos++;
                                        if (pos < source.Length)
                                        {
                                            c = source[pos];
                                            if (Char.IsDigit(c) || (c >= 'a' && c <= 'f') ||
                                                (c >= 'A' && c <= 'F'))
                                            {
                                                hexa.Append(c);
                                                pos++;
                                                if (pos < source.Length)
                                                {
                                                    c = source[pos];
                                                    if (Char.IsDigit(c) || (c >= 'a' && c <= 'f') ||
                                                        (c >= 'A' && c <= 'F'))
                                                    {
                                                        hexa.Append(c);
                                                        pos++;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            c = (char)Int32.Parse(hexa.ToString(), NumberStyles.HexNumber);
                            pos--;
                            break;
                        case 'u':
                            pos++;
                            if (pos + 3 >= source.Length)
                                throw new ArgumentException("Unrecognized escape sequence");
                            try
                            {
                                uint charValue = UInt32.Parse(source.Substring(pos, 4), NumberStyles.HexNumber);
                                c = (char)charValue;
                                pos += 3;
                            }
                            catch (SystemException)
                            {
                                throw new ArgumentException("Unrecognized escape sequence");
                            }

                            break;
                        case 'U':
                            pos++;
                            if (pos + 7 >= source.Length)
                                throw new ArgumentException("Unrecognized escape sequence");
                            try
                            {
                                uint charValue = UInt32.Parse(source.Substring(pos, 8), NumberStyles.HexNumber);
                                if (charValue > 0xffff)
                                    throw new ArgumentException("Unrecognized escape sequence");
                                c = (char)charValue;
                                pos += 7;
                            }
                            catch (SystemException)
                            {
                                throw new ArgumentException("Unrecognized escape sequence");
                            }
                            break;
                        default:
                            throw new ArgumentException("Unrecognized escape sequence");
                    }
                }

                pos++;
                sb.Append(c);
            }

            return sb.ToString();
        }


        public static string StringFromVerbatimLiteral(string source)
        {
            return source.Replace("\"\"", "\"");
        }

        public static char CharFromCSharpLiteral(string source)
        {
            string result = StringFromCSharpLiteral(source);

            if (result.Length != 1)

                throw new ArgumentException("Invalid char literal");

            return result[0];
        }
    }
}
