﻿using System;
using System.Linq;
using System.Text;

namespace Honyac
{
    public class Program
    {
        private string SourceCode { get; set; }

        public Program(string sourceCode)
        {
            this.SourceCode = sourceCode;
        }

        public string Execute()
        {
            var sb = new StringBuilder();
            sb.AppendLine($".intel_syntax noprefix");
            sb.AppendLine($".globl main");
            sb.AppendLine($"main:");

            // プロローグ
            // 変数26個分の領域を確保する
            sb.AppendLine($"  push rbp");
            sb.AppendLine($"  mov rbp, rsp");

            var tokenList = TokenList.Tokenize(SourceCode);
            var nodeMap = NodeMap.Create(tokenList);

            // 変数がある場合は変数の領域を確保
            if (nodeMap.LVars.Any())
            {
                var maxOffset = nodeMap.LVars.Max(lv => lv.Offset);
                sb.AppendLine($"  sub rsp, {maxOffset}");
            }

            foreach (var node in nodeMap.Nodes)
            {
                Generator.Generate(sb, node);

                // 式の評価結果としてスタックに一つの値が残っているはずなので、
                // スタックが溢れないようにポップしておく
                sb.AppendLine($"  pop rax");
            }

            // エピローグ
            // 最後の式の結果がRAXに残っているのでそれが返り値になる
            sb.AppendLine($"  mov rsp, rbp");
            sb.AppendLine($"  pop rbp");
            sb.AppendLine($"  ret");

            return sb.ToString();
        }

        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("引数の個数が正しくありません");
                return;
            }

            var p = new Program(args[0]);
            Console.Write(p.Execute());
        }
    }
}
