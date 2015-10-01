using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestBip38
    {
        [Test]
        public void TestEncode()
        {
            string address = "16ktGzmfrurhbhi6JGqsMWf7TyqK9HNAeF";
            byte[] privateKey = new byte[]
                                {
                                    0x64, 0xEE, 0xAB, 0x5F, 0x9B, 0xE2, 0xA0, 0x1A, 0x83, 0x65, 0xA5, 0x79, 0x51, 0x1E, 0xB3, 0x37,
                                    0x3C, 0x87, 0xC4, 0x0D, 0xA6, 0xD2, 0xA2, 0x5F, 0x05, 0xBD, 0xA6, 0x8F, 0xE0, 0x77, 0xB6, 0x6E
                                };
            string password = "\u03D2\u0301\u0000\U00010400\U0001F4A9";
            string expectedEncryptedKey = "6PRW5o9FLp4gJDDVqJQKJFTpMvdsSGJxMYHtHaQBF3ooa8mwD69bapcDQn";

            string encryptedPrivateKey = Bip38.Encode(address, privateKey, password, false, false, false);
            Assert.That(encryptedPrivateKey, Is.EquivalentTo(expectedEncryptedKey));
        }
    }
}