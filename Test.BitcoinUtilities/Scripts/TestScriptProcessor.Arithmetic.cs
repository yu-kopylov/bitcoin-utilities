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
        public void TestArithmetic_FailWhenPresent()
        {
            AssertFailWhenPresent
            (
                BitcoinScript.OP_2MUL,
                BitcoinScript.OP_2DIV,
                BitcoinScript.OP_MUL,
                BitcoinScript.OP_DIV,
                BitcoinScript.OP_MOD,
                BitcoinScript.OP_LSHIFT,
                BitcoinScript.OP_RSHIFT
            );
        }

        [Test]
        public void TestAdd1()
        {
            ScriptProcessor processor = new ScriptProcessor();

            // positive number
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_3,
                BitcoinScript.OP_1ADD
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"04"}).IgnoreCase);

            // negative number
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 1, 0x83,
                BitcoinScript.OP_1ADD
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"82"}).IgnoreCase);

            // zero
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_FALSE,
                BitcoinScript.OP_1ADD
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"01"}).IgnoreCase);

            // zero result
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 1, 0x81,
                BitcoinScript.OP_1ADD
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {""}).IgnoreCase);

            // overflow
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 0x4, 0xFF, 0xFF, 0xFF, 0x7F,
                BitcoinScript.OP_1ADD
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"0000008000"}).IgnoreCase);

            // non-minimal (negative zero)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 1, 0x80,
                BitcoinScript.OP_1ADD
            });

            Assert.False(processor.Valid);

            // non-minimal (positive)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 2, 0x7F, 0x00,
                BitcoinScript.OP_1ADD
            });

            Assert.False(processor.Valid);

            // non-minimal (negative)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 2, 0x7F, 0x80,
                BitcoinScript.OP_1ADD
            });

            Assert.False(processor.Valid);

            // input value too large
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 5, 0x11, 0x11, 0x11, 0x11, 0x11,
                BitcoinScript.OP_1ADD
            });

            Assert.False(processor.Valid);

            // input stack size (0)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_1ADD
            });

            Assert.False(processor.Valid);
        }

        [Test]
        public void TestSubtract1()
        {
            ScriptProcessor processor = new ScriptProcessor();

            // positive number
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_3,
                BitcoinScript.OP_1SUB
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"02"}).IgnoreCase);

            // negative number
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 1, 0x83,
                BitcoinScript.OP_1SUB
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"84"}).IgnoreCase);

            // zero
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_FALSE,
                BitcoinScript.OP_1SUB
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"81"}).IgnoreCase);

            // zero result
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_TRUE,
                BitcoinScript.OP_1SUB
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {""}).IgnoreCase);

            // overflow
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 0x4, 0xFF, 0xFF, 0xFF, 0xFF,
                BitcoinScript.OP_1SUB
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"0000008080"}).IgnoreCase);

            // non-minimal (negative zero)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 1, 0x80,
                BitcoinScript.OP_1SUB
            });

            Assert.False(processor.Valid);

            // non-minimal (positive)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 2, 0x7F, 0x00,
                BitcoinScript.OP_1SUB
            });

            Assert.False(processor.Valid);

            // non-minimal (negative)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 2, 0x7F, 0x80,
                BitcoinScript.OP_1SUB
            });

            Assert.False(processor.Valid);

            // input value too large
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 5, 0x11, 0x11, 0x11, 0x11, 0x11,
                BitcoinScript.OP_1SUB
            });

            Assert.False(processor.Valid);

            // input stack size (0)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_1SUB
            });

            Assert.False(processor.Valid);
        }

        [Test]
        public void TestNegate()
        {
            ScriptProcessor processor = new ScriptProcessor();

            // positive number
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 4, 0x11, 0x22, 0x33, 0x44,
                BitcoinScript.OP_NEGATE
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"112233C4"}).IgnoreCase);

            // negative number
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 4, 0x11, 0x22, 0x33, 0xC4,
                BitcoinScript.OP_NEGATE
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"11223344"}).IgnoreCase);

            // zero
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_FALSE,
                BitcoinScript.OP_NEGATE
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {""}).IgnoreCase);

            // non-minimal (negative zero)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 1, 0x80,
                BitcoinScript.OP_NEGATE
            });

            Assert.False(processor.Valid);

            // non-minimal (positive)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 2, 0x7F, 0x00,
                BitcoinScript.OP_NEGATE
            });

            Assert.False(processor.Valid);

            // non-minimal (negative)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 2, 0x7F, 0x80,
                BitcoinScript.OP_NEGATE
            });

            Assert.False(processor.Valid);

            // input value too large
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 5, 0x11, 0x11, 0x11, 0x11, 0x11,
                BitcoinScript.OP_NEGATE
            });

            Assert.False(processor.Valid);

            // input stack size (0)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_NEGATE
            });

            Assert.False(processor.Valid);
        }

        [Test]
        public void TestAbs()
        {
            ScriptProcessor processor = new ScriptProcessor();

            // positive number
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 4, 0x11, 0x22, 0x33, 0x44,
                BitcoinScript.OP_ABS
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"11223344"}).IgnoreCase);

            // negative number
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 4, 0x11, 0x22, 0x33, 0xC4,
                BitcoinScript.OP_ABS
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"11223344"}).IgnoreCase);

            // zero
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_FALSE,
                BitcoinScript.OP_ABS
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {""}).IgnoreCase);

            // non-minimal (negative zero)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 1, 0x80,
                BitcoinScript.OP_ABS
            });

            Assert.False(processor.Valid);

            // non-minimal (positive)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 2, 0x7F, 0x00,
                BitcoinScript.OP_ABS
            });

            Assert.False(processor.Valid);

            // non-minimal (negative)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 2, 0x7F, 0x80,
                BitcoinScript.OP_ABS
            });

            Assert.False(processor.Valid);

            // input value too large
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 5, 0x11, 0x11, 0x11, 0x11, 0x11,
                BitcoinScript.OP_ABS
            });

            Assert.False(processor.Valid);

            // input stack size (0)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_ABS
            });

            Assert.False(processor.Valid);
        }

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
        public void TestZeroNotEqual()
        {
            ScriptProcessor processor = new ScriptProcessor();

            // zero
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_FALSE,
                BitcoinScript.OP_0NOTEQUAL
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {""}).IgnoreCase);

            // one
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_TRUE,
                BitcoinScript.OP_0NOTEQUAL
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"01"}).IgnoreCase);

            // any other number (2)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_2,
                BitcoinScript.OP_0NOTEQUAL
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"01"}).IgnoreCase);

            // any other number (3)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_3,
                BitcoinScript.OP_0NOTEQUAL
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"01"}).IgnoreCase);

            // any other number (-1)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 1, 0x81,
                BitcoinScript.OP_0NOTEQUAL
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"01"}).IgnoreCase);

            // non-minimal (negative zero)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 1, 0x80,
                BitcoinScript.OP_0NOTEQUAL
            });

            Assert.False(processor.Valid);

            // non-minimal (positive)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 2, 0x7F, 0x00,
                BitcoinScript.OP_0NOTEQUAL
            });

            Assert.False(processor.Valid);

            // non-minimal (negative)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 2, 0x7F, 0x80,
                BitcoinScript.OP_0NOTEQUAL
            });

            Assert.False(processor.Valid);

            // input value too large
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 5, 0x11, 0x11, 0x11, 0x11, 0x11,
                BitcoinScript.OP_0NOTEQUAL
            });

            Assert.False(processor.Valid);

            // input stack size (0)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_0NOTEQUAL
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

        [Test]
        public void TestMin()
        {
            ScriptProcessor processor = new ScriptProcessor();

            // A < B
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_2,
                BitcoinScript.OP_3,
                BitcoinScript.OP_MIN
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"02"}).IgnoreCase);

            // B < A
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_3,
                BitcoinScript.OP_2,
                BitcoinScript.OP_MIN
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"02"}).IgnoreCase);

            // positive and short-negative
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 4, 0x11, 0x12, 0x13, 0x14,
                BitcoinScript.OP_PUSHDATA1, 1, 0x81,
                BitcoinScript.OP_MIN
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"81"}).IgnoreCase);

            // positive and long-negative
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_TRUE,
                BitcoinScript.OP_PUSHDATA1, 4, 0x11, 0x12, 0x13, 0x94,
                BitcoinScript.OP_MIN
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"11121394"}).IgnoreCase);

            // non-minimal (negative zero (A))
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 1, 0x80,
                BitcoinScript.OP_3,
                BitcoinScript.OP_MIN
            });

            Assert.False(processor.Valid);

            // non-minimal (negative zero (B))
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_3,
                BitcoinScript.OP_PUSHDATA1, 1, 0x80,
                BitcoinScript.OP_MIN
            });

            Assert.False(processor.Valid);

            // non-minimal (positive)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 2, 0x7F, 0x00,
                BitcoinScript.OP_3,
                BitcoinScript.OP_MIN
            });

            Assert.False(processor.Valid);

            // non-minimal (negative)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 2, 0x7F, 0x80,
                BitcoinScript.OP_3,
                BitcoinScript.OP_MIN
            });

            Assert.False(processor.Valid);

            // input value too large
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 5, 0x11, 0x11, 0x11, 0x11, 0x11,
                BitcoinScript.OP_3,
                BitcoinScript.OP_MIN
            });

            Assert.False(processor.Valid);

            // input stack size (1)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_3,
                BitcoinScript.OP_MIN
            });

            Assert.False(processor.Valid);

            // input stack size (0)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_MIN
            });

            Assert.False(processor.Valid);
        }

        [Test]
        public void TestMax()
        {
            ScriptProcessor processor = new ScriptProcessor();

            // A > B
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_3,
                BitcoinScript.OP_2,
                BitcoinScript.OP_MAX
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"03"}).IgnoreCase);

            // B > A
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_2,
                BitcoinScript.OP_3,
                BitcoinScript.OP_MAX
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"03"}).IgnoreCase);

            // negative and short-positive
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 4, 0x11, 0x12, 0x13, 0x94,
                BitcoinScript.OP_TRUE,
                BitcoinScript.OP_MAX
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"01"}).IgnoreCase);

            // negative and long-positive
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 1, 0x81,
                BitcoinScript.OP_PUSHDATA1, 4, 0x11, 0x12, 0x13, 0x14,
                BitcoinScript.OP_MAX
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString), Is.EqualTo(new string[] {"11121314"}).IgnoreCase);

            // non-minimal (negative zero (A))
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 1, 0x80,
                BitcoinScript.OP_3,
                BitcoinScript.OP_MAX
            });

            Assert.False(processor.Valid);

            // non-minimal (negative zero (B))
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_3,
                BitcoinScript.OP_PUSHDATA1, 1, 0x80,
                BitcoinScript.OP_MAX
            });

            Assert.False(processor.Valid);

            // non-minimal (positive)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 2, 0x7F, 0x00,
                BitcoinScript.OP_3,
                BitcoinScript.OP_MAX
            });

            Assert.False(processor.Valid);

            // non-minimal (negative)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 2, 0x7F, 0x80,
                BitcoinScript.OP_3,
                BitcoinScript.OP_MAX
            });

            Assert.False(processor.Valid);

            // input value too large
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 5, 0x11, 0x11, 0x11, 0x11, 0x11,
                BitcoinScript.OP_3,
                BitcoinScript.OP_MAX
            });

            Assert.False(processor.Valid);

            // input stack size (1)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_3,
                BitcoinScript.OP_MAX
            });

            Assert.False(processor.Valid);

            // input stack size (0)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_MAX
            });

            Assert.False(processor.Valid);
        }
    }
}