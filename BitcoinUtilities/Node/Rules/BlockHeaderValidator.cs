using System;
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

            //todo: also compare nBits with network settings (it is a static check)

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

            uint[] oldTimestamps = new uint[medianBlockCount];

            for (int i = 0; i < medianBlockCount; i++)
            {
                oldTimestamps[i] = parentChain.GetBlockByOffset(i).Header.Timestamp;
            }

            Array.Sort(oldTimestamps);

            uint medianTimestamp = oldTimestamps[medianBlockCount/2];

            return block.Header.Timestamp > medianTimestamp;
        }

        /// <summary>
        /// Checks that the nBits field of a block follow protocol rules.
        /// </summary>
        internal static bool IsNBitsValid(StoredBlock block, Subchain parentChain)
        {
            //todo: add test for this method

            StoredBlock parentBlock = parentChain.GetBlockByOffset(0);

            BigInteger blockDifficultyTarget = DifficultyUtils.NBitsToTarget(block.Header.NBits);
            if (block.Height%DifficultyUtils.DifficultyAdjustmentIntervalInBlocks != 0)
            {
                BigInteger parentBlockDifficultyTarget = DifficultyUtils.NBitsToTarget(parentBlock.Header.NBits);
                if (blockDifficultyTarget != parentBlockDifficultyTarget)
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
    }
}