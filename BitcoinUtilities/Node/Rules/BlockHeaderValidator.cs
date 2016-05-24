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
        public const int DifficultyAdjustmentInterval = 2016;

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

        internal static bool IsValid(StoredBlock block, StoredBlock parentBlock)
        {
            BigInteger blockDifficultyTarget = NumberUtils.NBitsToTarget(block.Header.NBits);
            if (block.Height%DifficultyAdjustmentInterval != 0)
            {
                BigInteger parentBlockDifficultyTarget = NumberUtils.NBitsToTarget(parentBlock.Header.NBits);
                if (blockDifficultyTarget != parentBlockDifficultyTarget)
                {
                    return false;
                }
            }
            else
            {
                //todo: calculate new difficulty
            }
            return true;
        }
    }
}