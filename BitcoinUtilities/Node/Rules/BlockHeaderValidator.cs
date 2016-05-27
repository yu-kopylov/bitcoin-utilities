using System;
using System.Numerics;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Storage;

namespace BitcoinUtilities.Node.Rules
{
    public static class BlockHeaderValidator
    {
        //todo: use network settings instead?
        public const int DifficultyAdjustmentIntervalInBlocks = 2016;
        public const int DifficultyAdjustmentIntervalInSeconds = 14*24*60*60;

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
            BigInteger difficultyTarget = NumberUtils.NBitsToTarget(header.NBits);

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

            BigInteger blockDifficultyTarget = NumberUtils.NBitsToTarget(block.Header.NBits);
            if (block.Height%DifficultyAdjustmentIntervalInBlocks != 0)
            {
                BigInteger parentBlockDifficultyTarget = NumberUtils.NBitsToTarget(parentBlock.Header.NBits);
                if (blockDifficultyTarget != parentBlockDifficultyTarget)
                {
                    return false;
                }
            }
            else
            {
                StoredBlock baseBlock = parentChain.GetBlockByHeight(block.Height - DifficultyAdjustmentIntervalInBlocks);

                // todo: check against non-bugged value and allow some percent-based difference? 

                // Note: There is a known bug here. It known as "off-by-one" or "Time Warp Bug".
                // Bug Description: http://bitcoin.stackexchange.com/questions/20597/where-exactly-is-the-off-by-one-difficulty-bug
                // Related Attack Description: https://bitcointalk.org/index.php?topic=43692.msg521772#msg521772
                uint timeSpent = parentBlock.Header.Timestamp - baseBlock.Header.Timestamp;

                BigInteger oldTarget = NumberUtils.NBitsToTarget(parentBlock.Header.NBits);

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
                BigInteger maxDifficultyTarget = NumberUtils.NBitsToTarget(0x1d00ffff);

                if (newTarget > maxDifficultyTarget)
                {
                    newTarget = maxDifficultyTarget;
                }

                // this rounds newTarget value to a value that would fit into NBits
                uint newNBits = NumberUtils.TargetToNBits(newTarget);
                newTarget = NumberUtils.NBitsToTarget(newNBits);

                if (blockDifficultyTarget != newTarget)
                {
                    return false;
                }
            }

            return true;
        }
    }
}