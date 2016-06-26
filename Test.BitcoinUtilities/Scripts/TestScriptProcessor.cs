using BitcoinUtilities.Scripts;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Scripts
{
    [TestFixture]
    public class TestScriptProcessor
    {
        [Test]
        public void TestIncompleteScript()
        {
            ScriptProcessor processor = new ScriptProcessor();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA_LEN_1
            });
            Assert.False(processor.Valid);
            Assert.That(processor.GetStack(), Is.Empty);
        }

        [Test]
        public void TestContants()
        {
            ScriptProcessor processor = new ScriptProcessor();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_FALSE,
                BitcoinScript.OP_PUSHDATA_LEN_1, 0xAA,
                BitcoinScript.OP_PUSHDATA_LEN_1 + 2, 0x01, 0x02, 0x03,
                BitcoinScript.OP_PUSHDATA1, 0x03, 0x04, 0x05, 0x06,
                BitcoinScript.OP_PUSHDATA2, 0x00, 0x03, 0x07, 0x08, 0x09,
                BitcoinScript.OP_PUSHDATA4, 0x00, 0x00, 0x00, 0x05, 0x11, 0x12, 0x13, 0x14, 0x15,
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

        [Test]
        public void TestIf()
        {
            ScriptProcessor processor = new ScriptProcessor();

            processor.Execute(new byte[]
            {
                BitcoinScript.OP_TRUE,
                BitcoinScript.OP_IF,
                BitcoinScript.OP_2,
                BitcoinScript.OP_ENDIF
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[] {2}}));

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_1NEGATE,
                BitcoinScript.OP_IF,
                BitcoinScript.OP_2,
                BitcoinScript.OP_ENDIF
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[] {2}}));

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA_LEN_1 + 2, 0, 1, 0x80,
                BitcoinScript.OP_IF,
                BitcoinScript.OP_2,
                BitcoinScript.OP_ENDIF
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[] {2}}));

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_FALSE,
                BitcoinScript.OP_IF,
                BitcoinScript.OP_2,
                BitcoinScript.OP_ENDIF
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack(), Is.Empty);

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA_LEN_1 + 2, 0, 0, 0,
                BitcoinScript.OP_IF,
                BitcoinScript.OP_2,
                BitcoinScript.OP_ENDIF
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack(), Is.Empty);

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA_LEN_1 + 2, 0, 0, 0x80,
                BitcoinScript.OP_IF,
                BitcoinScript.OP_2,
                BitcoinScript.OP_ENDIF
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack(), Is.Empty);
        }

        [Test]
        public void TestIfElse()
        {
            ScriptProcessor processor = new ScriptProcessor();

            processor.Execute(new byte[]
            {
                BitcoinScript.OP_TRUE,
                BitcoinScript.OP_IF,
                BitcoinScript.OP_2,
                BitcoinScript.OP_ELSE,
                BitcoinScript.OP_3,
                BitcoinScript.OP_ENDIF
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[] {2}}));

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_FALSE,
                BitcoinScript.OP_IF,
                BitcoinScript.OP_2,
                BitcoinScript.OP_ELSE,
                BitcoinScript.OP_3,
                BitcoinScript.OP_ENDIF
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[] {3}}));
        }

        [Test]
        public void TestElseWithoutIf()
        {
            ScriptProcessor processor = new ScriptProcessor();

            processor.Execute(new byte[]
            {
                BitcoinScript.OP_ELSE
            });

            Assert.False(processor.Valid);
            Assert.That(processor.GetStack(), Is.Empty);

            processor.Execute(new byte[]
            {
                BitcoinScript.OP_ELSE,
                BitcoinScript.OP_ENDIF
            });

            Assert.False(processor.Valid);
            Assert.That(processor.GetStack(), Is.Empty);
        }

        [Test]
        public void TestNotIf()
        {
            ScriptProcessor processor = new ScriptProcessor();

            processor.Execute(new byte[]
            {
                BitcoinScript.OP_FALSE,
                BitcoinScript.OP_NOTIF,
                BitcoinScript.OP_2,
                BitcoinScript.OP_ENDIF
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[] {2}}));

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_TRUE,
                BitcoinScript.OP_NOTIF,
                BitcoinScript.OP_2,
                BitcoinScript.OP_ENDIF
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack(), Is.Empty);
        }

        [Test]
        public void TestIfWithinIf()
        {
            ScriptProcessor processor = new ScriptProcessor();

            processor.Execute(new byte[]
            {
                BitcoinScript.OP_FALSE,
                BitcoinScript.OP_TRUE,
                BitcoinScript.OP_IF,
                BitcoinScript.OP_IF,
                BitcoinScript.OP_2,
                BitcoinScript.OP_ELSE,
                BitcoinScript.OP_3,
                BitcoinScript.OP_ENDIF,
                BitcoinScript.OP_ENDIF
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[] {3}}));

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_FALSE,
                BitcoinScript.OP_IF,
                BitcoinScript.OP_TRUE,
                BitcoinScript.OP_IF,
                BitcoinScript.OP_2,
                BitcoinScript.OP_ELSE,
                BitcoinScript.OP_3,
                BitcoinScript.OP_ENDIF,
                BitcoinScript.OP_ENDIF
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack(), Is.Empty);

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_TRUE,
                BitcoinScript.OP_TRUE,
                BitcoinScript.OP_IF,
                BitcoinScript.OP_IF,
                BitcoinScript.OP_2,
                BitcoinScript.OP_ELSE,
                BitcoinScript.OP_3,
                BitcoinScript.OP_ENDIF,
                BitcoinScript.OP_ENDIF
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[] {2}}));
        }

        [Test]
        public void TestIfWithoutCondition()
        {
            ScriptProcessor processor = new ScriptProcessor();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_IF,
                BitcoinScript.OP_ENDIF
            });
            Assert.False(processor.Valid);
            Assert.That(processor.GetStack(), Is.Empty);
        }

        [Test]
        public void TestIfWithoutEndIf()
        {
            ScriptProcessor processor = new ScriptProcessor();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_TRUE,
                BitcoinScript.OP_IF
            });
            Assert.False(processor.Valid);
            Assert.That(processor.GetStack(), Is.Empty);
        }

        [Test]
        public void TestEndIfWithoutIf()
        {
            ScriptProcessor processor = new ScriptProcessor();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_ENDIF
            });
            Assert.False(processor.Valid);
            Assert.That(processor.GetStack(), Is.Empty);
        }

        [Test]
        public void TestVerify()
        {
            ScriptProcessor processor = new ScriptProcessor();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_VERIFY
            });
            Assert.False(processor.Valid);
            Assert.That(processor.GetStack(), Is.Empty);

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_FALSE,
                BitcoinScript.OP_VERIFY
            });
            Assert.False(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[0]}));

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_TRUE,
                BitcoinScript.OP_VERIFY
            });
            Assert.True(processor.Valid);
            Assert.That(processor.GetStack(), Is.Empty);

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA_LEN_1 + 2, 0, 1, 0,
                BitcoinScript.OP_VERIFY
            });
            Assert.True(processor.Valid);
            Assert.That(processor.GetStack(), Is.Empty);
        }

        [Test]
        public void TestReturn()
        {
            ScriptProcessor processor = new ScriptProcessor();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_RETURN
            });
            Assert.False(processor.Valid);
            Assert.That(processor.GetStack(), Is.Empty);

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_FALSE,
                BitcoinScript.OP_RETURN
            });
            Assert.False(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[0]}));

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_TRUE,
                BitcoinScript.OP_RETURN
            });
            Assert.False(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[] {1}}));
        }
    }
}