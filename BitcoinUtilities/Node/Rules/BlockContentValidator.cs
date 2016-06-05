using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node.Rules
{
    public static class BlockContentValidator
    {
        public static bool IsMerkleTreeValid(BlockMessage block)
        {
            if (block.Transactions == null || block.Transactions.Length == 0)
            {
                return false;
            }

            List<byte[]> transactionHashes = new List<byte[]>(block.Transactions.Length);
            HashSet<byte[]> transactionHashSet = new HashSet<byte[]>(ByteArrayComparer.Instance);
            foreach (Tx transaction in block.Transactions)
            {
                byte[] hash = CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(transaction.Write));
                if (!transactionHashSet.Add(hash))
                {
                    // Duplicate transactions can be used to construct invalid block that has same merkle tree root as valid block.
                    // Such blocks should not prevent acceptance of valid blocks.
                    // See: https://en.bitcoin.it/wiki/Common_Vulnerabilities_and_Exposures#CVE-2012-2459
                    return false;
                }
                transactionHashes.Add(hash);
            }

            byte[] merkleTreeRoot = MerkleTreeUtils.GetTreeRoot(transactionHashes);
            return merkleTreeRoot.SequenceEqual(block.BlockHeader.MerkleRoot);
        }
    }
}