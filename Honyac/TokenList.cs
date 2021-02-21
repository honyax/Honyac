using System;
using System.Collections.Generic;

namespace Honyac
{
    public class TokenList : List<Token>
    {
        private int CurrentIndex { get; set; }
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
            for (var strIndex = 0;  strIndex < str.Length; )
            {
                if (char.IsWhiteSpace(str[strIndex]))
                {
                    strIndex++;
                    continue;
                }
                if (str[strIndex] == '+' || str[strIndex] == '-')
                {
                    AddToken(TokenKind.Reserved, 0, str[strIndex].ToString());
                    strIndex++;
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

                throw new ArgumentException($"Invalid String:{str} Index:{strIndex} char:{str[strIndex]}");
            }
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
            if (token != null && token.Kind == TokenKind.Reserved && token.Str.Length == 1 && token.Str[0] == op)
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
            if (token != null && token.Kind == TokenKind.Reserved && token.Str.Length == 1 && token.Str[0] == op)
            {
                CurrentIndex++;
                return;
            }

            throw new ArgumentException($"Token Not Match Expect:{op} CurrentIndex:{CurrentIndex}");
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
        Reserved,       // 記号
        Num,            // 整数トークン
    }
}
