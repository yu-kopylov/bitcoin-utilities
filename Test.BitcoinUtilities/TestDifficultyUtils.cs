using System;
using System.Numerics;
using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestDifficultyUtils
    {
        [Test]
        public void TestNBitsToTarget()
        {
            Assert.That(DifficultyUtils.NBitsToTarget(0x01003456), Is.EqualTo(new BigInteger(0)));
            Assert.That(DifficultyUtils.NBitsToTarget(0x05009234), Is.EqualTo(new BigInteger(0x92340000)));

            Assert.That(DifficultyUtils.NBitsToTarget(0x01003456), Is.EqualTo(new BigInteger(0x00)));
            Assert.That(DifficultyUtils.NBitsToTarget(0x01123456), Is.EqualTo(new BigInteger(0x12)));
            Assert.That(DifficultyUtils.NBitsToTarget(0x02008000), Is.EqualTo(new BigInteger(0x80)));
            Assert.That(DifficultyUtils.NBitsToTarget(0x05009234), Is.EqualTo(new BigInteger(0x92340000)));


            Assert.That(DifficultyUtils.NBitsToTarget(0x207FFFFF), Is.EqualTo(new BigInteger(new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x7F
            })));

            // High bit set(0x80 in 0x92).
            Assert.That(DifficultyUtils.NBitsToTarget(0x04923456), Is.EqualTo(new BigInteger(-0x12345600)));

            // Inverse of above; no high bit.
            Assert.That(DifficultyUtils.NBitsToTarget(0x04123456), Is.EqualTo(new BigInteger(0x12345600)));
        }

        [Test]
        public void TestTargetToNBits()
        {
            //todo: compare this results with Bitcoin Core implementation
            Assert.That(DifficultyUtils.TargetToNBits(0), Is.EqualTo(0x01000000));
            Assert.That(DifficultyUtils.TargetToNBits(1), Is.EqualTo(0x01010000));
            Assert.That(DifficultyUtils.TargetToNBits(0x12345678), Is.EqualTo(0x04123456));
            Assert.That(DifficultyUtils.TargetToNBits(0x81234567), Is.EqualTo(0x05008123));

            Assert.That(DifficultyUtils.NBitsToTarget(0x01000000), Is.EqualTo(new BigInteger(0)));
            Assert.That(DifficultyUtils.NBitsToTarget(0x01010000), Is.EqualTo(new BigInteger(1)));
            Assert.That(DifficultyUtils.NBitsToTarget(0x04123456), Is.EqualTo(new BigInteger(0x12345600)));
            Assert.That(DifficultyUtils.NBitsToTarget(0x05008123), Is.EqualTo(new BigInteger(0x81230000)));
        }

        [Test]
        public void TestDifficultyTargetToWork()
        {
            Assert.That(DifficultyUtils.DifficultyTargetToWork(-BigInteger.Pow(2, 1024)), Is.EqualTo(Math.Pow(2, 256)));
            Assert.That(DifficultyUtils.DifficultyTargetToWork(-10), Is.EqualTo(Math.Pow(2, 256)));
            Assert.That(DifficultyUtils.DifficultyTargetToWork(-1), Is.EqualTo(Math.Pow(2, 256)));
            Assert.That(DifficultyUtils.DifficultyTargetToWork(0), Is.EqualTo(Math.Pow(2, 256)));
            Assert.That(DifficultyUtils.DifficultyTargetToWork(1), Is.EqualTo(Math.Pow(2, 255)));
            Assert.That(DifficultyUtils.DifficultyTargetToWork(2), Is.EqualTo(Math.Pow(2, 256)/3).Within(1e-12).Percent);
            Assert.That(DifficultyUtils.DifficultyTargetToWork(3), Is.EqualTo(Math.Pow(2, 254)));
            Assert.That(DifficultyUtils.DifficultyTargetToWork(BigInteger.Pow(2, 254)), Is.EqualTo(4));
            Assert.That(DifficultyUtils.DifficultyTargetToWork(BigInteger.Pow(2, 256)/3), Is.EqualTo(3).Within(1e-12).Percent);
            Assert.That(DifficultyUtils.DifficultyTargetToWork(BigInteger.Pow(2, 255)), Is.EqualTo(2));
            Assert.That(DifficultyUtils.DifficultyTargetToWork(BigInteger.Pow(2, 256)), Is.EqualTo(1));
            Assert.That(DifficultyUtils.DifficultyTargetToWork(BigInteger.Pow(2, 257)), Is.EqualTo(1));
            Assert.That(DifficultyUtils.DifficultyTargetToWork(BigInteger.Pow(2, 1024)), Is.EqualTo(1));
        }

        [Test]
        public void TestCalculateNewTarget()
        {
            const int difficultyAdjustmentIntervalInSeconds = DifficultyUtils.DifficultyAdjustmentIntervalInSeconds;

            Assert.That(DifficultyUtils.CalculateNewTarget(difficultyAdjustmentIntervalInSeconds, 1000000), Is.EqualTo(new BigInteger(1000000)));

            Assert.That(DifficultyUtils.CalculateNewTarget(difficultyAdjustmentIntervalInSeconds/2, 1000000), Is.EqualTo(new BigInteger(500000)));
            Assert.That(DifficultyUtils.CalculateNewTarget(difficultyAdjustmentIntervalInSeconds/4, 1000000), Is.EqualTo(new BigInteger(250000)));
            Assert.That(DifficultyUtils.CalculateNewTarget(difficultyAdjustmentIntervalInSeconds/8, 1000000), Is.EqualTo(new BigInteger(250000)));

            Assert.That(DifficultyUtils.CalculateNewTarget(difficultyAdjustmentIntervalInSeconds*2, 1000000), Is.EqualTo(new BigInteger(2000000)));
            Assert.That(DifficultyUtils.CalculateNewTarget(difficultyAdjustmentIntervalInSeconds*4, 1000000), Is.EqualTo(new BigInteger(4000000)));
            Assert.That(DifficultyUtils.CalculateNewTarget(difficultyAdjustmentIntervalInSeconds*8, 1000000), Is.EqualTo(new BigInteger(4000000)));

            BigInteger maxTarget = DifficultyUtils.MaxDifficultyTarget;
            Assert.That(DifficultyUtils.CalculateNewTarget(difficultyAdjustmentIntervalInSeconds, maxTarget), Is.EqualTo(maxTarget));
            Assert.That(DifficultyUtils.CalculateNewTarget(difficultyAdjustmentIntervalInSeconds*2, maxTarget), Is.EqualTo(maxTarget));
        }
    }
}