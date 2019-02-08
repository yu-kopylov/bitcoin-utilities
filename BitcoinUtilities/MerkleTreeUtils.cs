using System;
using System.Collections.Generic;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities
{
    public static class MerkleTreeUtils
    {
        /// <summary>
        /// Calculates hash of Merkle tree root for a given array of transactions.
        /// </summary>
        /// <param name="transactions">The array of transactions.</param>
        /// <exception cref="ArgumentException">If the array of transactions is null or empty.</exception>
        public static byte[] GetTreeRoot(Tx[] transactions)
        {
            if (transactions == null || transactions.Length == 0)
            {
                throw new ArgumentException($"{nameof(transactions)} array is null or empty.");
            }

            if (transactions == null || transactions.Length == 0)
            {
                throw new ArgumentException($"{nameof(transactions)} array is null or empty.");
            }

            List<byte[]> hashes = new List<byte[]>(transactions.Length);
            foreach (Tx transaction in transactions)
            {
                hashes.Add(transaction.Hash);
            }

            return GetTreeRoot(hashes);
        }

        /// <summary>
        /// Calculates hash of Merkle tree root for a given list of hashes.
        /// </summary>
        /// <param name="hashes">The array of transaction hashes.</param>
        /// <exception cref="ArgumentException">If the array of transactions is null or empty.</exception>
        public static byte[] GetTreeRoot(List<byte[]> hashes)
        {
            if (hashes == null || hashes.Count == 0)
            {
                throw new ArgumentException($"{nameof(hashes)} list is null or empty.");
            }

            while (hashes.Count > 1)
            {
                List<byte[]> newHashes = new List<byte[]>((hashes.Count + 1) / 2);
                for (int i = 0; i < hashes.Count; i += 2)
                {
                    byte[] hash1 = hashes[i];
                    byte[] hash2 = (i + 1 < hashes.Count) ? hashes[i + 1] : hashes[i];
                    newHashes.Add(CryptoUtils.Sha256(CryptoUtils.Sha256(hash1, hash2)));
                }

                hashes = newHashes;
            }

            return hashes[0];
        }
    }
}