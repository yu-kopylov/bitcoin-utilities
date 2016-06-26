using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;
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

        [Test]
        public void TestCheckSig()
        {
            // this is the transaction '4b96658d39f7fd4241442e6dc9877c8ee0fe0d82477e6014e1681d1efe052c8d' from block 2754
            byte[] txText = new byte[]
            {
                0x01, 0x00, 0x00, 0x00, 0x01, 0xCD, 0x84, 0x21, 0x84, 0xFE, 0xBD, 0x96, 0xB4, 0x12, 0xA2, 0x22,
                0x73, 0x0F, 0x75, 0x46, 0x94, 0x20, 0x9A, 0x45, 0x98, 0x07, 0xAF, 0x89, 0x70, 0xAC, 0x19, 0xC2,
                0xDB, 0x0B, 0x80, 0x30, 0x00, 0x00, 0x00, 0x00, 0x00, 0x4A, 0x49, 0x30, 0x46, 0x02, 0x21, 0x00,
                0xB9, 0x7F, 0xD7, 0x62, 0x00, 0x74, 0xA8, 0xCF, 0x58, 0x52, 0x39, 0x5B, 0x68, 0xFE, 0xC6, 0x0C,
                0x8F, 0xAB, 0xD6, 0xEF, 0xBA, 0xF9, 0x08, 0xEB, 0x64, 0x59, 0x50, 0xE0, 0x8F, 0x78, 0x11, 0xFC,
                0x02, 0x21, 0x00, 0xB6, 0xDF, 0xD4, 0x7A, 0x03, 0x1D, 0xAA, 0xE2, 0x1A, 0x98, 0x5B, 0xB8, 0xFE,
                0x53, 0x77, 0x3D, 0x55, 0xB8, 0x3E, 0x3E, 0x44, 0x99, 0xF2, 0x20, 0x21, 0x03, 0xFC, 0x96, 0xDE,
                0xB6, 0xB9, 0xD7, 0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0x01, 0x00, 0xF2, 0x05, 0x2A, 0x01, 0x00, 0x00,
                0x00, 0x19, 0x76, 0xA9, 0x14, 0x54, 0xC2, 0x2D, 0xDF, 0xA2, 0x6C, 0x9F, 0xD0, 0x50, 0x41, 0x49,
                0x19, 0xDF, 0xF5, 0x32, 0x0C, 0x45, 0xA3, 0x3E, 0x58, 0x88, 0xAC, 0x00, 0x00, 0x00, 0x00
            };

            Tx tx = BitcoinStreamReader.FromBytes(txText, Tx.Read);
            byte[] pubScript = new byte[]
            {
                0x41, 0x04, 0x5E, 0x2F, 0xE4, 0xE8, 0xB4, 0x58, 0xA8, 0xFC, 0x3C, 0x75, 0xB7, 0x81, 0xBC, 0xFB,
                0xA7, 0xF1, 0x19, 0x39, 0x04, 0x90, 0xCB, 0x8E, 0xEC, 0xED, 0x31, 0x00, 0x8E, 0xE7, 0x6E, 0x43,
                0x18, 0xBE, 0x9E, 0x9D, 0x61, 0x9F, 0xB3, 0xF4, 0x19, 0xA2, 0xDB, 0x79, 0x96, 0xDB, 0xCB, 0xC1,
                0x56, 0x52, 0x11, 0xEC, 0x42, 0x6C, 0x7B, 0x44, 0x75, 0xFA, 0xA4, 0x0E, 0xCC, 0xA0, 0x32, 0x33,
                0x7F, 0xA5, 0xAC
            };

            ScriptProcessor processor = new ScriptProcessor();
            processor.Transaction = tx;

            processor.Execute(tx.Inputs[0].SignatureScript);
            processor.Execute(pubScript);

            Assert.That(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[] {1}}));

            Tx corruptedTx = BitcoinStreamReader.FromBytes(txText, Tx.Read);
            corruptedTx.Inputs[0].SignatureScript[10] = 0xAA;

            processor.Reset();
            processor.Transaction = corruptedTx;

            processor.Execute(corruptedTx.Inputs[0].SignatureScript);
            processor.Execute(pubScript);

            Assert.That(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[0]}));
        }
    }
}