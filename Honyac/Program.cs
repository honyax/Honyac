using System;
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
            sb.AppendLine($"  mov rax, 0");
            sb.AppendLine($"  call main");
            sb.AppendLine($"  ret");

            var tokenList = TokenList.Tokenize(SourceCode);
            var nodeMap = NodeMap.Create(tokenList);
            var generator = new Generator();

            // Nodesは関数ごとに存在する
            foreach (var node in nodeMap.Nodes)
            {
                if (node.Kind != NodeKind.Function)
                {
                    throw new Exception($"Invalid Node:{node}");
                }

                generator.Generate(sb, node);
            }

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
