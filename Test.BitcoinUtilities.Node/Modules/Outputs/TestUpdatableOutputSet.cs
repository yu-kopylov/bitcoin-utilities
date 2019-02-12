using System.Collections.Generic;
using System.Linq;
using System.Text;
using BitcoinUtilities;
using BitcoinUtilities.Node.Modules.Outputs;
using BitcoinUtilities.P2P.Primitives;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Node.Modules.Outputs
{
    [TestFixture]
    public class TestUpdatableOutputSet
    {
        [Test]
        public void TestAppendExistingUnspentOutputs()
        {
            UpdatableOutputSet set = new UpdatableOutputSet();

            byte[] tx1A = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("Tx 1"));
            byte[] tx1B = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("Tx 1"));
            byte[] tx2 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("Tx 2"));

            UtxoOutput tx1Output1 = new UtxoOutput(new TxOutPoint(tx1A, 1), 100, new byte[20]);
            UtxoOutput tx1Output2 = new UtxoOutput(new TxOutPoint(tx1A, 2), 200, new byte[20]);
            UtxoOutput tx1Output3 = new UtxoOutput(new TxOutPoint(tx1A, 3), 300, new byte[20]);

            set.AppendExistingUnspentOutputs(new[] {tx1Output1, tx1Output2});

            Assert.That(set.HasExistingTransaction(tx1A), Is.True);
            Assert.That(set.HasExistingTransaction(tx1B), Is.True);
            Assert.That(set.HasExistingTransaction(tx2), Is.False);

            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1A, 1)), Is.SameAs(tx1Output1));
            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1B, 1)), Is.SameAs(tx1Output1));
            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1B, 2)), Is.SameAs(tx1Output2));
            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx2, 2)), Is.Null);

            Assert.That(SortAndFormat(set.FindUnspentOutputs(tx1A)), Is.EqualTo(SortAndFormat(new[] {tx1Output1, tx1Output2})));
            Assert.That(SortAndFormat(set.FindUnspentOutputs(tx1B)), Is.EqualTo(SortAndFormat(new[] {tx1Output1, tx1Output2})));
            Assert.That(SortAndFormat(set.FindUnspentOutputs(tx2)), Is.Empty);
            Assert.That(SortAndFormat(set.Operations), Is.Empty);

            Assert.That(() => set.AppendExistingUnspentOutputs(new[] {tx1Output3}), Throws.Exception.Message.Contains("BIP-30"));
        }

        [Test]
        public void TestSpendExisting()
        {
            UpdatableOutputSet set = new UpdatableOutputSet();

            byte[] tx1 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("Tx 1"));
            byte[] tx2 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("Tx 2"));

            UtxoOutput tx1Output1 = new UtxoOutput(new TxOutPoint(tx1, 1), 100, new byte[20]);
            UtxoOutput tx1Output2 = new UtxoOutput(new TxOutPoint(tx1, 2), 200, new byte[20]);
            UtxoOutput tx1Output3 = new UtxoOutput(new TxOutPoint(tx1, 3), 200, new byte[20]);

            set.AppendExistingUnspentOutputs(new[] {tx1Output1, tx1Output2});

            Assert.That(set.HasExistingTransaction(tx1), Is.True);
            Assert.That(set.HasExistingTransaction(tx2), Is.False);

            set.Spend(tx1Output1);

            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1, 1)), Is.Null);
            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1, 2)), Is.SameAs(tx1Output2));
            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1, 3)), Is.Null);

            Assert.That(SortAndFormat(set.FindUnspentOutputs(tx1)), Is.EqualTo(SortAndFormat(new[] {tx1Output2})));
            Assert.That(SortAndFormat(set.FindUnspentOutputs(tx2)), Is.Empty);

            Assert.That(SortAndFormat(set.Operations), Is.EqualTo(SortAndFormat(new[]
            {
                UtxoOperation.Spend(tx1Output1)
            })));

            Assert.That(() => set.Spend(tx1Output1), Throws.Exception.Message.Contains("non-existent output"));
            Assert.That(() => set.Spend(tx1Output3), Throws.Exception.Message.Contains("non-existent output"));

            set.Spend(tx1Output2);

            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1, 1)), Is.Null);
            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1, 2)), Is.Null);
            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1, 3)), Is.Null);

            Assert.That(SortAndFormat(set.FindUnspentOutputs(tx1)), Is.Empty);
            Assert.That(SortAndFormat(set.FindUnspentOutputs(tx2)), Is.Empty);

            Assert.That(SortAndFormat(set.Operations), Is.EqualTo(SortAndFormat(new[]
            {
                UtxoOperation.Spend(tx1Output1),
                UtxoOperation.Spend(tx1Output2)
            })));
        }

        [Test]
        public void TestSpendCreated()
        {
            UpdatableOutputSet set = new UpdatableOutputSet();

            byte[] tx1 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("Tx 1"));
            byte[] tx2 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("Tx 2"));

            set.CreateUnspentOutput(tx1, 1, 100, new byte[20]);
            set.CreateUnspentOutput(tx1, 2, 200, new byte[20]);

            UtxoOutput tx1Output1T = new UtxoOutput(new TxOutPoint(tx1, 1), 100, new byte[20]);
            UtxoOutput tx1Output2T = new UtxoOutput(new TxOutPoint(tx1, 2), 200, new byte[20]);
            UtxoOutput tx1Output3T = new UtxoOutput(new TxOutPoint(tx1, 3), 300, new byte[20]);

            Assert.That(set.HasExistingTransaction(tx1), Is.False);

            UtxoOutput tx1Output1 = set.FindUnspentOutput(new TxOutPoint(tx1, 1));
            UtxoOutput tx1Output2 = set.FindUnspentOutput(new TxOutPoint(tx1, 2));

            Assert.That(Format(tx1Output1), Is.EqualTo(Format(tx1Output1T)));
            Assert.That(Format(tx1Output2), Is.EqualTo(Format(tx1Output2T)));
            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx2, 1)), Is.Null);

            Assert.That(SortAndFormat(set.FindUnspentOutputs(tx1)), Is.EqualTo(SortAndFormat(new[] {tx1Output1T, tx1Output2T})));

            Assert.That(SortAndFormat(set.Operations), Is.EqualTo(SortAndFormat(new[]
            {
                UtxoOperation.Create(tx1Output1T),
                UtxoOperation.Create(tx1Output2T)
            })));

            // spent in same block
            set.Spend(tx1Output1);

            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1, 1)), Is.Null);
            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1, 2)), Is.SameAs(tx1Output2));

            Assert.That(SortAndFormat(set.FindUnspentOutputs(tx1)), Is.EqualTo(SortAndFormat(new[] {tx1Output2T})));

            Assert.That(SortAndFormat(set.Operations), Is.EqualTo(SortAndFormat(new[]
            {
                UtxoOperation.Create(tx1Output1T),
                UtxoOperation.Create(tx1Output2T),
                UtxoOperation.Spend(tx1Output1T)
            })));

            Assert.That(() => set.Spend(tx1Output1), Throws.Exception.Message.Contains("non-existent output"));
            Assert.That(() => set.Spend(tx1Output3T), Throws.Exception.Message.Contains("non-existent output"));

            // spent in next block
            set.Spend(tx1Output2);

            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1, 1)), Is.Null);
            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1, 2)), Is.Null);

            Assert.That(SortAndFormat(set.FindUnspentOutputs(tx1)), Is.Empty);

            Assert.That(SortAndFormat(set.Operations), Is.EqualTo(SortAndFormat(new[]
            {
                UtxoOperation.Create(tx1Output1T),
                UtxoOperation.Create(tx1Output2T),
                UtxoOperation.Spend(tx1Output1T),
                UtxoOperation.Spend(tx1Output2T)
            })));
        }

        [Test]
        public void TestSpendExistingAndRecreate()
        {
            UpdatableOutputSet set = new UpdatableOutputSet();

            byte[] tx1 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("Tx 1"));

            UtxoOutput tx1Output1 = new UtxoOutput(new TxOutPoint(tx1, 1), 100, new byte[20]);
            UtxoOutput tx1Output2 = new UtxoOutput(new TxOutPoint(tx1, 2), 200, new byte[20]);

            set.AppendExistingUnspentOutputs(new[] {tx1Output1, tx1Output2});
            set.Spend(tx1Output1);
            set.Spend(tx1Output2);
            set.CreateUnspentOutput(tx1, 1, 300, new byte[20]);

            Assert.That(set.HasExistingTransaction(tx1), Is.True);

            UtxoOutput tx1Output1A = set.FindUnspentOutput(tx1Output1.OutPoint);
            UtxoOutput tx1Output1B = new UtxoOutput(new TxOutPoint(tx1, 1), 300, new byte[20]);

            Assert.That(Format(tx1Output1A), Is.EqualTo(Format(tx1Output1B)));
            Assert.That(set.FindUnspentOutput(tx1Output2.OutPoint), Is.Null);

            Assert.That(SortAndFormat(set.FindUnspentOutputs(tx1)), Is.EqualTo(SortAndFormat(new[] {tx1Output1B})));

            Assert.That(SortAndFormat(set.Operations), Is.EqualTo(SortAndFormat(new[]
            {
                UtxoOperation.Spend(tx1Output1),
                UtxoOperation.Spend(tx1Output2),
                UtxoOperation.Create(tx1Output1A)
            })));

            // spend again
            set.Spend(tx1Output1A);

            Assert.That(set.HasExistingTransaction(tx1), Is.True);

            Assert.That(set.FindUnspentOutput(tx1Output1.OutPoint), Is.Null);
            Assert.That(set.FindUnspentOutput(tx1Output2.OutPoint), Is.Null);

            Assert.That(SortAndFormat(set.FindUnspentOutputs(tx1)), Is.Empty);

            Assert.That(SortAndFormat(set.Operations), Is.EqualTo(SortAndFormat(new[]
            {
                UtxoOperation.Spend(tx1Output1),
                UtxoOperation.Spend(tx1Output2),
                UtxoOperation.Create(tx1Output1A),
                UtxoOperation.Spend(tx1Output1A)
            })));
        }

        private string[] SortAndFormat(IEnumerable<UtxoOutput> outputs)
        {
            return outputs.Select(Format).OrderBy(t => t).ToArray();
        }

        private static string Format(UtxoOutput output)
        {
            return $"{output.OutPoint} (V:{output.Value})";
        }

        private string[] SortAndFormat(IEnumerable<UtxoOperation> outputs)
        {
            return outputs.Select(Format).OrderBy(t => t).ToArray();
        }

        private static string Format(UtxoOperation operation)
        {
            return $"{(operation.Spent ? "Spent" : "Created")} {Format(operation.Output)}";
        }
    }
}