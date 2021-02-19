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
        /// 次のトークンが数値の場合、トークンを一つ読み進めて数字を取得し、trueを返す
        /// それ以外の場合はfalseを返す
        /// </summary>
        public bool TryConsumeNumber(out int value)
        {
            var token = Current;
            if (token != null && token.Kind == TokenKind.Num)
            {
                value = token.Value;
                CurrentIndex++;
                return true;
            }

            value = 0;
            return false;
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
