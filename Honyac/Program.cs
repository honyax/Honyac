using System;
using System.Text;

namespace Honyac
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("引数の個数が正しくありません");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($".intel_syntax noprefix");
            sb.AppendLine($".globl main");
            sb.AppendLine($"main:");
            sb.AppendLine($"  mov rax, {int.Parse(args[0])}");
            sb.AppendLine($"  ret");
            Console.Write(sb.ToString());
        }
    }
}
