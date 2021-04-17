using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using Honyac;

namespace HonyacTests
{
    [TestClass]
    public class UnitTest1
    {
        /// <summary>
        /// NodeのValueとOffsetに不正な値が入っていないかをチェック
        /// </summary>
        bool ValidateNode(Node node)
        {
            if (node == null)
                return true;

            if (node.Nodes != null)
            {
                if (!ValidateNode(node.Nodes.Item1))
                    return false;

                if (!ValidateNode(node.Nodes.Item2))
                    return false;
            }

            if (node.Kind != NodeKind.Num && node.Value != 0)
                return false;

            if (node.Kind != NodeKind.Lvar && (node.Offset != 0 || node.LVar != null))
                return false;

            if (node.Kind != NodeKind.If && node.Kind != NodeKind.While && node.Kind != NodeKind.For && node.Condition != null)
                return false;

            if (node.Kind != NodeKind.For && (node.Initialize != null || node.Loop != null))
                return false;

            if (node.Kind != NodeKind.Block && node.Bodies != null)
                return false;

            if (node.Kind != NodeKind.FuncCall && node.Kind != NodeKind.Function && node.FuncName != null)
                return false;

            if (node.Kind != NodeKind.Function && node.LVars != null)
                return false;

            return true;
        }

        bool ValidateNodeValuesAndOffsets(NodeMap nodeMap)
        {
            var generator = new Generator();
            var sb = new StringBuilder();

            foreach (var node in nodeMap.Nodes)
            {
                if (!ValidateNode(node))
                    return false;

                if (node.Kind != NodeKind.Function)
                    return false;

                generator.Generate(sb, node);
            }

            return true;
        }

        /// <summary>
        /// 渡されたソースコードをコンパイル、実行してExitCodeを取得する
        /// </summary>
        private int CopmlileAndExecOnWsl(string src)
        {
            var dir = @"D:\var\Honyac\Honyac\bin\netcoreapp3.1\";
            var ldir = @"/mnt/d/var/Honyac/Honyac/bin/netcoreapp3.1/";

            // Honyac.exe "main() { return 10; }" > tmp.S; cc -o tmp tmp.S; ./tmp; echo $?
            var prog = new Program(src);
            var asmCode = prog.Execute();
            File.WriteAllText(dir + "tmp.S", asmCode);

            var cc = new ProcessStartInfo();
            cc.FileName = "ubuntu1804";
            cc.Arguments = @$"run ""cc -o {ldir}tmp {ldir}tmp.S {ldir}tmp.o""";
            var ccProc = Process.Start(cc);
            ccProc.WaitForExit();
            Assert.IsTrue(ccProc.HasExited);

            var tmp = new ProcessStartInfo();
            tmp.FileName = "ubuntu1804";
            tmp.Arguments = $@"run ""{ldir}tmp""";
            var tmpProc = Process.Start(tmp);
            tmpProc.WaitForExit();
            Assert.IsTrue(tmpProc.HasExited);

            return tmpProc.ExitCode;
        }

        [TestMethod]
        public void Test01_単一の数字をコンパイル()
        {
            var src = "int main() { 100; }";
            var outTest = new StringBuilder()
                .AppendLine(".intel_syntax noprefix")
                .AppendLine("  mov rax, 0")
                .AppendLine("  call main")
                .AppendLine("  ret")
                .AppendLine(".globl main")
                .AppendLine("main:")
                .AppendLine("  push rbp")
                .AppendLine("  mov rbp, rsp")
                
                .AppendLine("  push 100")
                .AppendLine("  pop rax")
                
                .AppendLine("  mov rsp, rbp")
                .AppendLine("  pop rbp")
                .AppendLine("  ret");

            using (var output = new StringWriter())
            {
                Console.SetOut(output);

                Program.Main(new string[] { src });

                Assert.AreEqual(outTest.ToString(), output.ToString());
            }

            Assert.AreEqual(100, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test02_四則演算をコンパイル()
        {
            var src = "int main() { 80 - 95 + 50 - 20; }";
            var outTest = new StringBuilder()
                .AppendLine(".intel_syntax noprefix")
                .AppendLine("  mov rax, 0")
                .AppendLine("  call main")
                .AppendLine("  ret")
                .AppendLine(".globl main")
                .AppendLine("main:")
                .AppendLine("  push rbp")
                .AppendLine("  mov rbp, rsp")

                .AppendLine("  push 80")
                .AppendLine("  push 95")
                .AppendLine("  pop rdi")
                .AppendLine("  pop rax")
                .AppendLine("  sub rax, rdi")
                .AppendLine("  push rax")
                .AppendLine("  push 50")
                .AppendLine("  pop rdi")
                .AppendLine("  pop rax")
                .AppendLine("  add rax, rdi")
                .AppendLine("  push rax")
                .AppendLine("  push 20")
                .AppendLine("  pop rdi")
                .AppendLine("  pop rax")
                .AppendLine("  sub rax, rdi")
                .AppendLine("  push rax")
                .AppendLine("  pop rax")

                .AppendLine("  mov rsp, rbp")
                .AppendLine("  pop rbp")
                .AppendLine("  ret");

            using (var output = new StringWriter())
            {
                Console.SetOut(output);

                Program.Main(new string[] { src });

                Assert.AreEqual(outTest.ToString(), output.ToString());
            }

            Assert.AreEqual(15, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test03_トークン解析()
        {
            var src = "int main() { 10 + 50 - 60; }";
            var tokenList = TokenList.Tokenize(src);
            Assert.AreEqual(12, tokenList.Count);
            Assert.IsNotNull(tokenList.Expect(TokenKind.Type));
            Assert.AreEqual("main", tokenList.ExpectIdent().Str);
            Assert.IsTrue(tokenList.Consume('('));
            Assert.IsTrue(tokenList.Consume(')'));
            Assert.IsTrue(tokenList.Consume('{'));
            Assert.AreEqual(10, tokenList.ExpectNumber());
            Assert.IsTrue(tokenList.Consume('+'));
            Assert.AreEqual(50, tokenList.ExpectNumber());
            Assert.IsTrue(tokenList.Consume('-'));
            Assert.AreEqual(60, tokenList.ExpectNumber());
            Assert.IsTrue(tokenList.Consume(';'));
            Assert.IsTrue(tokenList.Consume('}'));
            Assert.IsTrue(tokenList.IsEof());

            Assert.AreEqual(0, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test04_NodeMap解析()
        {
            var src = "int main() { 100 - ( 7 + 2 ) * 10 - 20 / 5; }";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));
            var head = nodeMap.Head.Nodes.Item1.Bodies[0];
            Assert.AreEqual(NodeKind.Sub, head.Kind);
            Assert.AreEqual(NodeKind.Sub, head.Nodes.Item1.Kind);
            Assert.AreEqual(NodeKind.Div, head.Nodes.Item2.Kind);
            Assert.AreEqual(100, head.Nodes.Item1.Nodes.Item1.Value);
            Assert.AreEqual(NodeKind.Mul, head.Nodes.Item1.Nodes.Item2.Kind);
            Assert.AreEqual(NodeKind.Add, head.Nodes.Item1.Nodes.Item2.Nodes.Item1.Kind);
            Assert.AreEqual(10, head.Nodes.Item1.Nodes.Item2.Nodes.Item2.Value);
            Assert.AreEqual(7, head.Nodes.Item1.Nodes.Item2.Nodes.Item1.Nodes.Item1.Value);
            Assert.AreEqual(2, head.Nodes.Item1.Nodes.Item2.Nodes.Item1.Nodes.Item2.Value);
            Assert.AreEqual(20, head.Nodes.Item2.Nodes.Item1.Value);
            Assert.AreEqual(5, head.Nodes.Item2.Nodes.Item2.Value);

            Assert.AreEqual(6, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test05_Unary()
        {
            var src = "int main() { -10 + 20; }";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));
            var head = nodeMap.Head.Nodes.Item1.Bodies[0];
            Assert.AreEqual(NodeKind.Add, head.Kind);
            Assert.AreEqual(NodeKind.Sub, head.Nodes.Item1.Kind);
            Assert.AreEqual(20, head.Nodes.Item2.Value);
            Assert.AreEqual(NodeKind.Num, head.Nodes.Item1.Nodes.Item1.Kind);
            Assert.AreEqual(0, head.Nodes.Item1.Nodes.Item1.Value);
            Assert.AreEqual(10, head.Nodes.Item1.Nodes.Item2.Value);

            Assert.AreEqual(10, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test06_Comparators()
        {
            var comparators = new List<Tuple<NodeKind, string>>() {
                { Tuple.Create(NodeKind.Eq, "==") },
                { Tuple.Create(NodeKind.Ne, "!=") },
                { Tuple.Create(NodeKind.Lt, "<") },
                { Tuple.Create(NodeKind.Le, "<=") },
            };
            foreach (var tuple in comparators)
            {
                var tokenList = TokenList.Tokenize("int main() { 10 " + tuple.Item2 + " 20; }");
                var nodeMap = NodeMap.Create(tokenList);
                Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));
                var head = nodeMap.Head.Nodes.Item1.Bodies[0];
                Assert.AreEqual(tuple.Item1, head.Kind);
                Assert.AreEqual(10, head.Nodes.Item1.Value);
                Assert.AreEqual(20, head.Nodes.Item2.Value);
            }

            var inverseComparators = new List<Tuple<NodeKind, string>>() {
                { Tuple.Create(NodeKind.Lt, ">") },
                { Tuple.Create(NodeKind.Le, ">=") },
            };
            foreach (var tuple in inverseComparators)
            {
                var tokenList = TokenList.Tokenize("int main() { 10 " + tuple.Item2 + " 20; }");
                var nodeMap = NodeMap.Create(tokenList);
                Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));
                var head = nodeMap.Head.Nodes.Item1.Bodies[0];
                Assert.AreEqual(tuple.Item1, head.Kind);
                Assert.AreEqual(20, head.Nodes.Item1.Value);
                Assert.AreEqual(10, head.Nodes.Item2.Value);
            }
        }

        [TestMethod]
        public void Test07_1文字変数()
        {
            var src = @"
int main() {
    int a;
    int z;
    a = 10;
    z = 50;
    a = z + a;
}
";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));
            var block = nodeMap.Head.Nodes.Item1;
            Assert.AreEqual(5, block.Bodies.Count);
            var n0 = block.Bodies[2];
            var n1 = block.Bodies[3];
            var n2 = block.Bodies[4];
            Assert.AreEqual(8, n0.Nodes.Item1.Offset);
            Assert.AreEqual(10, n0.Nodes.Item2.Value);
            Assert.AreEqual(16, n1.Nodes.Item1.Offset);
            Assert.AreEqual(50, n1.Nodes.Item2.Value);
            Assert.AreEqual(8, n2.Nodes.Item1.Offset);
            Assert.AreEqual(NodeKind.Add, n2.Nodes.Item2.Kind);
            Assert.AreEqual(16, n2.Nodes.Item2.Nodes.Item1.Offset);
            Assert.AreEqual(8, n2.Nodes.Item2.Nodes.Item2.Offset);

            Assert.AreEqual(60, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test08_複数文字のローカル変数()
        {
            var src = @"
int main() {
    int foo;
    int bar;
    foo = 1;
    bar = 2 + 3;
    foo + bar;
}
";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));
            var block = nodeMap.Head.Nodes.Item1;
            Assert.AreEqual(block.Bodies.Count, 5);
            var n0 = block.Bodies[2];
            var n1 = block.Bodies[3];
            var n2 = block.Bodies[4];
            Assert.AreEqual(8, n0.Nodes.Item1.Offset);
            Assert.AreEqual(1, n0.Nodes.Item2.Value);
            Assert.AreEqual(16, n1.Nodes.Item1.Offset);
            Assert.AreEqual(NodeKind.Add, n1.Nodes.Item2.Kind);
            Assert.AreEqual(2, n1.Nodes.Item2.Nodes.Item1.Value);
            Assert.AreEqual(3, n1.Nodes.Item2.Nodes.Item2.Value);
            Assert.AreEqual(NodeKind.Add, n2.Kind);
            Assert.AreEqual(8, n2.Nodes.Item1.Offset);
            Assert.AreEqual(16, n2.Nodes.Item2.Offset);

            Assert.AreEqual(6, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test09_return文()
        {
            var src = @"
int main() {
    int abc;
    abc = 15;
    return abc;
}
";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));
            var block = nodeMap.Head.Nodes.Item1;
            Assert.AreEqual(3, block.Bodies.Count);
            var n0 = block.Bodies[1];
            var n1 = block.Bodies[2];
            Assert.AreEqual(NodeKind.Assign, n0.Kind);
            Assert.AreEqual(8, n0.Nodes.Item1.Offset);
            Assert.AreEqual(15, n0.Nodes.Item2.Value);
            Assert.AreEqual(NodeKind.Return, n1.Kind);
            Assert.AreEqual(8, n1.Nodes.Item1.Offset);
            Assert.IsNull(n1.Nodes.Item2);

            Assert.AreEqual(15, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test10_if文()
        {
            var src = @"
int main() {
    if ( 1 )
        return 2;
    else
        return 3;
}
";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));
            var block = nodeMap.Head.Nodes.Item1;
            Assert.AreEqual(1, block.Bodies.Count);
            var n0 = block.Bodies[0];
            Assert.AreEqual(NodeKind.If, n0.Kind);
            Assert.AreEqual(1, n0.Condition.Value);
            Assert.AreEqual(NodeKind.Return, n0.Nodes.Item1.Kind);
            Assert.AreEqual(2, n0.Nodes.Item1.Nodes.Item1.Value);
            Assert.AreEqual(NodeKind.Return, n0.Nodes.Item2.Kind);
            Assert.AreEqual(3, n0.Nodes.Item2.Nodes.Item1.Value);

            Assert.AreEqual(2, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test11_while文()
        {
            var src = @"
int main() {
    int a;
    int b;
    a = 0;
    b = 3;
    while ( a < b )
        a = a + 1;
    return a;
}
";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));

            Assert.AreEqual(3, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test12_for文()
        {
            var src = @"
int main() {
    int a;
    int i;
    a = 10;
    i = 0;
    for ( i = 0; i < 3; i = i + 1 )
        a = a + 1;
    return a;
}
";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));

            Assert.AreEqual(13, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test13_block()
        {
            var src = @"
int main() {
    int a;
    int i;
    a = 10;
    i = 0;
    for ( i = 0; i < 3; i = i + 1 ) {
        a = a + 1;
        a = a + 2;
    }
    return a;
}
";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));

            Assert.AreEqual(19, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test14_1_関数呼び出し()
        {
            var src = @"
int main() {
    return sub();
}
";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));

            Assert.AreEqual(10, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test14_2_関数呼び出し()
        {
            var src = @"
int hoge() {
    return 15;
}

int main() {
    return hoge();
}
";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));

            Assert.AreEqual(15, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test15_アドレスとポインタ()
        {
            var src = @"
int main() {
    int a;
    int *b;
    a = 10;
    b = &a;
    return *b;
}
";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));

            Assert.AreEqual(10, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test16_アドレスとポインタその２()
        {
            var src = @"
int main() {
    int a;
    int b;
    int *c;
    a = 3;
    b = 5;
    c = &b + 8;
    return *c;
}
";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));

            Assert.AreEqual(3, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test17_ポインタへの代入()
        {
            var src = @"
int main() {
    int a;
    int *b;
    b = &a;
    *b = 18;
    return a;
}
";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));

            Assert.AreEqual(18, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test18_ポインタのポインタへの代入()
        {
            var src = @"
int main() {
    int a;
    int *b;
    int **c;
    c = &b;
    b = &a;
    **c = 27;
    return a;
}
";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));

            Assert.AreEqual(27, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test19_1_ポインタの加算と減算()
        {
            // TODO: 現状は全ての変数をスタックに 8byte ずつ積んでいるので成立する
            var src = @"
int main() {
    int a;
    int b;
    int c;
    int d;
    d = 1;
    c = 2;
    b = 4;
    a = 8;
    int *p;
    int *q;
    p = &d;
    q = p + 3;
    return *q;
}
";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));

            Assert.AreEqual(8, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test19_2_ポインタのLVarに代入()
        {
            var src = @"
int main() {
    int a;
    int b;
    int *p;
    p = &b;
    *p = 3;
    *(p + 2) = 5;
    return b;
}
";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));

            Assert.AreEqual(3, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test20_sizeof演算子()
        {
            var src = @"
int main() {
    int a;
    int *b;
    return sizeof(a) + sizeof(b) + sizeof(100);
}
";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));

            Assert.AreEqual(24, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test21_配列()
        {
            var src = @"
int main() {
    int a[2];
    *a = 2;
    *(a+1) = 10;
    int *p;
    p = a;
    *p = *p * 2;
    return *p + *(p+1);
}
";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));

            Assert.AreEqual(14, CopmlileAndExecOnWsl(src));
        }

        [TestMethod]
        public void Test22_配列の添字()
        {
            var src = @"
int main() {
    int a[2];
    a[0] = 2;
    a[1] = 10;
    int *p;
    p = a;
    p[0] = p[0] * 2;
    return p[0] + p[1];
}
";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));

            Assert.AreEqual(14, CopmlileAndExecOnWsl(src));
        }
    }
}
