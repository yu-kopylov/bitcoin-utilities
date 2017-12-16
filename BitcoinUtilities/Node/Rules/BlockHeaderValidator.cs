using System;
using System.Collections.Generic;
using System.Numerics;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Storage;

namespace BitcoinUtilities.Node.Rules
{
    public static class BlockHeaderValidator
    {
        //todo: split BlockHeaderValidator to static and non-static parts

        public static bool IsValid(BlockHeader header)
        {
            return
                IsNBitsValid(header) &&
                IsTimestampValid(header) &&
                IsHashValid(header);
        }

        /// <summary>
        /// Checks that timestamp of the block header is not too far in the future.
        /// </summary>
        /// <remarks>Specification: https://en.bitcoin.it/wiki/Block_timestamp </remarks>
        internal static bool IsTimestampValid(BlockHeader header)
        {
            //todo: Specification requires to adjust current time. Why is it necessary?
            return UnixTime.ToDateTime(header.Timestamp) < SystemTime.UtcNow.AddHours(2);
        }

        /// <summary>
        /// Checks that the difficulty defined by the nBits field is not lower than the network minimum.
        /// </summary>
        internal static bool IsNBitsValid(BlockHeader blockHeader)
        {
            BigInteger target = DifficultyUtils.NBitsToTarget(blockHeader.NBits);
            return target <= DifficultyUtils.MaxDifficultyTarget;
        }

        /// <summary>
        /// Checks that a hash of the header matches nBits field in the block.
        /// </summary>
        internal static bool IsHashValid(BlockHeader header)
        {
            byte[] text = BitcoinStreamWriter.GetBytes(header.Write);
            //todo: This hash is calculated too often. Save it somewhere.
            byte[] hash = CryptoUtils.DoubleSha256(text);

            BigInteger hashAsNumber = NumberUtils.ToBigInteger(hash);
            BigInteger difficultyTarget = DifficultyUtils.NBitsToTarget(header.NBits);

            if (hashAsNumber > difficultyTarget)
            {
                return false;
            }

            return true;
        }

        public static bool IsValid(BitcoinFork fork, StoredBlock block, Subchain parentChain)
        {
            return IsTimeStampValid(block, parentChain) && IsNBitsValid(fork, block, parentChain);
        }

        /// <summary>
        /// Checks that timestamp of the block is after median time of the last 11 blocks.
        /// </summary>
        internal static bool IsTimeStampValid(StoredBlock block, Subchain parentChain)
        {
            //todo: use network settings
            const int medianBlockCount = 11;

            List<uint> oldTimestamps = new List<uint>(medianBlockCount);

            for (int i = 0; i < medianBlockCount && i < parentChain.Length; i++)
            {
                oldTimestamps.Add(parentChain.GetBlockByOffset(i).Header.Timestamp);
            }

            oldTimestamps.Sort();

            uint medianTimestamp = oldTimestamps[oldTimestamps.Count / 2];

            return block.Header.Timestamp > medianTimestamp;
        }

        /// <summary>
        /// Checks that the nBits field of a block follow protocol rules.
        /// </summary>
        internal static bool IsNBitsValid(BitcoinFork fork, StoredBlock block, Subchain parentChain)
        {
            uint expectedNBits = CalculateNextNBits(fork, parentChain);
            return expectedNBits == block.Header.NBits;
        }

        /// <summary>
        /// Calculates new NBits for a block that will be added after the given chain.
        /// </summary>
        private static uint CalculateNextNBits(BitcoinFork fork, Subchain parentChain)
        {
            if (fork == BitcoinFork.Core)
            {
                return CalculateNextNBitsCoreOriginal(parentChain);
            }

            if (fork == BitcoinFork.Cash)
            {
                uint timestamp = GetMedianTimePast(parentChain, parentChain.GetBlockByOffset(0).Height);
                if (timestamp >= 1510600000)
                {
                    return CalculateNextNBitsCash20171113(parentChain);
                }
                // In the past such delays were happening only in the beginning, so it should be fine to apply new algorithm after for all blocks after year 2017.
                if (timestamp >= 1483228800)
                {
                    return CalculateNextNBitsCashOriginal(parentChain);
                }
                return CalculateNextNBitsCoreOriginal(parentChain);
            }
            throw new InvalidOperationException($"Difficulty adjustment algorithm is not defined for fork {fork} at height {parentChain.GetBlockByOffset(0).Height}.");
        }

        /// <summary>
        /// Calculates new NBits for a block that will be added after the given chain using Bitcoin Core rules.
        /// </summary>
        private static uint CalculateNextNBitsCoreOriginal(Subchain parentChain)
        {
            //todo: add test for this method

            int blockHeight = parentChain.GetBlockByOffset(0).Height + 1;

            if (blockHeight % DifficultyUtils.DifficultyAdjustmentIntervalInBlocks != 0)
            {
                return parentChain.GetBlockByOffset(0).Header.NBits;
            }

            StoredBlock baseBlock = parentChain.GetBlockByHeight(blockHeight - DifficultyUtils.DifficultyAdjustmentIntervalInBlocks);
            StoredBlock prevBlock = parentChain.GetBlockByOffset(0);

            // Note: There is a known bug here. It known as "off-by-one" or "Time Warp Bug".
            // Bug Description: http://bitcoin.stackexchange.com/questions/20597/where-exactly-is-the-off-by-one-difficulty-bug
            // Related Attack Description: https://bitcointalk.org/index.php?topic=43692.msg521772#msg521772
            uint timeSpent = prevBlock.Header.Timestamp - baseBlock.Header.Timestamp;

            BigInteger oldTarget = DifficultyUtils.NBitsToTarget(prevBlock.Header.NBits);

            var newTarget = DifficultyUtils.CalculateNewTarget(timeSpent, oldTarget);

            return DifficultyUtils.TargetToNBits(newTarget);
        }

        /// <summary>
        /// Calculates new NBits for a block that will be added after the given chain using original Bitcoin Cash rules.
        /// <para/>
        /// 12 hours adjustment algorithm: https://github.com/Bitcoin-ABC/bitcoin-abc/blob/master/src/pow.cpp
        /// </summary>
        private static uint CalculateNextNBitsCashOriginal(Subchain parentChain)
        {
            //todo: add test for this method

            int blockHeight = parentChain.GetBlockByOffset(0).Height + 1;
            StoredBlock parentBlock = parentChain.GetBlockByOffset(0);

            if (blockHeight % DifficultyUtils.DifficultyAdjustmentIntervalInBlocks != 0)
            {
                // If producing the last 6 block took more than 12h, increase the difficulty
                // target by 25% (which reduces the difficulty by 20%). This ensure the
                // chain do not get stuck in case we lose hashrate abruptly.

                BigInteger expectedTarget = DifficultyUtils.NBitsToTarget(parentBlock.Header.NBits);

                uint timeSpent6 = GetMedianTimePast(parentChain, parentBlock.Height) - GetMedianTimePast(parentChain, parentBlock.Height - 6);

                if (timeSpent6 >= 12 * 3600)
                {
                    expectedTarget = expectedTarget + (expectedTarget >> 2);
                    return DifficultyUtils.TargetToNBits(expectedTarget);
                }

                return parentChain.GetBlockByOffset(0).Header.NBits;
            }

            StoredBlock baseBlock = parentChain.GetBlockByHeight(blockHeight - DifficultyUtils.DifficultyAdjustmentIntervalInBlocks);

            // Note: There is a known bug here. It known as "off-by-one" or "Time Warp Bug".
            // Bug Description: http://bitcoin.stackexchange.com/questions/20597/where-exactly-is-the-off-by-one-difficulty-bug
            // Related Attack Description: https://bitcointalk.org/index.php?topic=43692.msg521772#msg521772
            uint timeSpent = parentBlock.Header.Timestamp - baseBlock.Header.Timestamp;

            BigInteger oldTarget = DifficultyUtils.NBitsToTarget(parentBlock.Header.NBits);

            var newTarget = DifficultyUtils.CalculateNewTarget(timeSpent, oldTarget);

            return DifficultyUtils.TargetToNBits(newTarget);
        }

        /// <summary>
        /// Calculates new NBits for a block that will be added after the given chain using Bitcoin Cash rules that were applied on 13th of November 2017.
        /// <para/>
        /// Specification: https://github.com/Bitcoin-UAHF/spec/blob/master/nov-13-hardfork-spec.md
        /// </summary>
        private static uint CalculateNextNBitsCash20171113(Subchain parentChain)
        {
            //todo: add test for this method

            StoredBlock parentBlock = parentChain.GetBlockByOffset(0);

            StoredBlock B_first = GetMedianTimeBlock(parentChain, parentBlock.Height - 144);
            StoredBlock B_last = GetMedianTimeBlock(parentChain, parentBlock.Height);

            uint timespan = B_last.Header.Timestamp - B_first.Header.Timestamp;
            const uint minTimespan = 72 * 600;
            const uint maxTimespan = 288 * 600;
            if (timespan < minTimespan)
            {
                timespan = minTimespan;
            }
            if (timespan > maxTimespan)
            {
                timespan = maxTimespan;
            }
            BigInteger workPerformed = 0;
            for (int i = B_first.Height + 1; i <= B_last.Height; i++)
            {
                workPerformed += GetBlockProof(parentChain.GetBlockByHeight(i).Header);
            }
            BigInteger projectedWork = workPerformed * 600 / timespan;

            BigInteger expectedTarget = (BigInteger.Pow(2, 256) - projectedWork) / projectedWork;

            if (expectedTarget > DifficultyUtils.MaxDifficultyTarget)
            {
                expectedTarget = DifficultyUtils.MaxDifficultyTarget;
            }

            return DifficultyUtils.TargetToNBits(expectedTarget);
        }

        private static BigInteger GetBlockProof(BlockHeader header)
        {
            BigInteger target = DifficultyUtils.NBitsToTarget(header.NBits);
            return (BigInteger.Pow(2, 256) - target) / (target + 1) + 1;
        }

        private static uint GetMedianTimePast(Subchain chain, int blockHeight)
        {
            int medianArraySize = Math.Min(11, chain.Length);

            uint[] values = new uint[medianArraySize];
            for (int i = 0; i < medianArraySize; i++)
            {
                values[i] = chain.GetBlockByHeight(blockHeight - i).Header.Timestamp;
            }
            Array.Sort(values);
            return values[medianArraySize / 2];
        }

        private static StoredBlock GetMedianTimeBlock(Subchain chain, int blockHeight)
        {
            // todo: what should happen if blockHeight < 3
            StoredBlock[] blocks = new StoredBlock[3];
            blocks[0] = chain.GetBlockByHeight(blockHeight - 2);
            blocks[1] = chain.GetBlockByHeight(blockHeight - 1);
            blocks[2] = chain.GetBlockByHeight(blockHeight);
            Array.Sort(blocks, (b1, b2) => b1.Header.Timestamp.CompareTo(b2.Header.Timestamp));
            return blocks[1];
        }
    }
}