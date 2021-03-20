using System;
using System.Collections.Generic;

namespace Honyac
{
    public class TokenList : List<Token>
    {
        public int CurrentIndex { get; private set; }
        private Token Current { get { return CurrentIndex < Count ? this[CurrentIndex] : null; } }

        private TokenList() { }

        private Token AddToken(TokenKind kind, int value, string str)
        {
            var token = new Token();
            token.Kind = kind;
            token.Value = value;
            token.Str = str;
            Add(token);
            return token;
        }

        private void TokenizeInternal(string str)
        {
            this.CurrentIndex = 0;
            int length;
            string typeName;
            for (var strIndex = 0;  strIndex < str.Length; )
            {
                if (char.IsWhiteSpace(str[strIndex]))
                {
                    strIndex++;
                    continue;
                }
                if ("+-*/(){};&".Contains(str[strIndex]))
                {
                    AddToken(TokenKind.Punct, 0, str[strIndex].ToString());
                    strIndex++;
                    continue;
                }
                if ("!><=".Contains(str[strIndex]))
                {
                    // 比較演算子は1文字の場合と2文字の場合とがある。以下の6種類。
                    // !=, >=, <=, ==, >, <
                    var c1 = str[strIndex];
                    var c2 = str[strIndex + 1];
                    if ((c1 == '!' && c2 == '=') ||
                        (c1 == '>' && c2 == '=') ||
                        (c1 == '<' && c2 == '=') ||
                        (c1 == '=' && c2 == '='))
                    {
                        AddToken(TokenKind.Punct, 0, string.Concat(c1, c2));
                        strIndex += 2;
                        continue;
                    }
                    else if (c1 == '>' || c1 == '<' || c1 == '=')
                    {
                        AddToken(TokenKind.Punct, 0, c1.ToString());
                        strIndex++;
                        continue;
                    }
                }
                if (IsKeyword(str, strIndex, "if", out length))
                {
                    AddToken(TokenKind.If, 0, "if");
                    strIndex += length;
                    continue;
                }
                if (IsKeyword(str, strIndex, "else", out length))
                {
                    AddToken(TokenKind.Else, 0, "else");
                    strIndex += length;
                    continue;
                }
                if (IsKeyword(str, strIndex, "while", out length))
                {
                    AddToken(TokenKind.While, 0, "while");
                    strIndex += length;
                    continue;
                }
                if (IsKeyword(str, strIndex, "for", out length))
                {
                    AddToken(TokenKind.For, 0, "for");
                    strIndex += length;
                    continue;
                }
                if (IsKeyword(str, strIndex, "return", out length))
                {
                    AddToken(TokenKind.Return, 0, "return");
                    strIndex += length;
                    continue;
                }
                if (IsType(str, strIndex, out typeName, out length))
                {
                    AddToken(TokenKind.Type, 0, typeName);
                    strIndex += length;
                    continue;
                }
                if (char.IsDigit(str[strIndex]))
                {
                    var value = 0;
                    for (; strIndex < str.Length && char.IsDigit(str[strIndex]); strIndex++)
                    {
                        value = value * 10 + int.Parse(str[strIndex].ToString());
                    }
                    AddToken(TokenKind.Num, value, value.ToString());
                    continue;
                }
                if (IsIdentHead(str[strIndex]))
                {
                    // 識別子の開始インデックス(startIndex)と終了インデックス(strIndex)を求める
                    // 但し終了インデックスは識別子に含まない
                    var startIndex = strIndex;
                    strIndex++;
                    for (; IsIdent(str[strIndex]) && strIndex < str.Length; strIndex++)
                        ;
                    AddToken(TokenKind.Ident, 0, str.Substring(startIndex, strIndex - startIndex));
                    continue;
                }

                throw new ArgumentException($"Invalid String:{str} Index:{strIndex} char:{str[strIndex]}");
            }
        }

        private bool IsKeyword(string str, int strIndex, string keyword, out int length)
        {
            var len = keyword.Length;
            if (strIndex + len < str.Length && keyword.Equals(str.Substring(strIndex, len)) && !IsIdent(str[strIndex + len]))
            {
                length = len;
                return true;
            }

            length = 0;
            return false;
        }

        private bool IsType(string str, int strIndex, out string typeName, out int length)
        {
            if (IsKeyword(str, strIndex, "int", out length))
            {
                typeName = "int";
                return true;
            }

            typeName = null;
            return false;
        }

        /// <summary>
        /// 識別子の先頭として有効な文字
        /// </summary>
        private bool IsIdentHead(char c)
        {
            return ('a' <= c && c <= 'z') || ('A' <= c && c <= 'Z') || c == '_';
        }

        /// <summary>
        /// 識別子の戦闘以外で有効な文字
        /// </summary>
        private bool IsIdent(char c)
        {
            return IsIdentHead(c) || ('0' <= c && c <= '9');
        }

        public static TokenList Tokenize(string str)
        {
            var tokenList = new TokenList();
            tokenList.TokenizeInternal(str);
            return tokenList;
        }

        /// <summary>
        /// 次のトークンが期待している記号の時は、トークンを一つ読み進めてtrueを返す
        /// それ以外の場合はfalseを返す
        /// </summary>
        public bool Consume(char op)
        {
            var token = Current;
            if (token != null && token.Kind == TokenKind.Punct && token.Str.Length == 1 && token.Str[0] == op)
            {
                CurrentIndex++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 次のトークンが期待している記号の時は、トークンを一つ読み進めてtrueを返す
        /// それ以外の場合はfalseを返す
        /// </summary>
        public bool Consume(string op)
        {
            var token = Current;
            if (token != null && token.Kind == TokenKind.Punct && string.Equals(token.Str, op))
            {
                CurrentIndex++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 次のトークンが識別子トークンの場合は、トークンを一つ進めてそのトークンを返す。
        /// それ以外の場合はnullを返す
        /// </summary>
        /// <returns></returns>
        public Token ConsumeIdent()
        {
            var token = Current;
            if (token != null && token.Kind == TokenKind.Ident)
            {
                CurrentIndex++;
                return token;
            }

            return null;
        }

        public bool Consume(TokenKind kind)
        {
            var token = Current;
            if (token != null && token.Kind == kind)
            {
                CurrentIndex++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 次のトークンが期待している記号の時は、トークンを一つ読みすすめる
        /// それ以外の場合はエラーを報告する
        /// </summary>
        /// <param name="op"></param>
        public void Expect(char op)
        {
            var token = Current;
            if (token != null && token.Kind == TokenKind.Punct && token.Str.Length == 1 && token.Str[0] == op)
            {
                CurrentIndex++;
                return;
            }

            throw new ArgumentException($"Token Not Match Expect:{op} CurrentIndex:{CurrentIndex}");
        }

        public Token Expect(TokenKind kind)
        {
            var token = Current;
            if (token != null && token.Kind == kind)
            {
                CurrentIndex++;
                return token;
            }

            throw new ArgumentException($"Token Not Match Expect:{kind} Current:{token?.Kind}");
        }

        /// <summary>
        /// 次のトークンが数値の場合、トークンを1つ読み進めてその数値を返す。
        /// それ以外の場合はエラーを報告する。
        /// 
        /// </summary>
        public int ExpectNumber()
        {
            var token = Current;
            if (token != null && token.Kind == TokenKind.Num)
            {
                CurrentIndex++;
                return token.Value;
            }

            throw new ArgumentException($"Token is Not Number CurrentIndex:{CurrentIndex}");
        }

        public Token ExpectIdent()
        {
            var token = ConsumeIdent();
            if (token != null)
            {
                return token;
            }

            throw new ArgumentException($"Token is Not Ident CurrentIndex:{CurrentIndex}");
        }

        /// <summary>
        /// トークンが終わりに到達しているか
        /// </summary>
        public bool IsEof()
        {
            return CurrentIndex == this.Count;
        }
    }

    public class Token
    {
        /// <summary>トークン種別</summary>
        public TokenKind Kind { get; set; }
        /// <summary>KindがNumの場合、数値</summary>
        public int Value { get; set; }
        /// <summary>トークン文字列</summary>
        public string Str { get; set; }
    }

    /// <summary>
    /// トークン種別
    /// </summary>
    public enum TokenKind
    {
        Punct,  // 記号
        Ident,  // 識別子
        Type,   // 型
        If,     // if
        Else,   // else
        While,  // while
        For,    // for
        Return, // return
        Num,    // 整数トークン
    }
}
