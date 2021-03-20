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

            if (node.Kind != NodeKind.Lvar && node.Offset != 0)
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

                Assert.AreEqual(output.ToString(), outTest.ToString());
            }

            Assert.AreEqual(CopmlileAndExecOnWsl(src), 100);
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

                Assert.AreEqual(output.ToString(), outTest.ToString());
            }

            Assert.AreEqual(CopmlileAndExecOnWsl(src), 15);
        }

        [TestMethod]
        public void Test03_トークン解析()
        {
            var src = "int main() { 10 + 50 - 60; }";
            var tokenList = TokenList.Tokenize(src);
            Assert.AreEqual(tokenList.Count, 12);
            Assert.IsNotNull(tokenList.Expect(TokenKind.Type));
            Assert.AreEqual(tokenList.ExpectIdent().Str, "main");
            Assert.IsTrue(tokenList.Consume('('));
            Assert.IsTrue(tokenList.Consume(')'));
            Assert.IsTrue(tokenList.Consume('{'));
            Assert.AreEqual(tokenList.ExpectNumber(), 10);
            Assert.IsTrue(tokenList.Consume('+'));
            Assert.AreEqual(tokenList.ExpectNumber(), 50);
            Assert.IsTrue(tokenList.Consume('-'));
            Assert.AreEqual(tokenList.ExpectNumber(), 60);
            Assert.IsTrue(tokenList.Consume(';'));
            Assert.IsTrue(tokenList.Consume('}'));
            Assert.IsTrue(tokenList.IsEof());

            Assert.AreEqual(CopmlileAndExecOnWsl(src), 0);
        }

        [TestMethod]
        public void Test04_NodeMap解析()
        {
            var src = "int main() { 100 - ( 7 + 2 ) * 10 - 20 / 5; }";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));
            var head = nodeMap.Head.Nodes.Item1.Bodies[0];
            Assert.AreEqual(head.Kind, NodeKind.Sub);
            Assert.AreEqual(head.Nodes.Item1.Kind, NodeKind.Sub);
            Assert.AreEqual(head.Nodes.Item2.Kind, NodeKind.Div);
            Assert.AreEqual(head.Nodes.Item1.Nodes.Item1.Value, 100);
            Assert.AreEqual(head.Nodes.Item1.Nodes.Item2.Kind, NodeKind.Mul);
            Assert.AreEqual(head.Nodes.Item1.Nodes.Item2.Nodes.Item1.Kind, NodeKind.Add);
            Assert.AreEqual(head.Nodes.Item1.Nodes.Item2.Nodes.Item2.Value, 10);
            Assert.AreEqual(head.Nodes.Item1.Nodes.Item2.Nodes.Item1.Nodes.Item1.Value, 7);
            Assert.AreEqual(head.Nodes.Item1.Nodes.Item2.Nodes.Item1.Nodes.Item2.Value, 2);
            Assert.AreEqual(head.Nodes.Item2.Nodes.Item1.Value, 20);
            Assert.AreEqual(head.Nodes.Item2.Nodes.Item2.Value, 5);

            Assert.AreEqual(CopmlileAndExecOnWsl(src), 6);
        }

        [TestMethod]
        public void Test05_Unary()
        {
            var src = "int main() { -10 + 20; }";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));
            var head = nodeMap.Head.Nodes.Item1.Bodies[0];
            Assert.AreEqual(head.Kind, NodeKind.Add);
            Assert.AreEqual(head.Nodes.Item1.Kind, NodeKind.Sub);
            Assert.AreEqual(head.Nodes.Item2.Value, 20); ;
            Assert.AreEqual(head.Nodes.Item1.Nodes.Item1.Kind, NodeKind.Num);
            Assert.AreEqual(head.Nodes.Item1.Nodes.Item1.Value, 0);
            Assert.AreEqual(head.Nodes.Item1.Nodes.Item2.Value, 10);

            Assert.AreEqual(CopmlileAndExecOnWsl(src), 10);
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
                Assert.AreEqual(head.Kind, tuple.Item1);
                Assert.AreEqual(head.Nodes.Item1.Value, 10);
                Assert.AreEqual(head.Nodes.Item2.Value, 20);
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
                Assert.AreEqual(head.Kind, tuple.Item1);
                Assert.AreEqual(head.Nodes.Item1.Value, 20);
                Assert.AreEqual(head.Nodes.Item2.Value, 10);
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
            Assert.AreEqual(block.Bodies.Count, 5);
            var n0 = block.Bodies[2];
            var n1 = block.Bodies[3];
            var n2 = block.Bodies[4];
            Assert.AreEqual(n0.Nodes.Item1.Offset, 8);
            Assert.AreEqual(n0.Nodes.Item2.Value, 10);
            Assert.AreEqual(n1.Nodes.Item1.Offset, 16);
            Assert.AreEqual(n1.Nodes.Item2.Value, 50);
            Assert.AreEqual(n2.Nodes.Item1.Offset, 8);
            Assert.AreEqual(n2.Nodes.Item2.Kind, NodeKind.Add);
            Assert.AreEqual(n2.Nodes.Item2.Nodes.Item1.Offset, 16);
            Assert.AreEqual(n2.Nodes.Item2.Nodes.Item2.Offset, 8);

            Assert.AreEqual(CopmlileAndExecOnWsl(src), 60);
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
            Assert.AreEqual(n0.Nodes.Item1.Offset, 8);
            Assert.AreEqual(n0.Nodes.Item2.Value, 1);
            Assert.AreEqual(n1.Nodes.Item1.Offset, 16);
            Assert.AreEqual(n1.Nodes.Item2.Kind, NodeKind.Add);
            Assert.AreEqual(n1.Nodes.Item2.Nodes.Item1.Value, 2);
            Assert.AreEqual(n1.Nodes.Item2.Nodes.Item2.Value, 3);
            Assert.AreEqual(n2.Kind, NodeKind.Add);
            Assert.AreEqual(n2.Nodes.Item1.Offset, 8);
            Assert.AreEqual(n2.Nodes.Item2.Offset, 16);

            Assert.AreEqual(CopmlileAndExecOnWsl(src), 6);
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
            Assert.AreEqual(block.Bodies.Count, 3);
            var n0 = block.Bodies[1];
            var n1 = block.Bodies[2];
            Assert.AreEqual(n0.Kind, NodeKind.Assign);
            Assert.AreEqual(n0.Nodes.Item1.Offset, 8);
            Assert.AreEqual(n0.Nodes.Item2.Value, 15);
            Assert.AreEqual(n1.Kind, NodeKind.Return);
            Assert.AreEqual(n1.Nodes.Item1.Offset, 8);
            Assert.IsNull(n1.Nodes.Item2);

            Assert.AreEqual(CopmlileAndExecOnWsl(src), 15);
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
            Assert.AreEqual(block.Bodies.Count, 1);
            var n0 = block.Bodies[0];
            Assert.AreEqual(n0.Kind, NodeKind.If);
            Assert.AreEqual(n0.Condition.Value, 1);
            Assert.AreEqual(n0.Nodes.Item1.Kind, NodeKind.Return);
            Assert.AreEqual(n0.Nodes.Item1.Nodes.Item1.Value, 2);
            Assert.AreEqual(n0.Nodes.Item2.Kind, NodeKind.Return);
            Assert.AreEqual(n0.Nodes.Item2.Nodes.Item1.Value, 3);

            Assert.AreEqual(CopmlileAndExecOnWsl(src), 2);
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

            Assert.AreEqual(CopmlileAndExecOnWsl(src), 3);
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

            Assert.AreEqual(CopmlileAndExecOnWsl(src), 13);
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

            Assert.AreEqual(CopmlileAndExecOnWsl(src), 19);
        }

        [TestMethod]
        public void Test14_関数呼び出し()
        {
            var src = @"
int main() {
    return sub();
}
";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));

            Assert.AreEqual(CopmlileAndExecOnWsl(src), 10);
        }

        [TestMethod]
        public void Test15_アドレスとポインタ()
        {
            var src = @"
int main() {
    int a;
    int b;
    a = 10;
    b = &a;
    return *b;
}
";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));

            Assert.AreEqual(CopmlileAndExecOnWsl(src), 10);
        }

        [TestMethod]
        public void Test16_アドレスとポインタその２()
        {
            var src = @"
int main() {
    int a;
    int b;
    int c;
    a = 3;
    b = 5;
    c = &b + 8;
    return *c;
}
";
            var tokenList = TokenList.Tokenize(src);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));

            Assert.AreEqual(CopmlileAndExecOnWsl(src), 3);
        }
    }
}
