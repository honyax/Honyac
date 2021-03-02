using System;
using System.Collections.Generic;
using System.Text;

namespace Honyac
{
    public class Generator
    {
        private static void GenerateLval(StringBuilder sb, Node node)
        {
            if (node.Kind != NodeKind.Lvar)
            {
                throw new ArgumentException($"Node is not Left Variable:{node}");
            }

            sb.AppendLine($"  mov rax, rbp");
            sb.AppendLine($"  sub rax, {node.Offset}");
            sb.AppendLine($"  push rax");
        }

        public static void Generate(StringBuilder sb, Node node)
        {
            switch (node.Kind)
            {
                case NodeKind.Num:
                    sb.AppendLine($"  push {node.Value}");
                    return;

                case NodeKind.Lvar:
                    GenerateLval(sb, node);
                    sb.AppendLine($"  pop rax");
                    sb.AppendLine($"  mov rax, [rax]");
                    sb.AppendLine($"  push rax");
                    return;

                case NodeKind.Assign:
                    GenerateLval(sb, node.Nodes.Item1);
                    Generate(sb, node.Nodes.Item2);

                    sb.AppendLine($"  pop rdi");
                    sb.AppendLine($"  pop rax");
                    sb.AppendLine($"  mov [rax], rdi");
                    sb.AppendLine($"  push rdi");
                    return;

                case NodeKind.Return:
                    Generate(sb, node.Nodes.Item1);

                    sb.AppendLine($"  pop rax");
                    sb.AppendLine($"  mov rsp, rbp");
                    sb.AppendLine($"  pop rbp");
                    sb.AppendLine($"  ret");
                    return;

                default:
                    break;
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
