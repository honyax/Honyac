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
            // 50+10-25
            var sb = new StringBuilder();
            sb.AppendLine($".intel_syntax noprefix");
            sb.AppendLine($".globl main");
            sb.AppendLine($"main:");

            var index = 0;
            while (index < SourceCode.Length)
            {
                string calc;
                if (index == 0)
                {
                    calc = "mov";
                }
                else
                {
                    switch (SourceCode[index])
                    {
                        case '+':
                            calc = "add";
                            break;
                        case '-':
                            calc = "sub";
                            break;
                        default:
                            throw new ArgumentException($"Invalid SourceCode:{SourceCode}");
                    }
                    index++;
                }

                var value = 0;
                for (; index < SourceCode.Length; index++)
                {
                    if (char.IsDigit(SourceCode[index]))
                    {
                        value = value * 10 + int.Parse(SourceCode[index].ToString());
                    }
                    else
                    {
                        break;
                    }
                }
                sb.AppendLine($"  {calc} rax, {value}");
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
