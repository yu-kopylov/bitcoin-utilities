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
            processor.TransactionInputNumber = 0;

            processor.Execute(tx.Inputs[0].SignatureScript);
            processor.Execute(pubScript);

            Assert.True(processor.Valid);
            Assert.True(processor.Success);

            Tx corruptedTx = BitcoinStreamReader.FromBytes(txText, Tx.Read);
            corruptedTx.Inputs[0].SignatureScript[10] = 0xAA;

            processor.Reset();
            processor.Transaction = corruptedTx;
            processor.TransactionInputNumber = 0;

            processor.Execute(corruptedTx.Inputs[0].SignatureScript);
            processor.Execute(pubScript);

            Assert.True(processor.Valid);
            Assert.False(processor.Success);
        }

        [Test]
        public void TestCheckSigWithMultipleInputs()
        {
            // this is the transaction '3015186be9276f543d8e3e639c16d7627227307aadd2c588d2aa43c98ac2a1b3' from block 3309
            byte[] txText = new byte[]
            {
                0x01, 0x00, 0x00, 0x00, 0x03, 0x0D, 0xF4, 0x39, 0x38, 0xFA, 0x84, 0xD4, 0x40, 0xCE, 0xC1, 0xBF,
                0x0F, 0x89, 0x64, 0x4E, 0xB9, 0x3B, 0xB5, 0x88, 0x7B, 0xA4, 0x32, 0xB0, 0xF0, 0x26, 0xC3, 0x56,
                0xE1, 0x6F, 0xC2, 0x08, 0x89, 0x00, 0x00, 0x00, 0x00, 0x49, 0x48, 0x30, 0x45, 0x02, 0x21, 0x00,
                0xDC, 0x87, 0x0F, 0xD0, 0x01, 0xC3, 0x19, 0x2D, 0xBC, 0x14, 0xA0, 0xDD, 0xD9, 0x8C, 0xF2, 0x3A,
                0xA7, 0x60, 0xFF, 0x99, 0x23, 0xD1, 0x03, 0xC9, 0xC2, 0xFE, 0xAF, 0x8F, 0x2D, 0x00, 0xBE, 0xD6,
                0x02, 0x20, 0x49, 0x98, 0x97, 0xA1, 0x67, 0x72, 0x6D, 0x84, 0xC0, 0x64, 0xAC, 0xEB, 0x6F, 0x93,
                0x84, 0x64, 0x13, 0x68, 0xDF, 0x53, 0x68, 0x86, 0xDC, 0xBB, 0x81, 0x06, 0x12, 0x4D, 0xFD, 0xC8,
                0xCC, 0xDE, 0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0x08, 0x52, 0xED, 0xBD, 0xB2, 0x17, 0x69, 0xC6, 0x9F,
                0x41, 0x13, 0xC0, 0xBF, 0x8F, 0xD9, 0xD0, 0xD6, 0xB8, 0xDD, 0xFD, 0xD3, 0xFD, 0x03, 0xB3, 0x9A,
                0xCA, 0x63, 0x13, 0x0A, 0x94, 0x5F, 0x11, 0x00, 0x00, 0x00, 0x00, 0x4A, 0x49, 0x30, 0x46, 0x02,
                0x21, 0x00, 0xCA, 0x16, 0x10, 0xC6, 0xD3, 0x72, 0xE3, 0x74, 0x67, 0x67, 0x1B, 0xEB, 0xE7, 0x7B,
                0x72, 0x2F, 0xAC, 0x72, 0x04, 0x4A, 0xEF, 0x22, 0xCC, 0x32, 0x78, 0x8D, 0x39, 0x97, 0x26, 0xAA,
                0xFA, 0xA7, 0x02, 0x21, 0x00, 0xAF, 0x46, 0x77, 0xE2, 0xF2, 0x14, 0xEC, 0xEC, 0x85, 0xB2, 0x07,
                0xB9, 0x9B, 0xEE, 0x94, 0x17, 0x69, 0x7A, 0x66, 0xDE, 0xD3, 0xAA, 0x77, 0x5A, 0x99, 0xDF, 0x90,
                0x15, 0x76, 0x89, 0xC5, 0xF5, 0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0xF2, 0x50, 0x81, 0x33, 0x14, 0x11,
                0x5D, 0x7B, 0x50, 0xF8, 0x89, 0xCD, 0x64, 0xA7, 0x27, 0xB2, 0x3F, 0x13, 0x06, 0xF0, 0xAF, 0xD5,
                0x09, 0x77, 0x24, 0xB4, 0xC1, 0x01, 0x6B, 0x90, 0xE5, 0xCD, 0x00, 0x00, 0x00, 0x00, 0x49, 0x48,
                0x30, 0x45, 0x02, 0x20, 0x17, 0xDD, 0xFC, 0x6B, 0x14, 0x63, 0x11, 0xA5, 0x51, 0x0B, 0xF3, 0xA0,
                0x00, 0x08, 0xE2, 0x54, 0x27, 0x25, 0x4E, 0x54, 0x09, 0x46, 0x46, 0xE5, 0x54, 0x9A, 0x08, 0xB3,
                0xCB, 0xA5, 0xE8, 0x36, 0x02, 0x21, 0x00, 0xC6, 0x4A, 0x8A, 0x4A, 0x94, 0x37, 0x0A, 0x74, 0xEC,
                0x3D, 0x9D, 0xCD, 0xDD, 0x8D, 0x31, 0xDF, 0x37, 0x93, 0x4A, 0x92, 0x37, 0xE6, 0x40, 0x71, 0xC1,
                0x0A, 0xDC, 0xCA, 0xCB, 0xCB, 0x3E, 0xEA, 0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0x01, 0x00, 0xD6, 0x11,
                0x7E, 0x03, 0x00, 0x00, 0x00, 0x43, 0x41, 0x04, 0xE4, 0x07, 0x2D, 0x4A, 0x04, 0x88, 0x9E, 0xBF,
                0xDB, 0x48, 0x16, 0x5F, 0x7C, 0xB0, 0xAD, 0x5F, 0xD5, 0x86, 0x69, 0x09, 0x86, 0xC5, 0xB2, 0x29,
                0x74, 0x62, 0x95, 0x5F, 0x7D, 0x06, 0x50, 0xB9, 0x3A, 0xB6, 0x66, 0x38, 0x22, 0x92, 0x43, 0x8A,
                0xCC, 0x94, 0xE0, 0x75, 0x70, 0x19, 0x7D, 0xCB, 0xAE, 0xD0, 0xF4, 0x65, 0x2B, 0x17, 0xB8, 0x00,
                0xF8, 0xE2, 0x5E, 0xCE, 0x51, 0xE2, 0x43, 0xBB, 0xAC, 0x00, 0x00, 0x00, 0x00
            };

            Tx tx = BitcoinStreamReader.FromBytes(txText, Tx.Read);

            byte[][] pubScripts = new byte[][]
            {
                new byte[]
                {
                    0x41, 0x04, 0x41, 0xB1, 0x72, 0x55, 0x16, 0x61, 0xF2, 0xDA, 0x51, 0xD5, 0x59, 0x43, 0x54, 0x89,
                    0x4D, 0x07, 0x36, 0xD6, 0x96, 0xFF, 0xBB, 0x21, 0x2B, 0x4B, 0x4B, 0x8A, 0x96, 0x69, 0xFD, 0x24,
                    0x3E, 0x7C, 0x18, 0xDF, 0x99, 0x92, 0x35, 0x55, 0xF1, 0x2C, 0xC4, 0xFD, 0x96, 0xAC, 0x5E, 0x88,
                    0xCC, 0xD7, 0x10, 0x79, 0xF0, 0x67, 0x61, 0x23, 0xAA, 0x8D, 0xC9, 0xB0, 0x50, 0x86, 0xAF, 0x37,
                    0xF3, 0x14, 0xAC
                },
                new byte[]
                {
                    0x41, 0x04, 0x77, 0x2C, 0x3A, 0xD9, 0xAF, 0xD2, 0x0D, 0x65, 0xA7, 0xB0, 0x5C, 0x56, 0xE8, 0xEB,
                    0xD0, 0x02, 0xAB, 0xED, 0x58, 0xC4, 0x9E, 0xC5, 0x87, 0x4C, 0x6E, 0x6E, 0xEF, 0x4A, 0x29, 0x2F,
                    0x5D, 0xF1, 0x39, 0x98, 0xFC, 0x05, 0xB6, 0xB8, 0xDC, 0x63, 0x44, 0x7E, 0x02, 0x8D, 0xD4, 0xBB,
                    0xC1, 0xC0, 0x5B, 0x87, 0x2F, 0x85, 0x27, 0xD2, 0xB1, 0x05, 0x7E, 0x47, 0x83, 0x10, 0x28, 0x22,
                    0x3C, 0x3F, 0xAC
                },
                new byte[]
                {
                    0x41, 0x04, 0xCB, 0x41, 0xC4, 0x62, 0xF2, 0xE7, 0x31, 0x7E, 0x20, 0x9A, 0x9B, 0x83, 0x6C, 0xC4,
                    0xB1, 0x1D, 0x95, 0x9A, 0xDC, 0xA1, 0xFA, 0xA8, 0xD4, 0xB2, 0xE1, 0x03, 0x91, 0x98, 0xE1, 0xA6,
                    0x47, 0x26, 0x75, 0x03, 0x81, 0x63, 0xAD, 0xE2, 0x52, 0xEE, 0x3D, 0xB9, 0xDC, 0xBD, 0x1F, 0x43,
                    0x87, 0xF8, 0xE4, 0x4B, 0xCD, 0x97, 0xD5, 0xB1, 0xEF, 0x2A, 0x8F, 0x9C, 0x86, 0xA6, 0x02, 0xFF,
                    0x8B, 0x88, 0xAC
                }
            };

            ScriptProcessor processor = new ScriptProcessor();
            processor.Transaction = tx;

            for (int i = 0; i < tx.Inputs.Length; i++)
            {
                processor.Reset();
                processor.Transaction = tx;
                processor.TransactionInputNumber = i;

                processor.Execute(tx.Inputs[i].SignatureScript);
                processor.Execute(pubScripts[i]);

                Assert.True(processor.Valid, $"Input #{i}");
                Assert.True(processor.Success, $"Input #{i}");

                processor.Reset();
                processor.Transaction = tx;
                processor.TransactionInputNumber = (i + 1)%tx.Inputs.Length;

                processor.Execute(tx.Inputs[i].SignatureScript);
                processor.Execute(pubScripts[i]);

                Assert.True(processor.Valid, $"Input #{i}");
                Assert.False(processor.Success, $"Input #{i}");
            }
        }
    }
}