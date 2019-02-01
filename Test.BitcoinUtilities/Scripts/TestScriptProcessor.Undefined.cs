using System.Linq;
using BitcoinUtilities.Scripts;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Scripts
{
    [TestFixture]
    public partial class TestScriptProcessor
    {
        [Test]
        public void TestUndefined_FailWhenPresent()
        {
            int first = BitcoinScript.OP_NOP10 + 1;
            int count = BitcoinScript.OP_PUBKEYHASH - first;

            byte[] commands = Enumerable.Range(first, count).Select(c => (byte) c).ToArray();

            Assert.That(commands.First(), Is.EqualTo(0xBA));
            Assert.That(commands.Last(), Is.EqualTo(0xFC));

            AssertFailWhenPresent(commands);
        }
    }
}