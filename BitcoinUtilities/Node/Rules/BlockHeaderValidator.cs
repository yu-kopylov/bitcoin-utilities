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

        public static bool IsValid(StoredBlock block, Subchain parentChain)
        {
            return IsTimeStampValid(block, parentChain) && IsNBitsValid(block, parentChain);
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
        /// <para/>
        /// 12 hours adjustment algorithm: https://github.com/Bitcoin-ABC/bitcoin-abc/blob/master/src/pow.cpp
        /// </summary>
        internal static bool IsNBitsValid(StoredBlock block, Subchain parentChain)
        {
            //todo: add test for this method

            StoredBlock parentBlock = parentChain.GetBlockByOffset(0);

            BigInteger blockDifficultyTarget = DifficultyUtils.NBitsToTarget(block.Header.NBits);
            if (block.Height % DifficultyUtils.DifficultyAdjustmentIntervalInBlocks != 0)
            {
                BigInteger expectedTarget = DifficultyUtils.NBitsToTarget(parentBlock.Header.NBits);

                // todo: Apply this 12-hour adjustment rule only to Bitcoin Cash chain.
                // todo: In the Bitcoin Core chain such delays were happening only in the beginning, thus first 24000 blocks are excluded from this adjustment rule.
                // todo: Also implement the 12-hour adjustment rule for scenario with fast block generation.
                if (parentBlock.Height >= 24000)
                {
                    // If producing the last 6 block took more than 12h, increase the difficulty
                    // target by 25% (which reduces the difficulty by 20%). This ensure the
                    // chain do not get stuck in case we lose hashrate abruptly.

                    // todo: test, adjustment happened for block 478577
                    uint timeSpent6 = GetMedianTimePast(parentChain, parentBlock.Height) - GetMedianTimePast(parentChain, parentBlock.Height - 6);

                    if (timeSpent6 >= 12 * 3600)
                    {
                        // todo: check that that target is not bigger than maximum
                        expectedTarget = expectedTarget + (expectedTarget >> 2);
                        uint newNBits = DifficultyUtils.TargetToNBits(expectedTarget);
                        expectedTarget = DifficultyUtils.NBitsToTarget(newNBits);
                    }
                }

                if (blockDifficultyTarget != expectedTarget)
                {
                    return false;
                }
            }
            else
            {
                StoredBlock baseBlock = parentChain.GetBlockByHeight(block.Height - DifficultyUtils.DifficultyAdjustmentIntervalInBlocks);

                // todo: check against non-bugged value and allow some percent-based difference? 

                // Note: There is a known bug here. It known as "off-by-one" or "Time Warp Bug".
                // Bug Description: http://bitcoin.stackexchange.com/questions/20597/where-exactly-is-the-off-by-one-difficulty-bug
                // Related Attack Description: https://bitcointalk.org/index.php?topic=43692.msg521772#msg521772
                uint timeSpent = parentBlock.Header.Timestamp - baseBlock.Header.Timestamp;

                BigInteger oldTarget = DifficultyUtils.NBitsToTarget(parentBlock.Header.NBits);

                var newTarget = DifficultyUtils.CalculateNewTarget(timeSpent, oldTarget);

                // this rounds newTarget value to a value that would fit into NBits
                uint newNBits = DifficultyUtils.TargetToNBits(newTarget);
                newTarget = DifficultyUtils.NBitsToTarget(newNBits);

                if (blockDifficultyTarget != newTarget)
                {
                    return false;
                }
            }

            return true;
        }

        private static uint GetMedianTimePast(Subchain chain, int blockHeight)
        {
            int medianArraySize = Math.Min(11, blockHeight + 1);

            uint[] values = new uint[medianArraySize];
            for (int i = 0; i < medianArraySize; i++)
            {
                values[i] = chain.GetBlockByHeight(blockHeight - i).Header.Timestamp;
            }
            Array.Sort(values);
            return values[medianArraySize / 2];
        }
    }
}