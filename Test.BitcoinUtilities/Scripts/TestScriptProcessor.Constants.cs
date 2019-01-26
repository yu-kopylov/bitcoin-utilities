using BitcoinUtilities.Scripts;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Scripts
{
    [TestFixture]
    public partial class TestScriptProcessor
    {
        [Test]
        public void TestConstants()
        {
            ScriptProcessor processor = new ScriptProcessor();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_FALSE,
                BitcoinScript.OP_PUSHDATA_LEN_1, 0xAA,
                BitcoinScript.OP_PUSHDATA_LEN_1 + 2, 0x01, 0x02, 0x03,
                BitcoinScript.OP_PUSHDATA1, 0x03, 0x04, 0x05, 0x06,
                BitcoinScript.OP_PUSHDATA2, 0x03, 0x00, 0x07, 0x08, 0x09,
                BitcoinScript.OP_PUSHDATA4, 0x05, 0x00, 0x00, 0x00, 0x11, 0x12, 0x13, 0x14, 0x15,
                BitcoinScript.OP_1NEGATE,
                BitcoinScript.OP_TRUE,
                BitcoinScript.OP_2,
                BitcoinScript.OP_2 + 1,
                BitcoinScript.OP_16
            });

            Assert.That(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][]
            {
                new byte[] {16},
                new byte[] {3},
                new byte[] {2},
                new byte[] {1},
                new byte[] {0x81},
                new byte[] {0x11, 0x12, 0x13, 0x14, 0x15},
                new byte[] {0x07, 0x08, 0x09},
                new byte[] {0x04, 0x05, 0x06},
                new byte[] {0x01, 0x02, 0x03},
                new byte[] {0xAA},
                new byte[] {}
            }));
        }
    }
}