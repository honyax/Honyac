using System;
using System.Linq;
using System.Text;

namespace Honyac
{
    public class Generator
    {
        private int _counter = 1;
        private int Count() { return _counter++; }

        private void GenerateLval(StringBuilder sb, Node node)
        {
            switch (node.Kind)
            {
                case NodeKind.Lvar:
                    sb.AppendLine($"  mov rax, rbp");
                    sb.AppendLine($"  sub rax, {node.Offset}");
                    sb.AppendLine($"  push rax");
                    break;

                case NodeKind.DeRef:
                    GenerateLval(sb, node.Nodes.Item1);
                    sb.AppendLine($"  pop rax");
                    sb.AppendLine($"  mov rax, [rax]");
                    sb.AppendLine($"  push rax");
                    break;

                default:
                    throw new ArgumentException($"Invalid NodeKind:{node}");
            }
        }

        public void Generate(StringBuilder sb, Node node)
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

                case NodeKind.If:
                    int ifCnt = Count();
                    Generate(sb, node.Condition);
                    sb.AppendLine($"  pop rax");
                    sb.AppendLine($"  cmp rax, 0");
                    sb.AppendLine($"  je .L.else.{ifCnt}");
                    Generate(sb, node.Nodes.Item1);
                    sb.AppendLine($"  jmp .L.end.{ifCnt}");
                    sb.AppendLine($".L.else.{ifCnt}:");
                    if (node.Nodes.Item2 != null)
                    {
                        Generate(sb, node.Nodes.Item2);
                    }
                    sb.AppendLine($".L.end.{ifCnt}:");
                    return;

                case NodeKind.While:
                    int whileCnt = Count();
                    sb.AppendLine($".L.while.{whileCnt}:");
                    Generate(sb, node.Condition);
                    sb.AppendLine($"  pop rax");
                    sb.AppendLine($"  cmp rax, 0");
                    sb.AppendLine($"  je .L.end.{whileCnt}");
                    Generate(sb, node.Nodes.Item1);
                    sb.AppendLine($"  jmp .L.while.{whileCnt}");
                    sb.AppendLine($".L.end.{whileCnt}:");
                    return;

                case NodeKind.For:
                    int forCnt = Count();
                    if (node.Initialize != null)
                    {
                        Generate(sb, node.Initialize);
                    }
                    sb.AppendLine($".L.for.{forCnt}:");
                    Generate(sb, node.Condition);
                    sb.AppendLine($"  pop rax");
                    sb.AppendLine($"  cmp rax, 0");
                    sb.AppendLine($"  je .L.end.{forCnt}");
                    Generate(sb, node.Nodes.Item1);
                    if (node.Loop != null)
                    {
                        Generate(sb, node.Loop);
                    }
                    sb.AppendLine($"  jmp .L.for.{forCnt}");
                    sb.AppendLine($".L.end.{forCnt}:");
                    return;

                case NodeKind.Return:
                    Generate(sb, node.Nodes.Item1);

                    sb.AppendLine($"  pop rax");
                    sb.AppendLine($"  mov rsp, rbp");
                    sb.AppendLine($"  pop rbp");
                    sb.AppendLine($"  ret");
                    return;

                case NodeKind.Block:
                    // block内の全てのstatementを生成
                    foreach (var body in node.Bodies)
                    {
                        Generate(sb, body);

                        // 式の評価結果としてスタックに一つの値が残っているはずなので、
                        // スタックが溢れないようにポップしておく
                        sb.AppendLine($"  pop rax");
                    }
                    return;

                case NodeKind.FuncCall:
                    sb.AppendLine($"  mov rax, 0");
                    sb.AppendLine($"  call {node.FuncName}");
                    sb.AppendLine($"  push rax");
                    return;

                case NodeKind.Function:
                    // 関数の宣言
                    sb.AppendLine($".globl {node.FuncName}");
                    sb.AppendLine($"{node.FuncName}:");

                    // 関数プロローグ
                    sb.AppendLine($"  push rbp");
                    sb.AppendLine($"  mov rbp, rsp");

                    // 変数がある場合は変数の領域を確保
                    if (node.LVars != null && node.LVars.Any())
                    {
                        var maxOffset = node.LVars.Max(LVar => LVar.Offset);
                        sb.AppendLine($"  sub rsp, {maxOffset}");
                    }

                    Generate(sb, node.Nodes.Item1);

                    // エピローグ
                    // 最後の式の結果がRAXに残っているのでそれが返り値になる
                    sb.AppendLine($"  mov rsp, rbp");
                    sb.AppendLine($"  pop rbp");
                    sb.AppendLine($"  ret");
                    return;

                case NodeKind.Addr:
                    GenerateLval(sb, node.Nodes.Item1);
                    return;

                case NodeKind.DeRef:
                    Generate(sb, node.Nodes.Item1);
                    sb.AppendLine($"  pop rax");
                    sb.AppendLine($"  mov rax, [rax]");
                    sb.AppendLine($"  push rax");
                    return;

                case NodeKind.Type:
                    // 型宣言ノードはひとまず無視する。
                    // 呼び出すごとにpopされるので、ダミーのpushを入れる
                    sb.AppendLine($"  push 0");
                    return;

                default:
                    break;
            }

            Generate(sb, node.Nodes.Item1);
            Generate(sb, node.Nodes.Item2);

            if (node.Nodes.Item1.Kind == NodeKind.Lvar)
            {
                // ポインタに対する加減算の場合は、ポインタの指し先のサイズ分だけ加減算する
                // Add, Sub以外の場合はおかしなことになるがひとまず気にしない
                var lvar = node.Nodes.Item1.LVar;
                if (lvar != null && lvar.PointerCount > 0)
                {
                    int size;
                    if (lvar.PointerCount == 1)
                    {
                        var type = TypeUtils.TypeDic[lvar.Kind];
                        size = type.Size;
                    }
                    else
                    {
                        // 指し先がポインタの場合は8ずつ加減算する
                        size = 8;
                    }

                    sb.AppendLine($"  pop rax");
                    sb.AppendLine($"  mov rdi, {size}");
                    sb.AppendLine($"  mul rdi");
                    sb.AppendLine($"  push rax");
                }
            }

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
