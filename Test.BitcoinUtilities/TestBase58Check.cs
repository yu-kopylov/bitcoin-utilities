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
        public void TestEncodeDecodeNoCheck()
        {
            TestEncodeDecodeNoCheck(new byte[] {}, "");
            TestEncodeDecodeNoCheck(new byte[] {0x00}, "1");
            TestEncodeDecodeNoCheck(new byte[] {0x00, 0x00}, "11");
            TestEncodeDecodeNoCheck(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00}, "11111");
            TestEncodeDecodeNoCheck(new byte[] {0x01, 0xFF}, "9p");
            TestEncodeDecodeNoCheck(new byte[] {0xFF, 0x01}, "LQY");
            TestEncodeDecodeNoCheck(new byte[] {0x00, 0x00, 0x00, 0x01, 0xFF}, "1119p");
            TestEncodeDecodeNoCheck(new byte[] {0x00, 0x00, 0x00, 0xFF, 0x01}, "111LQY");
        }

        [Test]
        public void TestEncodeDecodeNoCheck58Pow()
        {
            for (int i = 1; i < 128; i++)
            {
                string str = "2".PadRight(i + 1, '1');
                BigInteger val = BigInteger.Pow(58, i);
                byte[] bytes = ToByteArray(val);
                TestEncodeDecodeNoCheck(bytes, str, "i = " + i);
            }
        }

        [Test]
        public void TestEncodeDecodeNoCheck58PowMinus1()
        {
            for (int i = 1; i < 128; i++)
            {
                string str = "".PadRight(i, 'z');
                BigInteger val = BigInteger.Pow(58, i) - 1;
                byte[] bytes = ToByteArray(val);
                TestEncodeDecodeNoCheck(bytes, str, "i = " + i);
            }
        }

        [Test]
        public void TestEncodeDecodeNoCheck58PowPlus1()
        {
            for (int i = 1; i < 128; i++)
            {
                string str = "2".PadRight(i, '1') + "2";
                BigInteger val = BigInteger.Pow(58, i) + 1;
                byte[] bytes = ToByteArray(val);
                TestEncodeDecodeNoCheck(bytes, str, "i = " + i);
            }
        }

        [Test]
        public void TestDecodeNoCheckValidation()
        {
            byte[] bytes;
            Assert.That(Base58Check.TryDecodeNoCheck("", out bytes), Is.True);
            Assert.That(Base58Check.TryDecodeNoCheck("*", out bytes), Is.False);
            Assert.That(Base58Check.TryDecodeNoCheck("\u0000", out bytes), Is.False);
            Assert.That(Base58Check.TryDecodeNoCheck("\u7FFF", out bytes), Is.False);
            Assert.That(Base58Check.TryDecodeNoCheck("\u8000", out bytes), Is.False);
            Assert.That(Base58Check.TryDecodeNoCheck("\uFFFF", out bytes), Is.False);

            Assert.That(Base58Check.TryDecodeNoCheck("\u0041", out bytes), Is.True);
            Assert.That(Base58Check.TryDecodeNoCheck("\u0141", out bytes), Is.False);
            Assert.That(Base58Check.TryDecodeNoCheck("\u4100", out bytes), Is.False);
            Assert.That(Base58Check.TryDecodeNoCheck("\u4141", out bytes), Is.False);
            Assert.That(Base58Check.TryDecodeNoCheck("\u7F41", out bytes), Is.False);
            Assert.That(Base58Check.TryDecodeNoCheck("\u8041", out bytes), Is.False);
            Assert.That(Base58Check.TryDecodeNoCheck("\u7F41", out bytes), Is.False);
            Assert.That(Base58Check.TryDecodeNoCheck("\uFF41", out bytes), Is.False);

            Assert.That(Base58Check.TryDecodeNoCheck("11", out bytes), Is.True);
            Assert.That(Base58Check.TryDecodeNoCheck("*11", out bytes), Is.False);
            Assert.That(Base58Check.TryDecodeNoCheck("1*1", out bytes), Is.False);
            Assert.That(Base58Check.TryDecodeNoCheck("11*", out bytes), Is.False);

            Assert.That(Base58Check.TryDecodeNoCheck("AA", out bytes), Is.True);
            Assert.That(Base58Check.TryDecodeNoCheck("*AA", out bytes), Is.False);
            Assert.That(Base58Check.TryDecodeNoCheck("A*A", out bytes), Is.False);
            Assert.That(Base58Check.TryDecodeNoCheck("AA*", out bytes), Is.False);
        }

        private void TestEncodeDecodeNoCheck(byte[] bytes, string str, string label = null)
        {
            Assert.That(Base58Check.EncodeNoCheck(bytes), Is.EqualTo(str), "(encode) " + label);

            byte[] decodedBytes;
            Assert.That(Base58Check.TryDecodeNoCheck(str, out decodedBytes), Is.True, "(decode) " + label);
            Assert.That(decodedBytes, Is.EqualTo(bytes), "(decode) " + label);
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
        public void TestEncodeDecode()
        {
            TestEncodeDecode(new byte[] {}, "3QJmnh");
            TestEncodeDecode(new byte[] {0x00}, "1Wh4bh");
            TestEncodeDecode(new byte[] {0x00, 0x00}, "112edB6q");
            TestEncodeDecode(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00}, "1111146Q4wc");
            TestEncodeDecode(new byte[] {0x01, 0xFF}, "zmHSYkM");
            TestEncodeDecode(new byte[] {0xFF, 0x01}, "3Bz9GmeHm");

            TestEncodeDecode(
                new byte[]
                {
                    0x80, 0x64, 0xEE, 0xAB, 0x5F, 0x9B, 0xE2, 0xA0, 0x1A, 0x83, 0x65, 0xA5, 0x79, 0x51, 0x1E, 0xB3, 0x37,
                    0x3C, 0x87, 0xC4, 0x0D, 0xA6, 0xD2, 0xA2, 0x5F, 0x05, 0xBD, 0xA6, 0x8F, 0xE0, 0x77, 0xB6, 0x6E
                }, "5Jajm8eQ22H3pGWLEVCXyvND8dQZhiQhoLJNKjYXk9roUFTMSZ4");

            TestEncodeDecode(
                new byte[]
                {
                    0x01, 0x42, 0xC0, 0xF4, 0xE7, 0x75, 0xA8, 0x40, 0x20, 0xB1, 0xF0, 0xCA, 0x28, 0x53, 0x28, 0x7A, 0xCA, 0xC1, 0x1A, 0x43,
                    0x03, 0x2E, 0x27, 0x4E, 0x54, 0x36, 0x92, 0xBE, 0x5F, 0xA2, 0xB8, 0x54, 0x32, 0x67, 0x15, 0x43, 0xC1, 0xAC, 0x0E
                }, "6PRW5o9FLp4gJDDVqJQKJFTpMvdsSGJxMYHtHaQBF3ooa8mwD69bapcDQn");
        }

        [Test]
        public void TestDecodeValidation()
        {
            byte[] bytes;
            Assert.That(Base58Check.TryDecode("", out bytes), Is.False);
            Assert.That(Base58Check.TryDecode("1", out bytes), Is.False);
            Assert.That(Base58Check.TryDecode("1111", out bytes), Is.False);

            Assert.That(Base58Check.TryDecode("1Wh4bh", out bytes), Is.True);
            Assert.That(Base58Check.TryDecode("2Wh4bh", out bytes), Is.False);
            Assert.That(Base58Check.TryDecode("1Wh5bh", out bytes), Is.False);
            Assert.That(Base58Check.TryDecode("1Wh4bg", out bytes), Is.False);
        }

        private void TestEncodeDecode(byte[] bytes, string str)
        {
            Assert.That(Base58Check.Encode(bytes), Is.EqualTo(str), "(encode)");

            byte[] decodedBytes;
            Assert.That(Base58Check.TryDecode(str, out decodedBytes), Is.True, "(decode)");
            Assert.That(decodedBytes, Is.EqualTo(bytes), "(decode)");
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

        [Test]
        public void TestDecodePerformance()
        {
            const string str = "5Jajm8eQ22H3pGWLEVCXyvND8dQZhiQhoLJNKjYXk9roUFTMSZ4";
            for (int i = 0; i < 10000; i++)
            {
                byte[] bytes;
                Base58Check.TryDecode(str, out bytes);
            }
        }
    }
}