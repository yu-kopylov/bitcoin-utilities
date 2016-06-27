using BitcoinUtilities.Scripts;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Scripts
{
    [TestFixture]
    public partial class TestScriptProcessor
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
    }
}