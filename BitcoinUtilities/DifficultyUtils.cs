using System;
using System.Numerics;

namespace BitcoinUtilities
{
    public static class DifficultyUtils
    {
        //todo: use network settings instead?
        public const int DifficultyAdjustmentIntervalInBlocks = 2016;
        public const int DifficultyAdjustmentIntervalInSeconds = 14*24*60*60;

        //todo: use network settings instead?
        public static readonly BigInteger MaxDifficultyTarget = NBitsToTarget(0x1D00FFFF);

        private static readonly BigInteger power256Of2 = BigInteger.Pow(2, 256);

        /// <summary>
        /// Converts a nBits to a difficulty target threshold.
        /// </summary>
        /// <remark>Specification: https://bitcoin.org/en/developer-reference#target-nbits </remark>
        /// <param name="nBits">The nBits to convert.</param>
        /// <returns>The difficulty target threshold as a <see cref="BigInteger"/>.</returns>
        public static BigInteger NBitsToTarget(uint nBits)
        {
            int exp = (int) (nBits >> 24) - 3;
            int mantissa = (int) (nBits & 0x007FFFFF);

            if ((nBits & 0x00800000) != 0)
            {
                mantissa = -mantissa;
            }

            BigInteger res;

            if (exp >= 0)
            {
                res = mantissa*BigInteger.Pow(256, exp);
            }
            else
            {
                res = mantissa/BigInteger.Pow(256, -exp);
            }

            return res;
        }

        /// <summary>
        /// Converts a difficulty target threshold to a nBits value.
        /// </summary>
        /// <remark>Specification: https://bitcoin.org/en/developer-reference#target-nbits </remark>
        /// <param name="target">The difficulty target threshold to convert.</param>
        /// <returns>The nBits value that can be stored in a block header.</returns>
        public static uint TargetToNBits(BigInteger target)
        {
            byte[] targetBytes = target.ToByteArray();

            int exp = targetBytes.Length;

            int mantissa = 0;
            for (int i = 0; i < 3 && i < targetBytes.Length; i++)
            {
                mantissa += targetBytes[targetBytes.Length - i - 1] << ((2 - i)*8);
            }

            return (uint) ((exp << 24) + mantissa);
        }

        /// <summary>
        /// Estimates the number of randomly generated 256-bit hashes that have
        /// approximately 1 hash that is less than or equal to the given difficulty target.
        /// <para/>
        /// If the target is negative then it is treated as a zero target.
        /// </summary>
        /// <param name="target">The difficulty target.</param>
        /// <returns>Estimated number of hashes.</returns>
        public static double DifficultyTargetToWork(BigInteger target)
        {
            //todo: compare with implementations in other Bitcoin projects

            if (target < 0)
            {
                return Math.Pow(2, 256);
            }

            if (target > power256Of2)
            {
                return 1;
            }

            BigInteger goodHashCount = target + 1;

            return Math.Pow(2, 256 - BigInteger.Log(goodHashCount, 2));
        }

        /// <summary>
        /// Calculates a new difficulty target based on an old target and a time spend on previous blocks generation.
        /// </summary>
        /// <param name="timeSpent">The time that was spent to generate blocks.</param>
        /// <param name="oldTarget">The previous difficulty target.</param>
        /// <returns>A new difficulty target.</returns>
        /// <remarks>Specification: https://en.bitcoin.it/wiki/Protocol_rules#Difficulty_change </remarks>
        public static BigInteger CalculateNewTarget(uint timeSpent, BigInteger oldTarget)
        {
            if (timeSpent < DifficultyAdjustmentIntervalInSeconds/4)
            {
                timeSpent = DifficultyAdjustmentIntervalInSeconds/4;
            }
            if (timeSpent > DifficultyAdjustmentIntervalInSeconds*4)
            {
                timeSpent = DifficultyAdjustmentIntervalInSeconds*4;
            }

            BigInteger newTarget = oldTarget*timeSpent/DifficultyAdjustmentIntervalInSeconds;

            // todo: use network settings
            // min difficulty is defined in https://bitcoin.org/en/developer-reference#target-nbits

            if (newTarget > MaxDifficultyTarget)
            {
                newTarget = MaxDifficultyTarget;
            }

            return newTarget;
        }
    }
}