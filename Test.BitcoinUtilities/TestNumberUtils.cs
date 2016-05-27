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

        [Test]
        public void TestNBitsToTarget()
        {
            Assert.That(NumberUtils.NBitsToTarget(0x01003456), Is.EqualTo(new BigInteger(0)));
            Assert.That(NumberUtils.NBitsToTarget(0x05009234), Is.EqualTo(new BigInteger(0x92340000)));

            Assert.That(NumberUtils.NBitsToTarget(0x01003456), Is.EqualTo(new BigInteger(0x00)));
            Assert.That(NumberUtils.NBitsToTarget(0x01123456), Is.EqualTo(new BigInteger(0x12)));
            Assert.That(NumberUtils.NBitsToTarget(0x02008000), Is.EqualTo(new BigInteger(0x80)));
            Assert.That(NumberUtils.NBitsToTarget(0x05009234), Is.EqualTo(new BigInteger(0x92340000)));


            Assert.That(NumberUtils.NBitsToTarget(0x207FFFFF), Is.EqualTo(new BigInteger(new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x7F
            })));

            // High bit set(0x80 in 0x92).
            Assert.That(NumberUtils.NBitsToTarget(0x04923456), Is.EqualTo(new BigInteger(-0x12345600)));

            // Inverse of above; no high bit.
            Assert.That(NumberUtils.NBitsToTarget(0x04123456), Is.EqualTo(new BigInteger(0x12345600)));
        }

        [Test]
        public void TestTargetToNBits()
        {
            //todo: compare this results with Bitcoin Core implementation
            Assert.That(NumberUtils.TargetToNBits(0), Is.EqualTo(0x01000000));
            Assert.That(NumberUtils.TargetToNBits(1), Is.EqualTo(0x01010000));
            Assert.That(NumberUtils.TargetToNBits(0x12345678), Is.EqualTo(0x04123456));
            Assert.That(NumberUtils.TargetToNBits(0x81234567), Is.EqualTo(0x05008123));

            Assert.That(NumberUtils.NBitsToTarget(0x01000000), Is.EqualTo(new BigInteger(0)));
            Assert.That(NumberUtils.NBitsToTarget(0x01010000), Is.EqualTo(new BigInteger(1)));
            Assert.That(NumberUtils.NBitsToTarget(0x04123456), Is.EqualTo(new BigInteger(0x12345600)));
            Assert.That(NumberUtils.NBitsToTarget(0x05008123), Is.EqualTo(new BigInteger(0x81230000)));
        }

        [Test]
        public void TestDifficultyTargetToWork()
        {
            Assert.That(NumberUtils.DifficultyTargetToWork(-BigInteger.Pow(2, 1024)), Is.EqualTo(Math.Pow(2, 256)));
            Assert.That(NumberUtils.DifficultyTargetToWork(-10), Is.EqualTo(Math.Pow(2, 256)));
            Assert.That(NumberUtils.DifficultyTargetToWork(-1), Is.EqualTo(Math.Pow(2, 256)));
            Assert.That(NumberUtils.DifficultyTargetToWork(0), Is.EqualTo(Math.Pow(2, 256)));
            Assert.That(NumberUtils.DifficultyTargetToWork(1), Is.EqualTo(Math.Pow(2, 255)));
            Assert.That(NumberUtils.DifficultyTargetToWork(2), Is.EqualTo(Math.Pow(2, 256)/3).Within(1e-12).Percent);
            Assert.That(NumberUtils.DifficultyTargetToWork(3), Is.EqualTo(Math.Pow(2, 254)));
            Assert.That(NumberUtils.DifficultyTargetToWork(BigInteger.Pow(2, 254)), Is.EqualTo(4));
            Assert.That(NumberUtils.DifficultyTargetToWork(BigInteger.Pow(2, 256)/3), Is.EqualTo(3).Within(1e-12).Percent);
            Assert.That(NumberUtils.DifficultyTargetToWork(BigInteger.Pow(2, 255)), Is.EqualTo(2));
            Assert.That(NumberUtils.DifficultyTargetToWork(BigInteger.Pow(2, 256)), Is.EqualTo(1));
            Assert.That(NumberUtils.DifficultyTargetToWork(BigInteger.Pow(2, 257)), Is.EqualTo(1));
            Assert.That(NumberUtils.DifficultyTargetToWork(BigInteger.Pow(2, 1024)), Is.EqualTo(1));
        }
    }
}