using BitcoinUtilities.Scripts;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Scripts
{
    [TestFixture]
    public partial class TestScriptProcessor
    {
        [Test]
        public void TestBitwiseLogic_FailWhenPresent()
        {
            AssertFailWhenPresent
            (
                BitcoinScript.OP_INVERT,
                BitcoinScript.OP_AND,
                BitcoinScript.OP_OR,
                BitcoinScript.OP_XOR
            );
        }

        [Test]
        public void TestEqual()
        {
            ScriptProcessor processor = new ScriptProcessor();

            processor.Execute(new byte[]
            {
                BitcoinScript.OP_2,
                BitcoinScript.OP_EQUAL,
            });

            Assert.False(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[] {2}}));

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_2,
                BitcoinScript.OP_2,
                BitcoinScript.OP_EQUAL,
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[] {1}}));

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_2,
                BitcoinScript.OP_3,
                BitcoinScript.OP_EQUAL,
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[0]}));

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA_LEN_1 + 1, 0, 2,
                BitcoinScript.OP_2,
                BitcoinScript.OP_EQUAL,
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[0]}));

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA_LEN_1 + 1, 2, 0,
                BitcoinScript.OP_2,
                BitcoinScript.OP_EQUAL,
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[0]}));
        }

        [Test]
        public void TestEqualVerify()
        {
            ScriptProcessor processor = new ScriptProcessor();

            processor.Execute(new byte[]
            {
                BitcoinScript.OP_2,
                BitcoinScript.OP_EQUALVERIFY,
            });

            Assert.False(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[] {2}}));

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_2,
                BitcoinScript.OP_2,
                BitcoinScript.OP_EQUALVERIFY,
            });

            Assert.True(processor.Valid);
            Assert.That(processor.GetStack(), Is.Empty);

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_2,
                BitcoinScript.OP_3,
                BitcoinScript.OP_EQUALVERIFY,
            });

            Assert.False(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[0]}));

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA_LEN_1 + 1, 0, 2,
                BitcoinScript.OP_2,
                BitcoinScript.OP_EQUALVERIFY,
            });

            Assert.False(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[0]}));

            processor.Reset();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA_LEN_1 + 1, 2, 0,
                BitcoinScript.OP_2,
                BitcoinScript.OP_EQUALVERIFY,
            });

            Assert.False(processor.Valid);
            Assert.That(processor.GetStack(), Is.EqualTo(new byte[][] {new byte[0]}));
        }
    }
}