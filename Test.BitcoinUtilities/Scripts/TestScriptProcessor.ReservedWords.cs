using BitcoinUtilities.Scripts;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Scripts
{
    [TestFixture]
    public partial class TestScriptProcessor
    {
        [Test]
        public void TestNopCommands()
        {
            byte[] nopCommands = new byte[]
            {
                BitcoinScript.OP_NOP1,
                BitcoinScript.OP_NOP4,
                BitcoinScript.OP_NOP5,
                BitcoinScript.OP_NOP6,
                BitcoinScript.OP_NOP7,
                BitcoinScript.OP_NOP8,
                BitcoinScript.OP_NOP9,
                BitcoinScript.OP_NOP10
            };

            ScriptProcessor processor = new ScriptProcessor();

            foreach (byte nopCommand in nopCommands)
            {
                processor.Reset();

                processor.Execute(new byte[] {nopCommand});

                Assert.True(processor.Valid);
                Assert.That(processor.GetStack(), Is.Empty);
            }
        }
    }
}