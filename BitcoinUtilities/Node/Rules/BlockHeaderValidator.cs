using System;
using System.Numerics;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Storage;

namespace BitcoinUtilities.Node.Rules
{
    public static class BlockHeaderValidator
    {
        public static bool IsValid(BlockHeader header)
        {
            return
                IsTimestampValid(header) &&
                IsHashValid(header);
        }

        /// <remarks>Specification: https://en.bitcoin.it/wiki/Block_timestamp</remarks>
        internal static bool IsTimestampValid(BlockHeader header)
        {
            //todo: also compare with previous blocks
            //todo: Specification requires to adjust current time. Why is it necessary?
            return UnixTime.ToDateTime(header.Timestamp) < DateTime.UtcNow.AddHours(2);
        }

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

            //todo: compare nBits with network settings


            //todo: add other validations

            return true;
        }

        internal static bool IsValid(StoredBlock block, Subchain parentChain)
        {
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