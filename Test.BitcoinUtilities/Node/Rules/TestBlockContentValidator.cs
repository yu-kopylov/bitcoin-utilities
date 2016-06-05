using System.Collections.Generic;
using System.IO;
using System.Linq;
using BitcoinUtilities;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Storage;
using NUnit.Framework;
using TestUtilities;

namespace Test.BitcoinUtilities.Node.Rules
{
    [TestFixture]
    public class TestBlockContentValidator
    {
        [Test]
        public void TestKnownBlocks()
        {
            List<byte[]> knownBlocks = new List<byte[]>();
            knownBlocks.Add(GenesisBlock.Raw);
            knownBlocks.Add(KnownBlocks.Block100000);
            knownBlocks.Add(KnownBlocks.Block100001);
            knownBlocks.Add(KnownBlocks.Block100002);

            foreach (byte[] rawBlock in knownBlocks)
            {
                BlockMessage block = BlockMessage.Read(new BitcoinStreamReader(new MemoryStream(rawBlock)));
                Assert.True(BlockContentValidator.IsMerkleTreeValid(block));
                Assert.True(BlockContentValidator.IsValid(new StoredBlock(block.BlockHeader), block));
            }
        }

        [Test]
        public void TestNoTransactions()
        {
            BlockMessage block = new BlockMessage(GenesisBlock.GetHeader(), new Tx[0]);
            Assert.False(BlockContentValidator.IsMerkleTreeValid(block));
        }

        [Test]
        public void TestWrongMerkleTreeRoot()
        {
            BlockMessage validBlock1 = BlockMessage.Read(new BitcoinStreamReader(new MemoryStream(KnownBlocks.Block100001)));
            BlockMessage validBlock2 = BlockMessage.Read(new BitcoinStreamReader(new MemoryStream(KnownBlocks.Block100002)));
            Assert.That(BlockContentValidator.IsMerkleTreeValid(validBlock1));
            Assert.That(BlockContentValidator.IsMerkleTreeValid(validBlock2));

            BlockMessage invalidBlock1 = new BlockMessage(validBlock1.BlockHeader, validBlock2.Transactions);
            BlockMessage invalidBlock2 = new BlockMessage(validBlock2.BlockHeader, validBlock1.Transactions);
            Assert.False(BlockContentValidator.IsMerkleTreeValid(invalidBlock1));
            Assert.False(BlockContentValidator.IsMerkleTreeValid(invalidBlock2));
        }

        [Test]
        public void TestDuplicateTransaction()
        {
            BlockMessage validBlock1 = BlockMessage.Read(new BitcoinStreamReader(new MemoryStream(KnownBlocks.Block100001)));
            BlockMessage validBlock2 = BlockMessage.Read(new BitcoinStreamReader(new MemoryStream(KnownBlocks.Block100002)));

            Assert.True(BlockContentValidator.IsMerkleTreeValid(validBlock1));
            Assert.True(BlockContentValidator.IsMerkleTreeValid(validBlock2));

            Assert.That(validBlock1.Transactions.Length, Is.EqualTo(12));
            Assert.That(validBlock2.Transactions.Length, Is.EqualTo(9));

            List<Tx> transactions1 = new List<Tx>(validBlock1.Transactions);
            transactions1.Add(validBlock1.Transactions[8]);
            transactions1.Add(validBlock1.Transactions[9]);
            transactions1.Add(validBlock1.Transactions[10]);
            transactions1.Add(validBlock1.Transactions[11]);
            BlockMessage invalidBlock1 = new BlockMessage(validBlock1.BlockHeader, transactions1.ToArray());

            List<Tx> transactions2 = new List<Tx>(validBlock2.Transactions);
            transactions2.Add(validBlock2.Transactions[8]);
            BlockMessage invalidBlock2 = new BlockMessage(validBlock2.BlockHeader, transactions2.ToArray());

            Assert.That(MerkleTreeUtils.GetTreeRoot(invalidBlock1.Transactions), Is.EqualTo(invalidBlock1.BlockHeader.MerkleRoot));
            Assert.That(MerkleTreeUtils.GetTreeRoot(invalidBlock2.Transactions), Is.EqualTo(invalidBlock2.BlockHeader.MerkleRoot));

            Assert.False(BlockContentValidator.IsMerkleTreeValid(invalidBlock1));
            Assert.False(BlockContentValidator.IsMerkleTreeValid(invalidBlock2));
        }

        [Test]
        public void TestInvalidCoinbase()
        {
            BlockMessage validBlock = BitcoinStreamReader.FromBytes(KnownBlocks.Block100000, BlockMessage.Read);
            Assert.True(BlockContentValidator.IsValidCoinbaseTransaction(validBlock));

            Tx validCoinbaseTransaction = validBlock.Transactions[0];
            TxIn validCoinbaseInput = validCoinbaseTransaction.Inputs[0];

            // first checking that we reconstruct block correctly
            {
                List<Tx> transactions = new List<Tx>();
                transactions.Add(new Tx(
                    validCoinbaseTransaction.Version,
                    new TxIn[] {new TxIn(new TxOutPoint(new byte[32], -1), validCoinbaseInput.SignatureScript, validCoinbaseInput.Sequence)},
                    validCoinbaseTransaction.Outputs,
                    validCoinbaseTransaction.LockTime));
                transactions.AddRange(validBlock.Transactions.Skip(1));
                BlockMessage invalidBlock = new BlockMessage(validBlock.BlockHeader, transactions.ToArray());
                Assert.True(BlockContentValidator.IsValidCoinbaseTransaction(invalidBlock));
            }

            // cheking invalid signature
            {
                List<Tx> transactions = new List<Tx>();
                transactions.Add(new Tx(
                    validCoinbaseTransaction.Version,
                    new TxIn[] {new TxIn(new TxOutPoint(new byte[32], -1), new byte[101], validCoinbaseInput.Sequence)},
                    validCoinbaseTransaction.Outputs,
                    validCoinbaseTransaction.LockTime));
                transactions.AddRange(validBlock.Transactions.Skip(1));
                BlockMessage invalidBlock = new BlockMessage(validBlock.BlockHeader, transactions.ToArray());
                Assert.False(BlockContentValidator.IsValidCoinbaseTransaction(invalidBlock));
            }

            // cheking invalid output index
            {
                List<Tx> transactions = new List<Tx>();
                transactions.Add(new Tx(
                    validCoinbaseTransaction.Version,
                    new TxIn[] {new TxIn(new TxOutPoint(new byte[32], 0), validCoinbaseInput.SignatureScript, validCoinbaseInput.Sequence)},
                    validCoinbaseTransaction.Outputs,
                    validCoinbaseTransaction.LockTime));
                transactions.AddRange(validBlock.Transactions.Skip(1));
                BlockMessage invalidBlock = new BlockMessage(validBlock.BlockHeader, transactions.ToArray());
                Assert.False(BlockContentValidator.IsValidCoinbaseTransaction(invalidBlock));
            }

            // cheking duplicate coinbase
            {
                List<Tx> transactions = new List<Tx>();
                transactions.Add(new Tx(
                    validCoinbaseTransaction.Version,
                    new TxIn[] {new TxIn(new TxOutPoint(new byte[32], -1), new byte[10], validCoinbaseInput.Sequence)},
                    validCoinbaseTransaction.Outputs,
                    validCoinbaseTransaction.LockTime));
                transactions.Add(new Tx(
                    validCoinbaseTransaction.Version,
                    new TxIn[] {new TxIn(new TxOutPoint(new byte[32], -1), new byte[20], validCoinbaseInput.Sequence)},
                    validCoinbaseTransaction.Outputs,
                    validCoinbaseTransaction.LockTime));
                transactions.AddRange(validBlock.Transactions.Skip(1));
                BlockMessage invalidBlock = new BlockMessage(validBlock.BlockHeader, transactions.ToArray());
                Assert.False(BlockContentValidator.IsValidCoinbaseTransaction(invalidBlock));
            }
        }
    }
}