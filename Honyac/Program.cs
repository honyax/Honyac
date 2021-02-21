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
            sb.AppendLine($"  mov rax, {tokenList.ExpectNumber()}");

            while (!tokenList.IsEof())
            {
                string cmd;
                if (tokenList.Consume('+'))
                {
                    cmd = "add";
                }
                else
                {
                    tokenList.Expect('-');
                    cmd = "sub";
                }

                sb.AppendLine($"  {cmd} rax, {tokenList.ExpectNumber()}");
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
