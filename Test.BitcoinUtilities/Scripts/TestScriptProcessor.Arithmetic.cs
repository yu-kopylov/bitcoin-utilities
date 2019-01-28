using System.Linq;
using BitcoinUtilities;
using BitcoinUtilities.Scripts;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Scripts
{
    [TestFixture]
    public partial class TestScriptProcessor
    {
        [Test]
        public void TestNot()
        {
            ScriptProcessor processor = new ScriptProcessor();

            // zero
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_FALSE,
                BitcoinScript.OP_NOT
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"01"}).IgnoreCase);

            // one
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_TRUE,
                BitcoinScript.OP_NOT
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {""}).IgnoreCase);

            // any other number (2)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_2,
                BitcoinScript.OP_NOT
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {""}).IgnoreCase);

            // any other number (3)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_3,
                BitcoinScript.OP_NOT
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {""}).IgnoreCase);

            // any other number (-1)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 1, 0x81,
                BitcoinScript.OP_NOT
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {""}).IgnoreCase);

            // non-minimal (negative zero)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 1, 0x80,
                BitcoinScript.OP_NOT
            });

            Assert.False(processor.Valid);

            // non-minimal (positive)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 2, 0x7F, 0x00,
                BitcoinScript.OP_NOT
            });

            Assert.False(processor.Valid);

            // non-minimal (negative)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 2, 0x7F, 0x80,
                BitcoinScript.OP_NOT
            });

            Assert.False(processor.Valid);

            // input value too large
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 5, 0x11, 0x11, 0x11, 0x11, 0x11,
                BitcoinScript.OP_NOT
            });

            Assert.False(processor.Valid);

            // input stack size (0)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_NOT
            });

            Assert.False(processor.Valid);
        }

        [Test]
        public void TestAdd()
        {
            ScriptProcessor processor = new ScriptProcessor();

            // simple case
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_2,
                BitcoinScript.OP_3,
                BitcoinScript.OP_ADD
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"05"}).IgnoreCase);

            // negative numbers
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 0x4, 0x12, 0x34, 0x56, 0x87,
                BitcoinScript.OP_PUSHDATA1, 0x4, 0x11, 0x11, 0x11, 0x81,
                BitcoinScript.OP_ADD
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"23456788"}).IgnoreCase);

            // zero input
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_FALSE,
                BitcoinScript.OP_3,
                BitcoinScript.OP_ADD
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"03"}).IgnoreCase);

            // zero result
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 0x1, 0x85,
                BitcoinScript.OP_PUSHDATA1, 0x1, 0x05,
                BitcoinScript.OP_ADD
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {""}).IgnoreCase);

            // positive overflow
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 0x4, 0xFF, 0xFF, 0xFF, 0x7F,
                BitcoinScript.OP_PUSHDATA1, 0x4, 0xFF, 0xFF, 0xFF, 0x7F,
                BitcoinScript.OP_ADD
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"FEFFFFFF00"}).IgnoreCase);

            // negative overflow
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 0x4, 0xFF, 0xFF, 0xFF, 0xFF,
                BitcoinScript.OP_PUSHDATA1, 0x4, 0xFF, 0xFF, 0xFF, 0xFF,
                BitcoinScript.OP_ADD
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"FEFFFFFF80"}).IgnoreCase);

            // non-minimal (negative zero (A))
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 1, 0x80,
                BitcoinScript.OP_3,
                BitcoinScript.OP_ADD
            });

            Assert.False(processor.Valid);

            // non-minimal (negative zero (B))
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_3,
                BitcoinScript.OP_PUSHDATA1, 1, 0x80,
                BitcoinScript.OP_ADD
            });

            Assert.False(processor.Valid);

            // non-minimal (positive)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 2, 0x7F, 0x00,
                BitcoinScript.OP_3,
                BitcoinScript.OP_ADD
            });

            Assert.False(processor.Valid);

            // non-minimal (negative)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 2, 0x7F, 0x80,
                BitcoinScript.OP_3,
                BitcoinScript.OP_ADD
            });

            Assert.False(processor.Valid);

            // input value too large
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 5, 0x11, 0x11, 0x11, 0x11, 0x11,
                BitcoinScript.OP_3,
                BitcoinScript.OP_ADD
            });

            Assert.False(processor.Valid);

            // input stack size (1)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_3,
                BitcoinScript.OP_ADD
            });

            Assert.False(processor.Valid);

            // input stack size (0)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_ADD
            });

            Assert.False(processor.Valid);
        }

        [Test]
        public void TestSubtract()
        {
            ScriptProcessor processor = new ScriptProcessor();

            // simple case
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_3,
                BitcoinScript.OP_2,
                BitcoinScript.OP_SUB
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"01"}).IgnoreCase);

            // negative numbers
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 0x4, 0x12, 0x34, 0x56, 0x87,
                BitcoinScript.OP_PUSHDATA1, 0x4, 0x11, 0x11, 0x11, 0x81,
                BitcoinScript.OP_SUB
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"01234586"}).IgnoreCase);

            // zero input
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_3,
                BitcoinScript.OP_FALSE,
                BitcoinScript.OP_SUB
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"03"}).IgnoreCase);

            // zero result
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 0x1, 0x05,
                BitcoinScript.OP_PUSHDATA1, 0x1, 0x05,
                BitcoinScript.OP_SUB
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {""}).IgnoreCase);

            // positive overflow
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 0x4, 0xFF, 0xFF, 0xFF, 0x7F,
                BitcoinScript.OP_PUSHDATA1, 0x4, 0xFF, 0xFF, 0xFF, 0xFF,
                BitcoinScript.OP_SUB
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"FEFFFFFF00"}).IgnoreCase);

            // negative overflow
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 0x4, 0xFF, 0xFF, 0xFF, 0xFF,
                BitcoinScript.OP_PUSHDATA1, 0x4, 0xFF, 0xFF, 0xFF, 0x7F,
                BitcoinScript.OP_SUB
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"FEFFFFFF80"}).IgnoreCase);

            // non-minimal (negative zero (A))
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 1, 0x80,
                BitcoinScript.OP_3,
                BitcoinScript.OP_SUB
            });

            Assert.False(processor.Valid);

            // non-minimal (negative zero (B))
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_3,
                BitcoinScript.OP_PUSHDATA1, 1, 0x80,
                BitcoinScript.OP_SUB
            });

            Assert.False(processor.Valid);

            // non-minimal (positive)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 2, 0x7F, 0x00,
                BitcoinScript.OP_3,
                BitcoinScript.OP_SUB
            });

            Assert.False(processor.Valid);

            // non-minimal (negative)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 2, 0x7F, 0x80,
                BitcoinScript.OP_3,
                BitcoinScript.OP_SUB
            });

            Assert.False(processor.Valid);

            // input value too large
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 5, 0x11, 0x11, 0x11, 0x11, 0x11,
                BitcoinScript.OP_3,
                BitcoinScript.OP_SUB
            });

            Assert.False(processor.Valid);

            // input stack size (1)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_3,
                BitcoinScript.OP_SUB
            });

            Assert.False(processor.Valid);

            // input stack size (0)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_SUB
            });

            Assert.False(processor.Valid);
        }
    }
}