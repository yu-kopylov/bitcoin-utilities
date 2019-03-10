using BitcoinUtilities;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P.Messages
{
    [TestFixture]
    public class TestFilterLoadMessage
    {
        [Test]
        public void TestRead()
        {
            // example was taken from: https://github.com/bitcoin/bitcoin/blob/master/src/test/bloom_tests.cpp

            FilterLoadMessage message = BitcoinStreamReader.FromBytes(HexUtils.GetBytesUnsafe("03ce4299050000000100008001"), FilterLoadMessage.Read);

            Assert.That(HexUtils.GetString(message.Filter), Is.EqualTo("CE4299").IgnoreCase);
            Assert.That(message.FunctionCount, Is.EqualTo(5));
            Assert.That(message.Tweak, Is.EqualTo(0x80000001));
            Assert.That(message.Flags, Is.EqualTo(0x01));
        }

        [Test]
        public void TestWrite()
        {
            // example was taken from: https://github.com/bitcoin/bitcoin/blob/master/src/test/bloom_tests.cpp

            FilterLoadMessage message = new FilterLoadMessage(new byte[] {0xCE, 0x42, 0x99}, 5, 0x80000001, 0x01);
            Assert.That(HexUtils.GetString(BitcoinStreamWriter.GetBytes(message.Write)), Is.EqualTo("03ce4299050000000100008001").IgnoreCase);
        }
    }
}