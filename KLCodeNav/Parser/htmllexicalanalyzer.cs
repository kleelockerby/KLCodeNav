using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace KLCodeNav
{
    internal class HtmlLexicalAnalyzer
    {
        internal HtmlLexicalAnalyzer(string inputTextString)
        {
            _inputStringReader = new StringReader(inputTextString);
            _nextCharacterCode = 0;
            _nextCharacter = ' ';
            _lookAheadCharacterCode = _inputStringReader.Read();
            _lookAheadCharacter = (char) _lookAheadCharacterCode;
            _previousCharacter = ' ';
            _ignoreNextWhitespace = true;
            _nextToken = new StringBuilder(100);
            _nextTokenType = HtmlTokenType.Text;
            this.GetNextCharacter();
        }

        internal void GetNextContentToken()
        {
            Debug.Assert(_nextTokenType != HtmlTokenType.EOF);
            _nextToken.Length = 0;
            if (this.IsAtEndOfStream)
            {
                _nextTokenType = HtmlTokenType.EOF;
                return;
            }

            if (this.IsAtTagStart)
            {
                this.GetNextCharacter();

                if (this.NextCharacter == '/')
                {
                    _nextToken.Append("</");
                    _nextTokenType = HtmlTokenType.ClosingTagStart;

                    this.GetNextCharacter();
                    _ignoreNextWhitespace = false;       
                }
                else
                {
                    _nextTokenType = HtmlTokenType.OpeningTagStart;
                    _nextToken.Append("<");
                    _ignoreNextWhitespace = true;       
                }
            }
            else if (this.IsAtDirectiveStart)
            {
                this.GetNextCharacter();
                if (_lookAheadCharacter == '[')
                {
                    this.ReadDynamicContent();
                }
                else if (_lookAheadCharacter == '-')
                {
                    this.ReadComment();
                }
                else
                {
                    this.ReadUnknownDirective();
                }
            }
            else
            {
                _nextTokenType = HtmlTokenType.Text;
                while (!this.IsAtTagStart && !this.IsAtEndOfStream && !this.IsAtDirectiveStart)
                {
                    if (this.NextCharacter == '<' && !this.IsNextCharacterEntity && _lookAheadCharacter == '?')
                    {
                        this.SkipProcessingDirective();
                    }
                    else
                    {
                        if (this.NextCharacter <= ' ')
                        {
                            if (_ignoreNextWhitespace)
                            {
                            }
                            else
                            {
                                _nextToken.Append(' ');
                            }

                            _ignoreNextWhitespace = true;       
                        }
                        else
                        {
                            _nextToken.Append(this.NextCharacter);
                            _ignoreNextWhitespace = false;
                        }

                        this.GetNextCharacter();
                    }
                }
            }
        }

        internal void GetNextTagToken()
        {
            _nextToken.Length = 0;
            if (this.IsAtEndOfStream)
            {
                _nextTokenType = HtmlTokenType.EOF;
                return;
            }

            this.SkipWhiteSpace();

            if (this.NextCharacter == '>' && !this.IsNextCharacterEntity)
            {
                _nextTokenType = HtmlTokenType.TagEnd;
                _nextToken.Append('>');
                this.GetNextCharacter();
            }
            else if (this.NextCharacter == '/' && _lookAheadCharacter == '>')
            {
                _nextTokenType = HtmlTokenType.EmptyTagEnd;
                _nextToken.Append("/>");
                this.GetNextCharacter();
                this.GetNextCharacter();
                _ignoreNextWhitespace = false;       
            }
            else if (IsGoodForNameStart(this.NextCharacter))
            {
                _nextTokenType = HtmlTokenType.Name;

                while (IsGoodForName(this.NextCharacter) && !this.IsAtEndOfStream)
                {
                    _nextToken.Append(this.NextCharacter);
                    this.GetNextCharacter();
                }
            }
            else
            {
                _nextTokenType = HtmlTokenType.Atom;
                _nextToken.Append(this.NextCharacter);
                this.GetNextCharacter();
            }
        }

        internal void GetNextEqualSignToken()
        {
            Debug.Assert(_nextTokenType != HtmlTokenType.EOF);
            _nextToken.Length = 0;

            _nextToken.Append('=');
            _nextTokenType = HtmlTokenType.EqualSign;

            this.SkipWhiteSpace();

            if (this.NextCharacter == '=')
            {
                this.GetNextCharacter();
            }
        }

        internal void GetNextAtomToken()
        {
            Debug.Assert(_nextTokenType != HtmlTokenType.EOF);
            _nextToken.Length = 0;

            this.SkipWhiteSpace();

            _nextTokenType = HtmlTokenType.Atom;

            if ((this.NextCharacter == '\'' || this.NextCharacter == '"') && !this.IsNextCharacterEntity)
            {
                char startingQuote = this.NextCharacter;
                this.GetNextCharacter();

                while (!(this.NextCharacter == startingQuote && !this.IsNextCharacterEntity) && !this.IsAtEndOfStream)
                {
                    _nextToken.Append(this.NextCharacter);
                    this.GetNextCharacter();
                }

                if (this.NextCharacter == startingQuote)
                {
                    this.GetNextCharacter();
                }

            }
            else
            {
                while (!this.IsAtEndOfStream && !Char.IsWhiteSpace(this.NextCharacter) && this.NextCharacter != '>')
                {
                    _nextToken.Append(this.NextCharacter);
                    this.GetNextCharacter();
                }
            }
        }

        internal HtmlTokenType NextTokenType
        {
            get { return _nextTokenType; }
        }

        internal string NextToken
        {
            get { return _nextToken.ToString(); }
        }

        private void GetNextCharacter()
        {
            if (_nextCharacterCode == -1)
            {
                throw new InvalidOperationException("GetNextCharacter method called at the end of a stream");
            }

            _previousCharacter = _nextCharacter;

            _nextCharacter = _lookAheadCharacter;
            _nextCharacterCode = _lookAheadCharacterCode;
            _isNextCharacterEntity = false;

            this.ReadLookAheadCharacter();

            if (_nextCharacter == '&')
            {
                if (_lookAheadCharacter == '#')
                {
                    int entityCode;
                    entityCode = 0;
                    this.ReadLookAheadCharacter();

                    for (int i = 0; i < 7 && Char.IsDigit(_lookAheadCharacter); i++)
                    {
                        entityCode = 10 * entityCode + (_lookAheadCharacterCode - (int) '0');
                        this.ReadLookAheadCharacter();
                    }

                    if (_lookAheadCharacter == ';')
                    {
                        this.ReadLookAheadCharacter();
                        _nextCharacterCode = entityCode;

                        _nextCharacter = (char) _nextCharacterCode;

                        _isNextCharacterEntity = true;
                    }
                    else
                    {
                        _nextCharacter = _lookAheadCharacter;
                        _nextCharacterCode = _lookAheadCharacterCode;
                        this.ReadLookAheadCharacter();
                        _isNextCharacterEntity = false;
                    }
                }
                else if (Char.IsLetter(_lookAheadCharacter))
                {
                    string entity = "";

                    for (int i = 0; i < 10 && (Char.IsLetter(_lookAheadCharacter) || Char.IsDigit(_lookAheadCharacter)); i++)
                    {
                        entity += _lookAheadCharacter;
                        this.ReadLookAheadCharacter();
                    }

                    if (_lookAheadCharacter == ';')
                    {
                        this.ReadLookAheadCharacter();

                        if (HtmlSchema.IsEntity(entity))
                        {
                            _nextCharacter = HtmlSchema.EntityCharacterValue(entity);
                            _nextCharacterCode = (int) _nextCharacter;
                            _isNextCharacterEntity = true;
                        }
                        else
                        {
                            _nextCharacter = _lookAheadCharacter;
                            _nextCharacterCode = _lookAheadCharacterCode;
                            this.ReadLookAheadCharacter();

                            _isNextCharacterEntity = false;
                        }
                    }
                    else
                    {
                        _nextCharacter = _lookAheadCharacter;
                        this.ReadLookAheadCharacter();
                        _isNextCharacterEntity = false;
                    }
                }
            }
        }

        private void ReadLookAheadCharacter()
        {
            if (_lookAheadCharacterCode != -1)
            {
                _lookAheadCharacterCode = _inputStringReader.Read();
                _lookAheadCharacter = (char) _lookAheadCharacterCode;
            }
        }

        private void SkipWhiteSpace()
        {
            while (true)
            {
                if (_nextCharacter == '<' && (_lookAheadCharacter == '?' || _lookAheadCharacter == '!'))
                {
                    this.GetNextCharacter();

                    if (_lookAheadCharacter == '[')
                    {
                        while (!this.IsAtEndOfStream && !(_previousCharacter == ']' && _nextCharacter == ']' && _lookAheadCharacter == '>'))
                        {
                            this.GetNextCharacter();
                        }

                        if (_nextCharacter == '>')
                        {
                            this.GetNextCharacter();
                        }
                    }
                    else
                    {
                        while (!this.IsAtEndOfStream && _nextCharacter != '>')
                        {
                            this.GetNextCharacter();
                        }

                        if (_nextCharacter == '>')
                        {
                            this.GetNextCharacter();
                        }
                    }
                }


                if (!Char.IsWhiteSpace(this.NextCharacter))
                {
                    break;
                }

                this.GetNextCharacter();
            }
        }

        private bool IsGoodForNameStart(char character)
        {
            return character == '_' || Char.IsLetter(character);
        }

        private bool IsGoodForName(char character)
        {
            return
                this.IsGoodForNameStart(character) ||
                character == '.' ||
                character == '-' ||
                character == ':' ||
                Char.IsDigit(character) ||
                IsCombiningCharacter(character) ||
                IsExtender(character);
        }

        private bool IsCombiningCharacter(char character)
        {
            return false;
        }

        private bool IsExtender(char character)
        {
            return false;
        }

        private void ReadDynamicContent()
        {
            Debug.Assert(_previousCharacter == '<' && _nextCharacter == '!' && _lookAheadCharacter == '[');

            _nextTokenType = HtmlTokenType.Text;
            _nextToken.Length = 0;

            this.GetNextCharacter();
            this.GetNextCharacter();

            while (!(_nextCharacter == ']' && _lookAheadCharacter == '>') && !this.IsAtEndOfStream)
            {
                this.GetNextCharacter();
            }

            if (!this.IsAtEndOfStream)
            {
                this.GetNextCharacter();

                this.GetNextCharacter();
            }
        }

        private void ReadComment()
        {
            Debug.Assert(_previousCharacter == '<' && _nextCharacter == '!' && _lookAheadCharacter == '-');

            _nextTokenType = HtmlTokenType.Comment;
            _nextToken.Length = 0;

            this.GetNextCharacter();    
            this.GetNextCharacter();    
            this.GetNextCharacter();       

            while (true)
            {
                while (!this.IsAtEndOfStream && !(_nextCharacter == '-' && _lookAheadCharacter == '-' || _nextCharacter == '!' && _lookAheadCharacter == '>'))
                {
                    _nextToken.Append(this.NextCharacter);
                    this.GetNextCharacter();
                }

                this.GetNextCharacter();
                if (_previousCharacter == '-' && _nextCharacter == '-' && _lookAheadCharacter == '>')
                {
                    this.GetNextCharacter();   
                    break;
                }
                else if (_previousCharacter == '!' && _nextCharacter == '>')
                {
                    break;
                }
                else
                {
                    _nextToken.Append(_previousCharacter);
                    continue;
                }
            }

            if (_nextCharacter == '>')
            {
                this.GetNextCharacter();
            }
        }

        private void ReadUnknownDirective()
        {
            Debug.Assert(_previousCharacter == '<' && _nextCharacter == '!' && !(_lookAheadCharacter == '-' || _lookAheadCharacter == '['));

            _nextTokenType = HtmlTokenType.Text;
            _nextToken.Length = 0;

            this.GetNextCharacter();

            while (!(_nextCharacter == '>' && !IsNextCharacterEntity) && !this.IsAtEndOfStream)
            {
                this.GetNextCharacter();
            }

            if (!this.IsAtEndOfStream)
            {
                this.GetNextCharacter();
            }
        }

        private void SkipProcessingDirective()
        {
            Debug.Assert(_nextCharacter == '<' && _lookAheadCharacter == '?');

            this.GetNextCharacter();
            this.GetNextCharacter();

            while (!((_nextCharacter == '?' || _nextCharacter == '/') && _lookAheadCharacter == '>') && !this.IsAtEndOfStream)
            {
                this.GetNextCharacter();
            }

            if (!this.IsAtEndOfStream)
            {
                this.GetNextCharacter();

                this.GetNextCharacter();
            }
        }

        private char NextCharacter
        {
            get { return _nextCharacter; }
        }

        private bool IsAtEndOfStream
        {
            get { return _nextCharacterCode == -1; }
        }

        private bool IsAtTagStart
        {
            get { return _nextCharacter == '<' && (_lookAheadCharacter == '/' || IsGoodForNameStart(_lookAheadCharacter)) && !_isNextCharacterEntity; }
        }

        private bool IsAtTagEnd
        {
            get { return (_nextCharacter == '>' || (_nextCharacter == '/' && _lookAheadCharacter == '>')) && !_isNextCharacterEntity; }
        }

        private bool IsAtDirectiveStart
        {
            get { return (_nextCharacter == '<' && _lookAheadCharacter == '!' && !this.IsNextCharacterEntity); }
        }

        private bool IsNextCharacterEntity
        {
            get { return _isNextCharacterEntity; }
        }

        private StringReader _inputStringReader;

        private int _nextCharacterCode;
        private char _nextCharacter;
        private int _lookAheadCharacterCode;
        private char _lookAheadCharacter;
        private char _previousCharacter;
        private bool _ignoreNextWhitespace;
        private bool _isNextCharacterEntity;

        StringBuilder _nextToken;
        HtmlTokenType _nextTokenType;

    }
}