﻿using System;
using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestBitcoinPrivateKey
    {
        [Test]
        public void TestIsValid()
        {
            //length check
            Assert.That(BitcoinPrivateKey.IsValid(null), Is.False);
            Assert.That(BitcoinPrivateKey.IsValid(new byte[0]), Is.False);
            Assert.That(BitcoinPrivateKey.IsValid(new byte[] {1}), Is.False);
            Assert.That(BitcoinPrivateKey.IsValid(new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01
            }), Is.False);
            Assert.That(BitcoinPrivateKey.IsValid(new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
                0x01
            }), Is.False);

            //lower boundary
            Assert.That(BitcoinPrivateKey.IsValid(new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01
            }), Is.True);
            Assert.That(BitcoinPrivateKey.IsValid(new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            }), Is.False);

            //upper boundary
            Assert.That(BitcoinPrivateKey.IsValid(new byte[]
            {
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFE,
                0xBA, 0xAE, 0xDC, 0xE6, 0xAF, 0x48, 0xA0, 0x3B, 0xBF, 0xD2, 0x5E, 0x8C, 0xD0, 0x36, 0x41, 0x40
            }), Is.True);
            Assert.That(BitcoinPrivateKey.IsValid(new byte[]
            {
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFE,
                0xBA, 0xAE, 0xDC, 0xE6, 0xAF, 0x48, 0xA0, 0x3B, 0xBF, 0xD2, 0x5E, 0x8C, 0xD0, 0x36, 0x41, 0x41
            }), Is.False);
        }

        [Test]
        public void TestIsStrong()
        {
            Assert.That(BitcoinPrivateKey.IsStrong(null), Is.False);

            Assert.That(BitcoinPrivateKey.IsStrong(new byte[0]), Is.False);

            // 0
            Assert.That(BitcoinPrivateKey.IsStrong(new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            }), Is.False);

            // 1
            Assert.That(BitcoinPrivateKey.IsStrong(new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01
            }), Is.False);

            // upper boundary
            Assert.That(BitcoinPrivateKey.IsStrong(new byte[]
            {
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFE,
                0xBA, 0xAE, 0xDC, 0xE6, 0xAF, 0x48, 0xA0, 0x3B, 0xBF, 0xD2, 0x5E, 0x8C, 0xD0, 0x36, 0x41, 0x40
            }), Is.False);

            // reference address example
            Assert.That(BitcoinPrivateKey.IsStrong(new byte[]
            {
                0x18, 0xE1, 0x4A, 0x7B, 0x6A, 0x30, 0x7F, 0x42, 0x6A, 0x94, 0xF8, 0x11, 0x47, 0x01, 0xE7, 0xC8,
                0xE7, 0x74, 0xE7, 0xF9, 0xA4, 0x7E, 0x2C, 0x20, 0x35, 0xDB, 0x29, 0xA2, 0x06, 0x32, 0x17, 0x25
            }), Is.True);

            // reference address example (without byte)
            Assert.That(BitcoinPrivateKey.IsStrong(new byte[]
            {
                0x18, 0xE1, 0x4A, 0x7B, 0x6A, 0x30, 0x7F, 0x42, 0x6A, 0x94, 0xF8, 0x11, 0x47, 0x01, 0xE7, 0xC8,
                0xE7, 0x74, 0xE7, 0xF9, 0xA4, 0x7E, 0x2C, 0x20, 0x35, 0xDB, 0x29, 0xA2, 0x06, 0x32, 0x17
            }), Is.False);

            // 1 / 12345
            Assert.That(BitcoinPrivateKey.IsStrong(new byte[]
            {
                0x94, 0xba, 0x0d, 0xba, 0x5d, 0x5b, 0xcd, 0xa6, 0x74, 0xff, 0xba, 0xfc, 0x9e, 0xae, 0x3b, 0xc2,
                0xf5, 0x47, 0x9f, 0xb5, 0xfc, 0x08, 0x59, 0x9b, 0x21, 0xfd, 0x01, 0x92, 0xd1, 0x82, 0xdf, 0x2d
            }), Is.False);

            // - 1 / 19
            Assert.That(BitcoinPrivateKey.IsStrong(new byte[]
            {
                0xa1, 0xaf, 0x28, 0x6b, 0xca, 0x1a, 0xf2, 0x86, 0xbc, 0xa1, 0xaf, 0x28, 0x6b, 0xca, 0x1a, 0xf1,
                0xb9, 0x46, 0x04, 0xc7, 0x97, 0x20, 0x65, 0x33, 0x35, 0xc8, 0x3b, 0xb7, 0x40, 0x22, 0x44, 0x29
            }), Is.False);

            // 32 different bytes (ordered)
            Assert.That(BitcoinPrivateKey.IsStrong(new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16,
                0x17, 0x18, 0x19, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30, 0x31, 0x32
            }), Is.False);

            // 32 different bytes (unordered)
            Assert.That(BitcoinPrivateKey.IsStrong(new byte[]
            {
                0x01, 0x32, 0x02, 0x31, 0x03, 0x30, 0x04, 0x29, 0x05, 0x28, 0x06, 0x27, 0x07, 0x26, 0x08, 0x25,
                0x09, 0x24, 0x10, 0x23, 0x11, 0x22, 0x12, 0x21, 0x13, 0x20, 0x14, 0x19, 0x15, 0x18, 0x16, 0x17
            }), Is.True);

            // 20 different bytes (unordered)
            Assert.That(BitcoinPrivateKey.IsStrong(new byte[]
            {
                0x01, 0x20, 0x02, 0x19, 0x03, 0x18, 0x04, 0x17, 0x05, 0x16, 0x06, 0x15, 0x07, 0x14, 0x08, 0x13,
                0x09, 0x12, 0x10, 0x11, 0x12, 0x01, 0x11, 0x02, 0x10, 0x03, 0x09, 0x04, 0x08, 0x05, 0x07, 0x06
            }), Is.False);
        }

        [Explicit]
        [Test]
        public void TestIsStrongProbability()
        {
            SecureRandom random = SecureRandom.Create();

            int totalCount = 100000;
            int validCount = 0;
            int strongCount = 0;

            for (int i = 0; i < totalCount; i++)
            {
                byte[] privateKey = random.NextBytes(32);

                bool valid = BitcoinPrivateKey.IsValid(privateKey);
                bool strong = BitcoinPrivateKey.IsStrong(privateKey);

                if (valid)
                {
                    validCount++;
                }

                if (strong)
                {
                    strongCount++;
                }
                else
                {
                    Console.WriteLine(BitConverter.ToString(privateKey));
                }
            }

            Console.WriteLine(
                "total: {0}, valid: {1}, strong: {2} (~{3:F2}%)",
                totalCount, validCount, strongCount, strongCount*100d/totalCount);
        }
    }
}