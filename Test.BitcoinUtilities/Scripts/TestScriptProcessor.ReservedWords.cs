using BitcoinUtilities.Scripts;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Scripts
{
    [TestFixture]
    public partial class TestScriptProcessor
    {
        [Test]
        public void TestReservedCommands_Nop()
        {
            byte[] commands = new byte[]
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

            foreach (byte command in commands)
            {
                processor.Reset();
                processor.Execute(new byte[] {command});

                Assert.True(processor.Valid);
                Assert.That(processor.GetStack(), Is.Empty);
            }
        }

        [Test]
        public void TestReservedCommands_FailWhenExecuted()
        {
            byte[] commands = new byte[]
            {
                BitcoinScript.OP_RESERVED,
                BitcoinScript.OP_VER,
                BitcoinScript.OP_RESERVED1,
                BitcoinScript.OP_RESERVED2
            };

            ScriptProcessor processor = new ScriptProcessor();

            foreach (byte command in commands)
            {
                processor.Reset();
                processor.Execute(new byte[] {command});

                Assert.False(processor.Valid);
                Assert.That(processor.GetStack(), Is.Empty);

                processor.Reset();
                processor.Execute(new byte[]
                {
                    BitcoinScript.OP_FALSE,
                    BitcoinScript.OP_IF,
                    command,
                    BitcoinScript.OP_ENDIF
                });

                Assert.True(processor.Valid);
                Assert.That(processor.GetStack(), Is.Empty);
            }
        }

        [Test]
        public void TestReservedCommands_FailWhenPresent()
        {
            byte[] commands = new byte[]
            {
                BitcoinScript.OP_VERIF,
                BitcoinScript.OP_VERNOTIF
            };

            ScriptProcessor processor = new ScriptProcessor();

            foreach (byte command in commands)
            {
                processor.Reset();
                processor.Execute(new byte[]
                {
                    BitcoinScript.OP_FALSE,
                    BitcoinScript.OP_IF,
                    command,
                    BitcoinScript.OP_ENDIF
                });

                Assert.False(processor.Valid);
                Assert.That(processor.GetStack(), Is.Empty);
            }
        }
    }
}