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
            tokenList.TryConsumeNumber(out int value);
            Assert.AreEqual(value, 10);
            Assert.IsTrue(tokenList.Consume('+'));
            tokenList.TryConsumeNumber(out value);
            Assert.AreEqual(value, 50);
            Assert.IsTrue(tokenList.Consume('-'));
            tokenList.TryConsumeNumber(out value);
            Assert.AreEqual(value, 60);
            Assert.IsTrue(tokenList.IsEof());
        }
    }
}
