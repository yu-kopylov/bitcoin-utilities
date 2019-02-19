using System;
using System.Collections.Generic;
using System.Numerics;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node.Rules
{
    public static class BlockHeaderValidator
    {
        /* Lower Boundaries:
         * 2016 - When difficulty changes, we need the 2016th block before the current one. Source: https://en.bitcoin.it/wiki/Protocol_rules#Difficulty_change .
         * 11 - A timestamp is accepted as valid if it is greater than the median timestamp of previous 11 blocks. Source: https://en.bitcoin.it/wiki/Block_timestamp .
         */
        public const int RequiredSubchainLength = 2016;

        /// <summary>
        /// Validates basic structure of the given header.
        /// </summary>
        /// <param name="header">The header to validate.</param>
        /// <param name="hash">The hash of the given header.</param>
        /// <returns>True if header is valid; otherwise, false.</returns>
        public static bool IsValid(NetworkParameters networkParameters, BlockHeader header, byte[] hash)
        {
            return
                IsNBitsValid(networkParameters, header) &&
                IsTimestampValid(header) &&
                IsHashValid(header, hash);
        }

        /// <summary>
        /// Validates the given header against the specified blockchain, but does not performs basic structure validation.
        /// </summary>
        /// <param name="networkParameters">Parameters of the network.</param>
        /// <param name="header">The header to validate.</param>
        /// <param name="parentChain">A chain of headers that precede the given block in the blockchain.</param>
        /// <returns>True if header is valid; otherwise, false.</returns>
        public static bool IsValid(NetworkParameters networkParameters, IValidatableHeader header, ISubchain<IValidatableHeader> parentChain)
        {
            byte[] checkpointHash = networkParameters.GetCheckpointHash(header.Height);
            if (checkpointHash != null)
            {
                return ByteArrayComparer.Instance.Equals(checkpointHash, header.Hash);
            }

            return IsTimeStampValid(header, parentChain) && IsNBitsValid(networkParameters, header, parentChain);
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
        internal static bool IsNBitsValid(NetworkParameters networkParameters, BlockHeader header)
        {
            BigInteger target = DifficultyUtils.NBitsToTarget(header.NBits);
            return target <= networkParameters.MaxDifficultyTarget;
        }

        /// <summary>
        /// Checks that a hash of the header matches nBits field in the header, and that header is not its own parent.
        /// </summary>
        internal static bool IsHashValid(BlockHeader header, byte[] hash)
        {
            if (ByteArrayComparer.Instance.Equals(hash, header.PrevBlock))
            {
                return false;
            }

            BigInteger hashAsNumber = NumberUtils.ToBigInteger(hash);
            BigInteger difficultyTarget = DifficultyUtils.NBitsToTarget(header.NBits);

            if (hashAsNumber > difficultyTarget)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks that timestamp of the block is after median time of the last 11 blocks.
        /// </summary>
        internal static bool IsTimeStampValid(IValidatableHeader header, ISubchain<IValidatableHeader> parentChain)
        {
            //todo: use network settings
            const int medianBlockCount = 11;

            List<uint> oldTimestamps = new List<uint>(medianBlockCount);

            for (int i = 0; i < medianBlockCount && i < parentChain.Count; i++)
            {
                oldTimestamps.Add(parentChain.GetBlockByOffset(i).Timestamp);
            }

            oldTimestamps.Sort();

            uint medianTimestamp = oldTimestamps[oldTimestamps.Count / 2];

            return header.Timestamp > medianTimestamp;
        }

        /// <summary>
        /// Checks that the nBits field of a block follow protocol rules.
        /// </summary>
        internal static bool IsNBitsValid(NetworkParameters networkParameters, IValidatableHeader header, ISubchain<IValidatableHeader> parentChain)
        {
            uint expectedNBits = CalculateNextNBits(networkParameters, parentChain);
            return expectedNBits == header.NBits;
        }

        /// <summary>
        /// Calculates new NBits for a block that will be added after the given chain.
        /// </summary>
        private static uint CalculateNextNBits(NetworkParameters networkParameters, ISubchain<IValidatableHeader> parentChain)
        {
            if (!networkParameters.DifficultyAdjustmentEnabled)
            {
                return parentChain.GetBlockByOffset(0).NBits;
            }

            BigInteger nextTarget = CalculateNextTarget(networkParameters.Fork, parentChain);
            if (nextTarget > networkParameters.MaxDifficultyTarget)
            {
                nextTarget = networkParameters.MaxDifficultyTarget;
            }

            return DifficultyUtils.TargetToNBits(nextTarget);
        }

        /// <summary>
        /// Calculates new difficulty target for a block that will be added after the given chain.
        /// </summary>
        private static BigInteger CalculateNextTarget(BitcoinFork fork, ISubchain<IValidatableHeader> parentChain)
        {
            if (fork == BitcoinFork.Core)
            {
                return CalculateNextTargetCoreOriginal(parentChain);
            }

            if (fork == BitcoinFork.Cash)
            {
                uint timestamp = GetMedianTimePast(parentChain, parentChain.GetBlockByOffset(0).Height);
                if (timestamp >= 1510600000)
                {
                    return CalculateNextTargetCash20171113(parentChain);
                }

                // In the past such delays were happening only in the beginning, so it should be fine to apply new algorithm after for all blocks after year 2017.
                if (timestamp >= 1483228800)
                {
                    return CalculateNextTargetCashOriginal(parentChain);
                }

                return CalculateNextTargetCoreOriginal(parentChain);
            }

            throw new InvalidOperationException($"Difficulty adjustment algorithm is not defined for fork {fork} at height {parentChain.GetBlockByOffset(0).Height}.");
        }

        /// <summary>
        /// Calculates new NBits for a block that will be added after the given chain using Bitcoin Core rules.
        /// </summary>
        private static BigInteger CalculateNextTargetCoreOriginal(ISubchain<IValidatableHeader> parentChain)
        {
            //todo: add test for this method

            int blockHeight = parentChain.GetBlockByOffset(0).Height + 1;

            if (blockHeight % DifficultyUtils.DifficultyAdjustmentIntervalInBlocks != 0)
            {
                return DifficultyUtils.NBitsToTarget(parentChain.GetBlockByOffset(0).NBits);
            }

            IValidatableHeader baseBlock = parentChain.GetBlockByHeight(blockHeight - DifficultyUtils.DifficultyAdjustmentIntervalInBlocks);
            IValidatableHeader prevBlock = parentChain.GetBlockByOffset(0);

            // Note: There is a known bug here. It known as "off-by-one" or "Time Warp Bug".
            // Bug Description: http://bitcoin.stackexchange.com/questions/20597/where-exactly-is-the-off-by-one-difficulty-bug
            // Related Attack Description: https://bitcointalk.org/index.php?topic=43692.msg521772#msg521772
            uint timeSpent = prevBlock.Timestamp - baseBlock.Timestamp;

            BigInteger oldTarget = DifficultyUtils.NBitsToTarget(prevBlock.NBits);
            return DifficultyUtils.CalculateNewTarget(timeSpent, oldTarget);
        }

        /// <summary>
        /// Calculates new NBits for a block that will be added after the given chain using original Bitcoin Cash rules.
        /// <para/>
        /// 12 hours adjustment algorithm: https://github.com/Bitcoin-ABC/bitcoin-abc/blob/master/src/pow.cpp
        /// </summary>
        private static BigInteger CalculateNextTargetCashOriginal(ISubchain<IValidatableHeader> parentChain)
        {
            //todo: add test for this method

            int blockHeight = parentChain.GetBlockByOffset(0).Height + 1;
            IValidatableHeader prevBlock = parentChain.GetBlockByOffset(0);
            BigInteger oldTarget = DifficultyUtils.NBitsToTarget(prevBlock.NBits);

            if (blockHeight % DifficultyUtils.DifficultyAdjustmentIntervalInBlocks != 0)
            {
                // If producing the last 6 block took more than 12h, increase the difficulty
                // target by 25% (which reduces the difficulty by 20%). This ensure the
                // chain do not get stuck in case we lose hashrate abruptly.

                uint timeSpent6 = GetMedianTimePast(parentChain, prevBlock.Height) - GetMedianTimePast(parentChain, prevBlock.Height - 6);

                if (timeSpent6 >= 12 * 3600)
                {
                    return oldTarget + (oldTarget >> 2);
                }

                return oldTarget;
            }

            IValidatableHeader baseBlock = parentChain.GetBlockByHeight(blockHeight - DifficultyUtils.DifficultyAdjustmentIntervalInBlocks);

            // Note: There is a known bug here. It known as "off-by-one" or "Time Warp Bug".
            // Bug Description: http://bitcoin.stackexchange.com/questions/20597/where-exactly-is-the-off-by-one-difficulty-bug
            // Related Attack Description: https://bitcointalk.org/index.php?topic=43692.msg521772#msg521772
            uint timeSpent = prevBlock.Timestamp - baseBlock.Timestamp;

            return DifficultyUtils.CalculateNewTarget(timeSpent, oldTarget);
        }

        /// <summary>
        /// Calculates new NBits for a block that will be added after the given chain using Bitcoin Cash rules that were applied on 13th of November 2017.
        /// <para/>
        /// Specification: https://github.com/Bitcoin-UAHF/spec/blob/master/nov-13-hardfork-spec.md
        /// </summary>
        private static BigInteger CalculateNextTargetCash20171113(ISubchain<IValidatableHeader> parentChain)
        {
            //todo: add test for this method

            IValidatableHeader parentBlock = parentChain.GetBlockByOffset(0);

            IValidatableHeader B_first = GetMedianTimeBlock(parentChain, parentBlock.Height - 144);
            IValidatableHeader B_last = GetMedianTimeBlock(parentChain, parentBlock.Height);

            uint timespan = B_last.Timestamp - B_first.Timestamp;
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
                workPerformed += GetBlockProof(parentChain.GetBlockByHeight(i).NBits);
            }

            BigInteger projectedWork = workPerformed * 600 / timespan;
            return (BigInteger.Pow(2, 256) - projectedWork) / projectedWork;
        }

        private static BigInteger GetBlockProof(uint nBits)
        {
            BigInteger target = DifficultyUtils.NBitsToTarget(nBits);
            return (BigInteger.Pow(2, 256) - target) / (target + 1) + 1;
        }

        private static uint GetMedianTimePast(ISubchain<IValidatableHeader> chain, int blockHeight)
        {
            int medianArraySize = Math.Min(11, chain.Count);

            uint[] values = new uint[medianArraySize];
            for (int i = 0; i < medianArraySize; i++)
            {
                values[i] = chain.GetBlockByHeight(blockHeight - i).Timestamp;
            }

            Array.Sort(values);
            return values[medianArraySize / 2];
        }

        private static IValidatableHeader GetMedianTimeBlock(ISubchain<IValidatableHeader> chain, int blockHeight)
        {
            // todo: what should happen if blockHeight < 3st
            IValidatableHeader[] blocks = new IValidatableHeader[3];
            blocks[0] = chain.GetBlockByHeight(blockHeight - 2);
            blocks[1] = chain.GetBlockByHeight(blockHeight - 1);
            blocks[2] = chain.GetBlockByHeight(blockHeight);
            Array.Sort(blocks, (b1, b2) => b1.Timestamp.CompareTo(b2.Timestamp));
            return blocks[1];
        }
    }
}