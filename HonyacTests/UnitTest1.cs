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
            outTest.AppendLine("  mov rax, 100");
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
            outTest.AppendLine("  mov rax, 80");
            outTest.AppendLine("  sub rax, 95");
            outTest.AppendLine("  add rax, 50");
            outTest.AppendLine("  sub rax, 20");
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
            Assert.AreEqual(nodeMap.Head.Kind, NodeKind.Sub);
            Assert.AreEqual(nodeMap.Head.Nodes.Item1.Kind, NodeKind.Sub);
            Assert.AreEqual(nodeMap.Head.Nodes.Item2.Kind, NodeKind.Div);
            Assert.AreEqual(nodeMap.Head.Nodes.Item1.Nodes.Item1.Value, 100);
            Assert.AreEqual(nodeMap.Head.Nodes.Item1.Nodes.Item2.Kind, NodeKind.Mul);
            Assert.AreEqual(nodeMap.Head.Nodes.Item1.Nodes.Item2.Nodes.Item1.Kind, NodeKind.Add);
            Assert.AreEqual(nodeMap.Head.Nodes.Item1.Nodes.Item2.Nodes.Item2.Value, 10);
            Assert.AreEqual(nodeMap.Head.Nodes.Item1.Nodes.Item2.Nodes.Item1.Nodes.Item1.Value, 78);
            Assert.AreEqual(nodeMap.Head.Nodes.Item1.Nodes.Item2.Nodes.Item1.Nodes.Item2.Value, 25);
            Assert.AreEqual(nodeMap.Head.Nodes.Item2.Nodes.Item1.Value, 20);
            Assert.AreEqual(nodeMap.Head.Nodes.Item2.Nodes.Item2.Value, 5);
        }
    }
}
