using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Honyac;

namespace HonyacTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void Test01_単一の数字をコンパイル()
        {
            var inTest = 100;
            var outTest = new StringBuilder();
            outTest.AppendLine(".intel_syntax noprefix");
            outTest.AppendLine(".globl main");
            outTest.AppendLine("main:");
            outTest.AppendLine("  push 100");
            outTest.AppendLine("  pop rax");
            outTest.AppendLine("  ret");

            using (var output = new StringWriter())
            {
                Console.SetOut(output);

                Program.Main(new string[] { inTest.ToString() });

                Assert.AreEqual(output.ToString(), outTest.ToString());
            }
        }

        [TestMethod]
        public void Test02_四則演算をコンパイル()
        {
            var inTest = "80 - 95 + 50 - 20";
            var outTest = new StringBuilder();
            outTest.AppendLine(".intel_syntax noprefix");
            outTest.AppendLine(".globl main");
            outTest.AppendLine("main:");


            outTest.AppendLine("  push 80");
            outTest.AppendLine("  push 95");
            outTest.AppendLine("  pop rdi");
            outTest.AppendLine("  pop rax");
            outTest.AppendLine("  sub rax, rdi");
            outTest.AppendLine("  push rax");
            outTest.AppendLine("  push 50");
            outTest.AppendLine("  pop rdi");
            outTest.AppendLine("  pop rax");
            outTest.AppendLine("  add rax, rdi");
            outTest.AppendLine("  push rax");
            outTest.AppendLine("  push 20");
            outTest.AppendLine("  pop rdi");
            outTest.AppendLine("  pop rax");
            outTest.AppendLine("  sub rax, rdi");
            outTest.AppendLine("  push rax");
            outTest.AppendLine("  pop rax");
            outTest.AppendLine("  ret");

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
            var tokenList = TokenList.Tokenize("10 + 50 - 60");
            Assert.AreEqual(tokenList.Count, 5);
            Assert.AreEqual(tokenList.ExpectNumber(), 10);
            Assert.IsTrue(tokenList.Consume('+'));
            Assert.AreEqual(tokenList.ExpectNumber(), 50);
            Assert.IsTrue(tokenList.Consume('-'));
            Assert.AreEqual(tokenList.ExpectNumber(), 60);
            Assert.IsTrue(tokenList.IsEof());
        }

        [TestMethod]
        public void Test04_NodeMap解析()
        {
            var tokenList = TokenList.Tokenize("100 - ( 78 + 25 ) * 10 - 20 / 5");
            var nodeMap = NodeMap.Create(tokenList);
            var head = nodeMap.Head;
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
            var tokenList = TokenList.Tokenize("-10 + 20");
            var nodeMap = NodeMap.Create(tokenList);
            var head = nodeMap.Head;
            Assert.AreEqual(head.Kind, NodeKind.Add);
            Assert.AreEqual(head.Nodes.Item1.Kind, NodeKind.Sub);
            Assert.AreEqual(head.Nodes.Item2.Value, 20); ;
            Assert.AreEqual(head.Nodes.Item1.Nodes.Item1.Kind, NodeKind.Num);
            Assert.AreEqual(head.Nodes.Item1.Nodes.Item1.Value, 0);
            Assert.AreEqual(head.Nodes.Item1.Nodes.Item2.Value, 10);
        }
    }
}
