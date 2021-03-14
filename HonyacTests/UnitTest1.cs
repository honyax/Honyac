using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

        [TestMethod]
        public void Test01_単一の数字をコンパイル()
        {
            var inTest = "main() { 100; }";
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

                Program.Main(new string[] { inTest });

                Assert.AreEqual(output.ToString(), outTest.ToString());
            }
        }

        [TestMethod]
        public void Test02_四則演算をコンパイル()
        {
            var inTest = "main() { 80 - 95 + 50 - 20; }";
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

                Program.Main(new string[] { inTest });

                Assert.AreEqual(output.ToString(), outTest.ToString());
            }
        }

        [TestMethod]
        public void Test03_トークン解析()
        {
            var tokenList = TokenList.Tokenize("main() { 10 + 50 - 60; }");
            Assert.AreEqual(tokenList.Count, 11);
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
        }

        [TestMethod]
        public void Test04_NodeMap解析()
        {
            var tokenList = TokenList.Tokenize("main() { 100 - ( 78 + 25 ) * 10 - 20 / 5; }");
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
            Assert.AreEqual(head.Nodes.Item1.Nodes.Item2.Nodes.Item1.Nodes.Item1.Value, 78);
            Assert.AreEqual(head.Nodes.Item1.Nodes.Item2.Nodes.Item1.Nodes.Item2.Value, 25);
            Assert.AreEqual(head.Nodes.Item2.Nodes.Item1.Value, 20);
            Assert.AreEqual(head.Nodes.Item2.Nodes.Item2.Value, 5);
        }

        [TestMethod]
        public void Test05_Unary()
        {
            var tokenList = TokenList.Tokenize("main() { -10 + 20; }");
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));
            var head = nodeMap.Head.Nodes.Item1.Bodies[0];
            Assert.AreEqual(head.Kind, NodeKind.Add);
            Assert.AreEqual(head.Nodes.Item1.Kind, NodeKind.Sub);
            Assert.AreEqual(head.Nodes.Item2.Value, 20); ;
            Assert.AreEqual(head.Nodes.Item1.Nodes.Item1.Kind, NodeKind.Num);
            Assert.AreEqual(head.Nodes.Item1.Nodes.Item1.Value, 0);
            Assert.AreEqual(head.Nodes.Item1.Nodes.Item2.Value, 10);
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
                var tokenList = TokenList.Tokenize("main() { 10 " + tuple.Item2 + " 20; }");
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
                var tokenList = TokenList.Tokenize("main() { 10 " + tuple.Item2 + " 20; }");
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
            var str = @"
main() {
    a = 10;
    z = 50;
    a = z + a;
}
";
            var tokenList = TokenList.Tokenize(str);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));
            var block = nodeMap.Head.Nodes.Item1;
            Assert.AreEqual(block.Bodies.Count, 3);
            var n0 = block.Bodies[0];
            var n1 = block.Bodies[1];
            var n2 = block.Bodies[2];
            Assert.AreEqual(n0.Nodes.Item1.Offset, 8);
            Assert.AreEqual(n0.Nodes.Item2.Value, 10);
            Assert.AreEqual(n1.Nodes.Item1.Offset, 16);
            Assert.AreEqual(n1.Nodes.Item2.Value, 50);
            Assert.AreEqual(n2.Nodes.Item1.Offset, 8);
            Assert.AreEqual(n2.Nodes.Item2.Kind, NodeKind.Add);
            Assert.AreEqual(n2.Nodes.Item2.Nodes.Item1.Offset, 16);
            Assert.AreEqual(n2.Nodes.Item2.Nodes.Item2.Offset, 8);
        }

        [TestMethod]
        public void Test08_複数文字のローカル変数()
        {
            var sb = new StringBuilder();
            var str = @"
main() {
    foo = 1;
    bar = 2 + 3;
    foo + bar;
}
";
            var tokenList = TokenList.Tokenize(str);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));
            var block = nodeMap.Head.Nodes.Item1;
            Assert.AreEqual(block.Bodies.Count, 3);
            var n0 = block.Bodies[0];
            var n1 = block.Bodies[1];
            var n2 = block.Bodies[2];
            Assert.AreEqual(n0.Nodes.Item1.Offset, 8);
            Assert.AreEqual(n0.Nodes.Item2.Value, 1);
            Assert.AreEqual(n1.Nodes.Item1.Offset, 16);
            Assert.AreEqual(n1.Nodes.Item2.Kind, NodeKind.Add);
            Assert.AreEqual(n1.Nodes.Item2.Nodes.Item1.Value, 2);
            Assert.AreEqual(n1.Nodes.Item2.Nodes.Item2.Value, 3);
            Assert.AreEqual(n2.Kind, NodeKind.Add);
            Assert.AreEqual(n2.Nodes.Item1.Offset, 8);
            Assert.AreEqual(n2.Nodes.Item2.Offset, 16);
        }

        [TestMethod]
        public void Test09_return文()
        {
            var str = @"
main() {
    abc = 15;
    return abc;
}
";
            var tokenList = TokenList.Tokenize(str);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));
            var block = nodeMap.Head.Nodes.Item1;
            Assert.AreEqual(block.Bodies.Count, 2);
            var n0 = block.Bodies[0];
            var n1 = block.Bodies[1];
            Assert.AreEqual(n0.Kind, NodeKind.Assign);
            Assert.AreEqual(n0.Nodes.Item1.Offset, 8);
            Assert.AreEqual(n0.Nodes.Item2.Value, 15);
            Assert.AreEqual(n1.Kind, NodeKind.Return);
            Assert.AreEqual(n1.Nodes.Item1.Offset, 8);
            Assert.IsNull(n1.Nodes.Item2);
        }

        [TestMethod]
        public void Test10_if文()
        {
            var str = @"
main() {
    if ( 1 )
        return 2;
    else
        return 3;
}
";
            var tokenList = TokenList.Tokenize(str);
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
        }

        [TestMethod]
        public void Test11_while文()
        {
            var str = @"
main() {
    a = 0;
    b = 3;
    while ( a < b )
        a = a + 1;
    return a;
}
";
            var tokenList = TokenList.Tokenize(str);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));
        }

        [TestMethod]
        public void Test12_for文()
        {
            var str = @"
main() {
    a = 10;
    i = 0;
    for ( i = 0; i < 3; i = i + 1 )
        a = a + 1;
    return a;
}
";
            var tokenList = TokenList.Tokenize(str);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));
        }

        [TestMethod]
        public void Test13_block()
        {
            var str = @"
main() {
    a = 10;
    i = 0;
    for ( i = 0; i < 3; i = i + 1 ) {
        a = a + 1;
        a = a + 2;
    }
    return a;
}
";
            var tokenList = TokenList.Tokenize(str);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));
        }

        [TestMethod]
        public void Test14_関数呼び出し()
        {
            var str = @"
main() {
    return sub();
}
";
            var tokenList = TokenList.Tokenize(str);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));
        }

        [TestMethod]
        public void Test15_アドレスとポインタ()
        {
            var str = @"
main() {
    a = 10;
    b = &a;
    return *b;
}
";
            var tokenList = TokenList.Tokenize(str);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));
        }

        [TestMethod]
        public void Test16_アドレスとポインタその２()
        {
            var str = @"
main() {
    a = 3;
    b = 5;
    c = &b + 8;
    return *c;
}
";
            var tokenList = TokenList.Tokenize(str);
            var nodeMap = NodeMap.Create(tokenList);
            Assert.IsTrue(ValidateNodeValuesAndOffsets(nodeMap));
        }
    }
}
