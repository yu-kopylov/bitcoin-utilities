using BitcoinUtilities.Scripts;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Scripts
{
    [TestFixture]
    public partial class TestScriptProcessor
    {
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

            // Bitcoin Core allows multiple OP_ELSE for a single OP_IF
            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_TRUE,
                BitcoinScript.OP_IF,
                BitcoinScript.OP_2,
                BitcoinScript.OP_ELSE,
                BitcoinScript.OP_3,
                BitcoinScript.OP_ELSE,
                BitcoinScript.OP_4,
                BitcoinScript.OP_ELSE,
                BitcoinScript.OP_5,
                BitcoinScript.OP_ENDIF
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][]
            {
                new byte[] {4},
                new byte[] {2}
            }));
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