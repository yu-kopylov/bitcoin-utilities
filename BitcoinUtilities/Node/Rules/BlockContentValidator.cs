using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Storage;

namespace BitcoinUtilities.Node.Rules
{
    public static class BlockContentValidator
    {
        //todo: add XMLDOC here and to other validators

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

        public static bool IsValidCoinbaseTransaction(BlockMessage block)
        {
            if (block.Transactions == null || block.Transactions.Length == 0)
            {
                return false;
            }

            if (!IsValidCoinbaseTransaction(block.Transactions[0]))
            {
                return false;
            }

            // also checking that there is only one coinbase transaction
            for (int i = 1; i < block.Transactions.Length; i++)
            {
                Tx transaction = block.Transactions[i];
                foreach (TxIn input in transaction.Inputs)
                {
                    if (input.PreviousOutput.Hash.All(b => b == 0) || input.PreviousOutput.Index < 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsValidCoinbaseTransaction(Tx transaction)
        {
            if (transaction.Inputs.Length != 1)
            {
                return false;
            }
            TxIn input = transaction.Inputs[0];
            if (input.PreviousOutput.Hash.Any(b => b != 0) || input.PreviousOutput.Index != -1)
            {
                return false;
            }
            if (input.SignatureScript == null)
            {
                return false;
            }
            //todo: use network settings?
            return input.SignatureScript.Length >= 2 && input.SignatureScript.Length <= 100;
        }
    }
}