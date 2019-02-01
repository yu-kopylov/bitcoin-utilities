using BitcoinUtilities.Scripts;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Scripts
{
    [TestFixture]
    public partial class TestScriptProcessor
    {
        [Test]
        public void TestPseudoWords_FailWhenPresent()
        {
            AssertFailWhenPresent(
                BitcoinScript.OP_PUBKEYHASH,
                BitcoinScript.OP_PUBKEY,
                BitcoinScript.OP_INVALIDOPCODE
            );
        }
    }
}