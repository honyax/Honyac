using System;
using System.Collections.Generic;
using System.Text;

namespace Honyac
{
    public class Generator
    {
        public static void Generate(StringBuilder sb, Node node)
        {
            if (node.Kind == NodeKind.Num)
            {
                sb.AppendLine($"  push {node.Value}");
                return;
            }

            Generate(sb, node.Nodes.Item1);
            Generate(sb, node.Nodes.Item2);

            sb.AppendLine("  pop rdi");
            sb.AppendLine("  pop rax");

            switch (node.Kind)
            {
                case NodeKind.Add:
                    sb.AppendLine("  add rax, rdi");
                    break;
                case NodeKind.Sub:
                    sb.AppendLine("  sub rax, rdi");
                    break;
                case NodeKind.Mul:
                    sb.AppendLine("  imul rax, rdi");
                    break;
                case NodeKind.Div:
                    sb.AppendLine("  cqo");
                    sb.AppendLine("  idiv rdi");
                    break;
                case NodeKind.Eq:
                    sb.AppendLine("  cmp rax, rdi");
                    sb.AppendLine("  sete al");
                    sb.AppendLine("  movzb rax, al");
                    break;
                case NodeKind.Ne:
                    sb.AppendLine("  cmp rax, rdi");
                    sb.AppendLine("  setne al");
                    sb.AppendLine("  movzb rax, al");
                    break;
                case NodeKind.Lt:
                    sb.AppendLine("  cmp rax, rdi");
                    sb.AppendLine("  setl al");
                    sb.AppendLine("  movzb rax, al");
                    break;
                case NodeKind.Le:
                    sb.AppendLine("  cmp rax, rdi");
                    sb.AppendLine("  setle al");
                    sb.AppendLine("  movzb rax, al");
                    break;
            }

            sb.AppendLine("  push rax");
        }
    }
}
