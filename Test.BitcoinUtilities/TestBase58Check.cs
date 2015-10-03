using System;
using System.Numerics;
using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestBase58Check
    {
        [Test]
        public void TestEncodeNoCheck()
        {
            Assert.That(Base58Check.EncodeNoCheck(new byte[] {}), Is.EqualTo(""));
            Assert.That(Base58Check.EncodeNoCheck(new byte[] {0x00}), Is.EqualTo("1"));
            Assert.That(Base58Check.EncodeNoCheck(new byte[] {0x00, 0x00}), Is.EqualTo("11"));
            Assert.That(Base58Check.EncodeNoCheck(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00}), Is.EqualTo("11111"));
            Assert.That(Base58Check.EncodeNoCheck(new byte[] {0x01, 0xFF}), Is.EqualTo("9p"));
            Assert.That(Base58Check.EncodeNoCheck(new byte[] {0xFF, 0x01}), Is.EqualTo("LQY"));
            Assert.That(Base58Check.EncodeNoCheck(new byte[] {0x00, 0x00, 0x00, 0x01, 0xFF}), Is.EqualTo("1119p"));
            Assert.That(Base58Check.EncodeNoCheck(new byte[] {0x00, 0x00, 0x00, 0xFF, 0x01}), Is.EqualTo("111LQY"));
        }

        [Test]
        public void TestEncodeNoCheck58Pow()
        {
            for (int i = 1; i < 128; i++)
            {
                string str = "2".PadRight(i + 1, '1');
                BigInteger val = BigInteger.Pow(58, i);
                Assert.That(Base58Check.EncodeNoCheck(ToByteArray(val)), Is.EqualTo(str), "i = " + i);
            }
        }

        [Test]
        public void TestEncodeNoCheck58PowMinus1()
        {
            for (int i = 1; i < 128; i++)
            {
                string str = "".PadRight(i, 'z');
                BigInteger val = BigInteger.Pow(58, i) - 1;
                Assert.That(Base58Check.EncodeNoCheck(ToByteArray(val)), Is.EqualTo(str), "i = " + i);
            }
        }

        [Test]
        public void TestEncodeNoCheck58PowPlus1()
        {
            for (int i = 1; i < 128; i++)
            {
                string str = "2".PadRight(i, '1') + "2";
                BigInteger val = BigInteger.Pow(58, i) + 1;
                Assert.That(Base58Check.EncodeNoCheck(ToByteArray(val)), Is.EqualTo(str), "i = " + i);
            }
        }

        private static byte[] ToByteArray(BigInteger value)
        {
            byte[] bytes = value.ToByteArray();
            Array.Reverse(bytes);
            int firstByte = 0;
            while (bytes[firstByte] == 0)
            {
                firstByte++;
            }
            byte[] truncatedBytes = new byte[bytes.Length - firstByte];
            Array.Copy(bytes, firstByte, truncatedBytes, 0, bytes.Length - firstByte);
            return truncatedBytes;
        }

        [Test]
        public void TestEncode()
        {
            Assert.That(Base58Check.Encode(new byte[] {}), Is.EqualTo("3QJmnh"));
            Assert.That(Base58Check.Encode(new byte[] {0x00}), Is.EqualTo("1Wh4bh"));
            Assert.That(Base58Check.Encode(new byte[] {0x00, 0x00}), Is.EqualTo("112edB6q"));
            Assert.That(Base58Check.Encode(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00}), Is.EqualTo("1111146Q4wc"));
            Assert.That(Base58Check.Encode(new byte[] {0x01, 0xFF}), Is.EqualTo("zmHSYkM"));
            Assert.That(Base58Check.Encode(new byte[] {0xFF, 0x01}), Is.EqualTo("3Bz9GmeHm"));

            Assert.That(Base58Check.Encode(
                new byte[]
                {
                    0x80, 0x64, 0xEE, 0xAB, 0x5F, 0x9B, 0xE2, 0xA0, 0x1A, 0x83, 0x65, 0xA5, 0x79, 0x51, 0x1E, 0xB3, 0x37,
                    0x3C, 0x87, 0xC4, 0x0D, 0xA6, 0xD2, 0xA2, 0x5F, 0x05, 0xBD, 0xA6, 0x8F, 0xE0, 0x77, 0xB6, 0x6E
                }), Is.EqualTo("5Jajm8eQ22H3pGWLEVCXyvND8dQZhiQhoLJNKjYXk9roUFTMSZ4"));

            Assert.That(Base58Check.Encode(
                new byte[]
                {
                    0x01, 0x42, 0xC0, 0xF4, 0xE7, 0x75, 0xA8, 0x40, 0x20, 0xB1, 0xF0, 0xCA, 0x28, 0x53, 0x28, 0x7A, 0xCA, 0xC1, 0x1A, 0x43,
                    0x03, 0x2E, 0x27, 0x4E, 0x54, 0x36, 0x92, 0xBE, 0x5F, 0xA2, 0xB8, 0x54, 0x32, 0x67, 0x15, 0x43, 0xC1, 0xAC, 0x0E
                }), Is.EqualTo("6PRW5o9FLp4gJDDVqJQKJFTpMvdsSGJxMYHtHaQBF3ooa8mwD69bapcDQn"));
        }

        [Test]
        public void TestEncodePerformance()
        {
            byte[] privateKey = new byte[33];
            privateKey[0] = 0x80;
            privateKey[1] = 0x7F;
            for (int i = 0; i < 10000; i++)
            {
                privateKey[2] = (byte) i;
                privateKey[3] = (byte) (i/256);
                Base58Check.Encode(privateKey);
            }
        }
    }
}