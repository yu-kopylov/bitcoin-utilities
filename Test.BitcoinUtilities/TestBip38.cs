using System;
using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestBip38
    {
        [Test]
        public void TestNoCompressionNoEcMultiply()
        {
            TestEncryptDecrypt(new byte[]
                               {
                                   0xCB, 0xF4, 0xB9, 0xF7, 0x04, 0x70, 0x85, 0x6B, 0xB4, 0xF4, 0x0F, 0x80, 0xB8, 0x7E, 0xDB, 0x90,
                                   0x86, 0x59, 0x97, 0xFF, 0xEE, 0x6D, 0xF3, 0x15, 0xAB, 0x16, 0x6D, 0x71, 0x3A, 0xF4, 0x33, 0xA5
                               }, "TestingOneTwoThree", false, "6PRVWUbkzzsbcVac2qwfssoUJAN1Xhrg6bNk8J7Nzm5H7kxEbn2Nh2ZoGg");
            TestEncryptDecrypt(new byte[]
                               {
                                   0x09, 0xC2, 0x68, 0x68, 0x80, 0x09, 0x5B, 0x1A, 0x4C, 0x24, 0x9E, 0xE3, 0xAC, 0x4E, 0xEA, 0x8A,
                                   0x01, 0x4F, 0x11, 0xE6, 0xF9, 0x86, 0xD0, 0xB5, 0x02, 0x5A, 0xC1, 0xF3, 0x9A, 0xFB, 0xD9, 0xAE
                               }, "Satoshi", false, "6PRNFFkZc2NZ6dJqFfhRoFNMR9Lnyj7dYGrzdgXXVMXcxoKTePPX1dWByq");
            TestEncryptDecrypt(new byte[]
                               {
                                   0x64, 0xEE, 0xAB, 0x5F, 0x9B, 0xE2, 0xA0, 0x1A, 0x83, 0x65, 0xA5, 0x79, 0x51, 0x1E, 0xB3, 0x37,
                                   0x3C, 0x87, 0xC4, 0x0D, 0xA6, 0xD2, 0xA2, 0x5F, 0x05, 0xBD, 0xA6, 0x8F, 0xE0, 0x77, 0xB6, 0x6E
                               }, "\u03D2\u0301\u0000\U00010400\U0001F4A9", false, "6PRW5o9FLp4gJDDVqJQKJFTpMvdsSGJxMYHtHaQBF3ooa8mwD69bapcDQn");
        }

        [Test]
        public void TestCompressionNoEcMultiply()
        {
            TestEncryptDecrypt(new byte[]
                               {
                                   0xCB, 0xF4, 0xB9, 0xF7, 0x04, 0x70, 0x85, 0x6B, 0xB4, 0xF4, 0x0F, 0x80, 0xB8, 0x7E, 0xDB, 0x90,
                                   0x86, 0x59, 0x97, 0xFF, 0xEE, 0x6D, 0xF3, 0x15, 0xAB, 0x16, 0x6D, 0x71, 0x3A, 0xF4, 0x33, 0xA5
                               }, "TestingOneTwoThree", true, "6PYNKZ1EAgYgmQfmNVamxyXVWHzK5s6DGhwP4J5o44cvXdoY7sRzhtpUeo");
            TestEncryptDecrypt(new byte[]
                               {
                                   0x09, 0xC2, 0x68, 0x68, 0x80, 0x09, 0x5B, 0x1A, 0x4C, 0x24, 0x9E, 0xE3, 0xAC, 0x4E, 0xEA, 0x8A,
                                   0x01, 0x4F, 0x11, 0xE6, 0xF9, 0x86, 0xD0, 0xB5, 0x02, 0x5A, 0xC1, 0xF3, 0x9A, 0xFB, 0xD9, 0xAE
                               }, "Satoshi", true, "6PYLtMnXvfG3oJde97zRyLYFZCYizPU5T3LwgdYJz1fRhh16bU7u6PPmY7");
        }

        [Test]
        public void TestEncryptValidation()
        {
            //invalid input private key
            Assert.Throws<ArgumentException>(() => Bip38.Encrypt(null, "test", false));
            Assert.Throws<ArgumentException>(() => Bip38.Encrypt(new byte[] {1}, "test", false));

            //invalid password
            Assert.Throws<ArgumentException>(() => Bip38.Encrypt(new byte[]
                                                                 {
                                                                     0xCB, 0xF4, 0xB9, 0xF7, 0x04, 0x70, 0x85, 0x6B, 0xB4, 0xF4, 0x0F, 0x80, 0xB8, 0x7E, 0xDB, 0x90,
                                                                     0x86, 0x59, 0x97, 0xFF, 0xEE, 0x6D, 0xF3, 0x15, 0xAB, 0x16, 0x6D, 0x71, 0x3A, 0xF4, 0x33, 0xA5
                                                                 }, null, false));
        }

        [Test]
        public void TestDecryptValidation()
        {
            byte[] privateKey;
            bool useCompressedPublicKey;

            //invalid encrypted key
            Assert.That(Bip38.TryDecrypt(null, "test", out privateKey, out useCompressedPublicKey), Is.False);
            Assert.That(Bip38.TryDecrypt("1111", "test", out privateKey, out useCompressedPublicKey), Is.False);

            //invalid password
            Assert.Throws<ArgumentException>(() => Bip38.TryDecrypt("6PYNKZ1EAgYgmQfmNVamxyXVWHzK5s6DGhwP4J5o44cvXdoY7sRzhtpUeo", null, out privateKey, out useCompressedPublicKey));

            //invalid private key
            Assert.That(Bip38.TryDecrypt("6PRWPsu2b2SG8t4TwvriL3GdTUkHtmJz7j3yuuba5NnbaTdpMcSx5C5Dr2", "test", out privateKey, out useCompressedPublicKey), Is.False);

            //corrupted hash
            Assert.That(Bip38.TryDecrypt("6PYNKZf6c6v11hV7uh3FMd6tYunsJYoShheWUPktXRsDDsW345WjefLxh7", "TestingOneTwoThree", out privateKey, out useCompressedPublicKey), Is.False);
        }

        private void TestEncryptDecrypt(byte[] privateKey, string password, bool useCompressedPublicKey, string encryptedKey)
        {
            string calculatedEncryptedKey = Bip38.Encrypt(privateKey, password, useCompressedPublicKey);
            Assert.That(calculatedEncryptedKey, Is.EquivalentTo(encryptedKey));

            Assert.That(Bip38.TryDecrypt(encryptedKey, password, out var calculatedPrivateKey, out var calculatedUseCompressedPublicKey), Is.True);
            Assert.That(calculatedPrivateKey, Is.EqualTo(privateKey));
            Assert.That(calculatedUseCompressedPublicKey, Is.EqualTo(useCompressedPublicKey));
        }
    }
}