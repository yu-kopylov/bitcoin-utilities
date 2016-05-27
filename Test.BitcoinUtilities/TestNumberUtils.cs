using System;
using System.Numerics;
using BitcoinUtilities;
using BitcoinUtilities.P2P;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestNumberUtils
    {
        [Test]
        public void TestReverseBytesUShort()
        {
            Assert.That(NumberUtils.ReverseBytes((ushort) 0x1234), Is.EqualTo(0x3412));
            Assert.That(NumberUtils.ReverseBytes((ushort) 0xFF00), Is.EqualTo(0x00FF));
            Assert.That(NumberUtils.ReverseBytes((ushort) 0x00FF), Is.EqualTo(0xFF00));
        }

        [Test]
        public void TestReverseBytesUInt()
        {
            Assert.That(NumberUtils.ReverseBytes((uint) 0x12345678), Is.EqualTo(0x78563412));
            Assert.That(NumberUtils.ReverseBytes((uint) 0xFFFF0000), Is.EqualTo(0x0000FFFF));
            Assert.That(NumberUtils.ReverseBytes((uint) 0x0000FFFF), Is.EqualTo(0xFFFF0000));
        }

        [Test]
        public void TestGetBytes()
        {
            Assert.That(NumberUtils.GetBytes(0), Is.EqualTo(new byte[] {0, 0, 0, 0}));
            Assert.That(NumberUtils.GetBytes(1), Is.EqualTo(new byte[] {1, 0, 0, 0}));
            Assert.That(NumberUtils.GetBytes(-1), Is.EqualTo(new byte[] {0xFF, 0xFF, 0xFF, 0xFF}));
            Assert.That(NumberUtils.GetBytes(0x12345678), Is.EqualTo(new byte[] {0x78, 0x56, 0x34, 0x12}));
            Assert.That(
                NumberUtils.GetBytes(unchecked((int) 0x81828384)),
                Is.EqualTo(new byte[] {0x84, 0x83, 0x82, 0x81}));
        }

        [Test]
        public void TestToBigInteger()
        {
            Assert.That(NumberUtils.ToBigInteger(new byte[] {0}), Is.EqualTo(new BigInteger(0)));
            Assert.That(NumberUtils.ToBigInteger(new byte[] {1}), Is.EqualTo(new BigInteger(1)));
            Assert.That(NumberUtils.ToBigInteger(new byte[] {255}), Is.EqualTo(new BigInteger(255)));

            Assert.That(NumberUtils.ToBigInteger(new byte[] {1, 0}), Is.EqualTo(new BigInteger(1)));
            Assert.That(NumberUtils.ToBigInteger(new byte[] {0, 1}), Is.EqualTo(new BigInteger(256)));

            Assert.That(NumberUtils.ToBigInteger(new byte[] {0, 200}), Is.EqualTo(new BigInteger(51200)));
        }
    }
}