using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Scripts;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Scripts
{
    [TestFixture]
    public class TestScriptParser
    {
        [Test]
        public void TestEmpty()
        {
            ScriptParser parser = new ScriptParser();
            List<ScriptCommand> commands;

            Assert.True(parser.TryParse(new byte[0], out commands));
            Assert.That(commands, Is.Empty);
        }

        [Test]
        public void TestSimple()
        {
            ScriptParser parser = new ScriptParser();
            List<ScriptCommand> commands;

            Assert.True(parser.TryParse(new byte[] {BitcoinScript.OP_TRUE, BitcoinScript.OP_VERIFY}, out commands));
            Assert.That(commands.Select(c => c.Code), Is.EqualTo(new byte[] {BitcoinScript.OP_TRUE, BitcoinScript.OP_VERIFY}));
            Assert.That(commands.Select(c => c.Offset), Is.EqualTo(new int[] {0, 1}));
            Assert.That(commands.Select(c => c.Length), Is.EqualTo(new int[] {1, 1}));
        }

        [Test]
        public void TestPushData()
        {
            ScriptParser parser = new ScriptParser();
            List<ScriptCommand> commands;
            byte[] script;

            script = new byte[] {BitcoinScript.OP_PUSHDATA_LEN_1};
            Assert.False(parser.TryParse(script, out commands));
            Assert.That(commands, Is.Null);

            script = new byte[] {BitcoinScript.OP_PUSHDATA_LEN_1, 1};
            Assert.True(parser.TryParse(script, out commands));
            Assert.That(commands.Select(c => c.Code), Is.EqualTo(new byte[] {BitcoinScript.OP_PUSHDATA_LEN_1}));
            Assert.That(commands.Select(c => c.Offset), Is.EqualTo(new int[] {0}));
            Assert.That(commands.Select(c => c.Length), Is.EqualTo(new int[] {2}));

            script = new byte[75];
            script[0] = BitcoinScript.OP_PUSHDATA_LEN_75;
            Assert.False(parser.TryParse(script, out commands));
            Assert.That(commands, Is.Null);

            script = new byte[76];
            script[0] = BitcoinScript.OP_PUSHDATA_LEN_75;
            Assert.True(parser.TryParse(script, out commands));
            Assert.That(commands.Select(c => c.Code), Is.EqualTo(new byte[] {BitcoinScript.OP_PUSHDATA_LEN_75}));
            Assert.That(commands.Select(c => c.Offset), Is.EqualTo(new int[] {0}));
            Assert.That(commands.Select(c => c.Length), Is.EqualTo(new int[] {76}));

            script = new byte[11];
            script[0] = BitcoinScript.OP_TRUE;
            script[1] = BitcoinScript.OP_PUSHDATA_LEN_1 + 9;
            Assert.False(parser.TryParse(script, out commands));
            Assert.That(commands, Is.Null);

            script = new byte[12];
            script[0] = BitcoinScript.OP_TRUE;
            script[1] = BitcoinScript.OP_PUSHDATA_LEN_1 + 9;
            Assert.True(parser.TryParse(script, out commands));
            Assert.That(commands.Select(c => c.Code), Is.EqualTo(new byte[] {BitcoinScript.OP_TRUE, BitcoinScript.OP_PUSHDATA_LEN_1 + 9}));
            Assert.That(commands.Select(c => c.Offset), Is.EqualTo(new int[] {0, 1}));
            Assert.That(commands.Select(c => c.Length), Is.EqualTo(new int[] {1, 11}));
        }

        [Test]
        public void TestPushData1()
        {
            ScriptParser parser = new ScriptParser();
            List<ScriptCommand> commands;
            byte[] script;

            script = new byte[] {BitcoinScript.OP_PUSHDATA1};
            Assert.False(parser.TryParse(script, out commands));
            Assert.That(commands, Is.Null);

            script = new byte[] {BitcoinScript.OP_PUSHDATA1, 0};
            Assert.True(parser.TryParse(script, out commands));
            Assert.That(commands.Select(c => c.Code), Is.EqualTo(new byte[] {BitcoinScript.OP_PUSHDATA1}));
            Assert.That(commands.Select(c => c.Offset), Is.EqualTo(new int[] {0}));
            Assert.That(commands.Select(c => c.Length), Is.EqualTo(new int[] {2}));

            script = new byte[] {BitcoinScript.OP_PUSHDATA1, 1};
            Assert.False(parser.TryParse(script, out commands));
            Assert.That(commands, Is.Null);

            script = new byte[] {BitcoinScript.OP_PUSHDATA1, 1, 1};
            Assert.True(parser.TryParse(script, out commands));
            Assert.That(commands.Select(c => c.Code), Is.EqualTo(new byte[] {BitcoinScript.OP_PUSHDATA1}));
            Assert.That(commands.Select(c => c.Offset), Is.EqualTo(new int[] {0}));
            Assert.That(commands.Select(c => c.Length), Is.EqualTo(new int[] {3}));

            script = new byte[256];
            script[0] = BitcoinScript.OP_PUSHDATA1;
            script[1] = 255;
            Assert.False(parser.TryParse(script, out commands));
            Assert.That(commands, Is.Null);

            script = new byte[257];
            script[0] = BitcoinScript.OP_PUSHDATA1;
            script[1] = 255;
            Assert.True(parser.TryParse(script, out commands));
            Assert.That(commands.Select(c => c.Code), Is.EqualTo(new byte[] {BitcoinScript.OP_PUSHDATA1}));
            Assert.That(commands.Select(c => c.Offset), Is.EqualTo(new int[] {0}));
            Assert.That(commands.Select(c => c.Length), Is.EqualTo(new int[] {257}));

            script = new byte[12];
            script[0] = BitcoinScript.OP_TRUE;
            script[1] = BitcoinScript.OP_PUSHDATA1;
            script[2] = 10;
            Assert.False(parser.TryParse(script, out commands));
            Assert.That(commands, Is.Null);

            script = new byte[13];
            script[0] = BitcoinScript.OP_TRUE;
            script[1] = BitcoinScript.OP_PUSHDATA1;
            script[2] = 10;
            Assert.True(parser.TryParse(script, out commands));
            Assert.That(commands.Select(c => c.Code), Is.EqualTo(new byte[] {BitcoinScript.OP_TRUE, BitcoinScript.OP_PUSHDATA1}));
            Assert.That(commands.Select(c => c.Offset), Is.EqualTo(new int[] {0, 1}));
            Assert.That(commands.Select(c => c.Length), Is.EqualTo(new int[] {1, 12}));
        }

        [Test]
        public void TestPushData2()
        {
            ScriptParser parser = new ScriptParser();
            List<ScriptCommand> commands;
            byte[] script;

            script = new byte[] {BitcoinScript.OP_PUSHDATA2, 0};
            Assert.False(parser.TryParse(script, out commands));
            Assert.That(commands, Is.Null);

            script = new byte[] {BitcoinScript.OP_PUSHDATA2, 0, 0};
            Assert.True(parser.TryParse(script, out commands));
            Assert.That(commands.Select(c => c.Code), Is.EqualTo(new byte[] {BitcoinScript.OP_PUSHDATA2}));
            Assert.That(commands.Select(c => c.Offset), Is.EqualTo(new int[] {0}));
            Assert.That(commands.Select(c => c.Length), Is.EqualTo(new int[] {3}));

            script = new byte[] {BitcoinScript.OP_PUSHDATA2, 0, 1};
            Assert.False(parser.TryParse(script, out commands));
            Assert.That(commands, Is.Null);

            script = new byte[] {BitcoinScript.OP_PUSHDATA2, 0, 1, 1};
            Assert.True(parser.TryParse(script, out commands));
            Assert.That(commands.Select(c => c.Code), Is.EqualTo(new byte[] {BitcoinScript.OP_PUSHDATA2}));
            Assert.That(commands.Select(c => c.Offset), Is.EqualTo(new int[] {0}));
            Assert.That(commands.Select(c => c.Length), Is.EqualTo(new int[] {4}));

            script = new byte[0xFF82];
            script[0] = BitcoinScript.OP_PUSHDATA2;
            script[1] = 0xFF;
            script[2] = 0x80;
            Assert.False(parser.TryParse(script, out commands));
            Assert.That(commands, Is.Null);

            script = new byte[0xFF83];
            script[0] = BitcoinScript.OP_PUSHDATA2;
            script[1] = 0xFF;
            script[2] = 0x80;
            Assert.True(parser.TryParse(script, out commands));
            Assert.That(commands.Select(c => c.Code), Is.EqualTo(new byte[] {BitcoinScript.OP_PUSHDATA2}));
            Assert.That(commands.Select(c => c.Offset), Is.EqualTo(new int[] {0}));
            Assert.That(commands.Select(c => c.Length), Is.EqualTo(new int[] {0xFF83}));

            script = new byte[13];
            script[0] = BitcoinScript.OP_TRUE;
            script[1] = BitcoinScript.OP_PUSHDATA2;
            script[2] = 0;
            script[3] = 10;
            Assert.False(parser.TryParse(script, out commands));
            Assert.That(commands, Is.Null);

            script = new byte[14];
            script[0] = BitcoinScript.OP_TRUE;
            script[1] = BitcoinScript.OP_PUSHDATA2;
            script[2] = 0;
            script[3] = 10;
            Assert.True(parser.TryParse(script, out commands));
            Assert.That(commands.Select(c => c.Code), Is.EqualTo(new byte[] {BitcoinScript.OP_TRUE, BitcoinScript.OP_PUSHDATA2}));
            Assert.That(commands.Select(c => c.Offset), Is.EqualTo(new int[] {0, 1}));
            Assert.That(commands.Select(c => c.Length), Is.EqualTo(new int[] {1, 13}));
        }

        [Test]
        public void TestPushData4()
        {
            ScriptParser parser = new ScriptParser();
            List<ScriptCommand> commands;
            byte[] script;

            script = new byte[] {BitcoinScript.OP_PUSHDATA4, 0, 0, 0};
            Assert.False(parser.TryParse(script, out commands));
            Assert.That(commands, Is.Null);

            script = new byte[] {BitcoinScript.OP_PUSHDATA4, 0, 0, 0, 0};
            Assert.True(parser.TryParse(script, out commands));
            Assert.That(commands.Select(c => c.Code), Is.EqualTo(new byte[] {BitcoinScript.OP_PUSHDATA4}));
            Assert.That(commands.Select(c => c.Offset), Is.EqualTo(new int[] {0}));
            Assert.That(commands.Select(c => c.Length), Is.EqualTo(new int[] {5}));

            script = new byte[] {BitcoinScript.OP_PUSHDATA4, 0, 0, 0, 1};
            Assert.False(parser.TryParse(script, out commands));
            Assert.That(commands, Is.Null);

            script = new byte[] {BitcoinScript.OP_PUSHDATA4, 0, 0, 0, 1, 1};
            Assert.True(parser.TryParse(script, out commands));
            Assert.That(commands.Select(c => c.Code), Is.EqualTo(new byte[] {BitcoinScript.OP_PUSHDATA4}));
            Assert.That(commands.Select(c => c.Offset), Is.EqualTo(new int[] {0}));
            Assert.That(commands.Select(c => c.Length), Is.EqualTo(new int[] {6}));

            script = new byte[0x01020384];
            script[0] = BitcoinScript.OP_PUSHDATA4;
            script[1] = 0x01;
            script[2] = 0x02;
            script[3] = 0x03;
            script[4] = 0x80;
            Assert.False(parser.TryParse(script, out commands));
            Assert.That(commands, Is.Null);

            script = new byte[0x01020385];
            script[0] = BitcoinScript.OP_PUSHDATA4;
            script[1] = 0x01;
            script[2] = 0x02;
            script[3] = 0x03;
            script[4] = 0x80;
            Assert.True(parser.TryParse(script, out commands));
            Assert.That(commands.Select(c => c.Code), Is.EqualTo(new byte[] {BitcoinScript.OP_PUSHDATA4}));
            Assert.That(commands.Select(c => c.Offset), Is.EqualTo(new int[] {0}));
            Assert.That(commands.Select(c => c.Length), Is.EqualTo(new int[] {0x01020385}));

            script = new byte[15];
            script[0] = BitcoinScript.OP_TRUE;
            script[1] = BitcoinScript.OP_PUSHDATA4;
            script[2] = 0;
            script[3] = 0;
            script[4] = 0;
            script[5] = 10;
            Assert.False(parser.TryParse(script, out commands));
            Assert.That(commands, Is.Null);

            script = new byte[16];
            script[0] = BitcoinScript.OP_TRUE;
            script[1] = BitcoinScript.OP_PUSHDATA4;
            script[2] = 0;
            script[3] = 0;
            script[4] = 0;
            script[5] = 10;
            Assert.True(parser.TryParse(script, out commands));
            Assert.That(commands.Select(c => c.Code), Is.EqualTo(new byte[] {BitcoinScript.OP_TRUE, BitcoinScript.OP_PUSHDATA4}));
            Assert.That(commands.Select(c => c.Offset), Is.EqualTo(new int[] {0, 1}));
            Assert.That(commands.Select(c => c.Length), Is.EqualTo(new int[] {1, 15}));

            script = new byte[0x1000];
            script[0] = BitcoinScript.OP_PUSHDATA4;
            script[1] = 0xFF;
            script[2] = 0xFF;
            script[3] = 0xFF;
            script[4] = 0xFF;
            Assert.False(parser.TryParse(script, out commands));
            Assert.That(commands, Is.Null);
        }
    }
}