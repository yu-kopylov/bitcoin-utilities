using System.IO;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.Storage.Converters;
using BitcoinUtilities.Storage.Models;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Storage.Converters
{
    [TestFixture]
    public class TestBlockConverter
    {
        [Test]
        public void Test()
        {
            BlockConverter conv = new BlockConverter();

            BlockMessage blockMessage;
            MemoryStream mem = new MemoryStream(GenesisBlock.Raw);
            using (BitcoinStreamReader reader = new BitcoinStreamReader(mem))
            {
                blockMessage = BlockMessage.Read(reader);
            }

            Block block = conv.FromMessage(blockMessage);

            Assert.That(block.Hash, Is.EqualTo(GenesisBlock.Hash));
            Assert.That(block.Transactions.Count, Is.EqualTo(1));

            Transaction transaction = block.Transactions[0];

            Assert.That(transaction.Hash, Is.EqualTo(GenesisBlock.MerkleRoot));
            Assert.That(transaction.Inputs.Count, Is.EqualTo(1));
            Assert.That(transaction.Outputs.Count, Is.EqualTo(1));

            TransactionInput input = transaction.Inputs[0];

            Assert.That(input.OutputHash, Is.EqualTo(new byte[32]));
            Assert.That(input.OutputIndex, Is.EqualTo(0xFFFFFFFF));
            Assert.That(input.SignatureScript, Is.EqualTo(GenesisBlock.SignatureScript));
            Assert.That(input.Sequence, Is.EqualTo(0xFFFFFFFF));

            TransactionOutput output = transaction.Outputs[0];

            Assert.That(output.Value, Is.EqualTo(5000000000));
            Assert.That(output.PubkeyScript, Is.EqualTo(GenesisBlock.PubkeyScript));
        }
    }
}