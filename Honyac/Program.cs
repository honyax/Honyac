using System;
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

            var tokenList = TokenList.Tokenize(SourceCode);
            var value = 0;
            if (tokenList.TryConsumeNumber(out value))
            {
                sb.AppendLine($"  mov rax, {value}");
            }
            else
            {
                throw new ArgumentException($"Invalid Code:{SourceCode}");
            }

            while (!tokenList.IsEof())
            {
                var cmd = string.Empty;
                if (tokenList.Consume('+'))
                {
                    cmd = "add";
                }
                else if (tokenList.Consume('-'))
                {
                    cmd = "sub";
                }
                else
                {
                    throw new ArgumentException($"Invalid Code:{SourceCode}");
                }

                if (!tokenList.TryConsumeNumber(out value))
                {
                    throw new ArgumentException($"Invalid Code:{SourceCode}");
                }

                sb.AppendLine($"  {cmd} rax, {value}");
            }

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
