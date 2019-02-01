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
        public void TestSplice_FailWhenPresent()
        {
            AssertFailWhenPresent
            (
                BitcoinScript.OP_CAT,
                BitcoinScript.OP_SUBSTR,
                BitcoinScript.OP_LEFT,
                BitcoinScript.OP_RIGHT
            );
        }

        [Test]
        public void TestSize()
        {
            ScriptProcessor processor = new ScriptProcessor();

            // length = 0
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_FALSE,
                BitcoinScript.OP_SIZE
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString).ToArray(), Is.EqualTo(new string[] {"", ""}).IgnoreCase);

            // length = 1 (positive)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_TRUE,
                BitcoinScript.OP_SIZE
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString).ToArray(), Is.EqualTo(new string[] {"01", "01"}).IgnoreCase);

            // length = 1 (negative)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 1, 0x85,
                BitcoinScript.OP_SIZE
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString).ToArray(), Is.EqualTo(new string[] {"01", "85"}).IgnoreCase);

            // length = 2
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA1, 2, 0x01, 0x02,
                BitcoinScript.OP_SIZE
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString).ToArray(), Is.EqualTo(new string[] {"02", "0102"}).IgnoreCase);

            // length = 0x7F
            processor.Reset();
            processor.Execute(Enumerable.Empty<byte>()
                .Concat(new byte[] {BitcoinScript.OP_PUSHDATA4, 0x7F, 0x00, 0x00, 0x00})
                .Concat(new byte[0x7F])
                .Concat(new byte[] {BitcoinScript.OP_SIZE})
                .ToArray()
            );

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString).ToArray(), Is.EqualTo(new string[] {"7F", new string('0', 0xFE)}).IgnoreCase);

            // length = 0x80
            processor.Reset();
            processor.Execute(Enumerable.Empty<byte>()
                .Concat(new byte[] {BitcoinScript.OP_PUSHDATA4, 0x80, 0x00, 0x00, 0x00})
                .Concat(new byte[0x80])
                .Concat(new byte[] {BitcoinScript.OP_SIZE})
                .ToArray()
            );

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString).ToArray(), Is.EqualTo(new string[] {"8000", new string('0', 0x100)}).IgnoreCase);

            // length = 0x1234
            processor.Reset();
            processor.Execute(Enumerable.Empty<byte>()
                .Concat(new byte[] {BitcoinScript.OP_PUSHDATA4, 0x34, 0x12, 0x00, 0x00})
                .Concat(new byte[0x1234])
                .Concat(new byte[] {BitcoinScript.OP_SIZE})
                .ToArray()
            );

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack().Select(HexUtils.GetString).ToArray(), Is.EqualTo(new string[] {"3412", new string('0', 0x2468)}).IgnoreCase);

            // input stack size (0)
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_SIZE
            });

            Assert.False(processor.Valid);
        }
    }
}