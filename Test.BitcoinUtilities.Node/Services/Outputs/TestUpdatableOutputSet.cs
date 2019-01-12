using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BitcoinUtilities;
using BitcoinUtilities.Node.Services.Outputs;
using BitcoinUtilities.P2P.Primitives;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Node.Services.Outputs
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

            UtxoOutput tx1Output1 = new UtxoOutput(tx1A, 1, 0, new TxOut(100, new byte[20]));
            UtxoOutput tx1Output2 = new UtxoOutput(tx1A, 2, 0, new TxOut(200, new byte[20]));
            UtxoOutput tx1Output3 = new UtxoOutput(tx1A, 3, 0, new TxOut(300, new byte[20]));

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
            Assert.That(SortAndFormat(set.ExistingSpentOutputs), Is.Empty);
            Assert.That(SortAndFormat(set.CreatedUnspentOutputs), Is.Empty);
            Assert.That(SortAndFormat(set.CreatedSpentOutputs), Is.Empty);

            Assert.That(() => set.AppendExistingUnspentOutputs(new[] {tx1Output3}), Throws.Exception.Message.Contains("BIP-30"));
        }

        [Test]
        public void TestSpendExisting()
        {
            UpdatableOutputSet set = new UpdatableOutputSet();

            byte[] tx1 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("Tx 1"));
            byte[] tx2 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("Tx 2"));

            UtxoOutput tx1Output1 = new UtxoOutput(tx1, 1, 0, new TxOut(100, new byte[20]));
            UtxoOutput tx1Output2 = new UtxoOutput(tx1, 2, 0, new TxOut(200, new byte[20]));
            UtxoOutput tx1Output3 = new UtxoOutput(tx1, 3, 0, new TxOut(200, new byte[20]));

            set.AppendExistingUnspentOutputs(new[] {tx1Output1, tx1Output2});

            Assert.That(set.HasExistingTransaction(tx1), Is.True);
            Assert.That(set.HasExistingTransaction(tx2), Is.False);

            set.Spend(tx1Output1, 1);

            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1, 1)), Is.Null);
            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1, 2)), Is.SameAs(tx1Output2));
            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1, 3)), Is.Null);

            Assert.That(SortAndFormat(set.FindUnspentOutputs(tx1)), Is.EqualTo(SortAndFormat(new[] {tx1Output2})));
            Assert.That(SortAndFormat(set.FindUnspentOutputs(tx2)), Is.Empty);

            Assert.That(SortAndFormat(set.ExistingSpentOutputs), Is.EqualTo(SortAndFormat(new[] {tx1Output1.Spend(1)})));
            Assert.That(SortAndFormat(set.CreatedUnspentOutputs), Is.Empty);
            Assert.That(SortAndFormat(set.CreatedSpentOutputs), Is.Empty);

            Assert.That(() => set.Spend(tx1Output1, 1), Throws.Exception.Message.Contains("non-existent output"));
            Assert.That(() => set.Spend(tx1Output3, 2), Throws.Exception.Message.Contains("non-existent output"));

            set.Spend(tx1Output2, 2);

            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1, 1)), Is.Null);
            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1, 2)), Is.Null);
            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1, 3)), Is.Null);

            Assert.That(SortAndFormat(set.FindUnspentOutputs(tx1)), Is.Empty);
            Assert.That(SortAndFormat(set.FindUnspentOutputs(tx2)), Is.Empty);

            Assert.That(SortAndFormat(set.ExistingSpentOutputs), Is.EqualTo(SortAndFormat(new[] {tx1Output1.Spend(1), tx1Output2.Spend(2)})));
            Assert.That(SortAndFormat(set.CreatedUnspentOutputs), Is.Empty);
            Assert.That(SortAndFormat(set.CreatedSpentOutputs), Is.Empty);
        }

        [Test]
        public void TestSpendCreated()
        {
            UpdatableOutputSet set = new UpdatableOutputSet();

            byte[] tx1 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("Tx 1"));
            byte[] tx2 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("Tx 2"));

            set.CreateUnspentOutput(tx1, 1, 0, new TxOut(100, new byte[20]));
            set.CreateUnspentOutput(tx1, 2, 0, new TxOut(200, new byte[20]));

            UtxoOutput tx1Output1T = new UtxoOutput(tx1, 1, 0, new TxOut(100, new byte[20]));
            UtxoOutput tx1Output2T = new UtxoOutput(tx1, 2, 0, new TxOut(200, new byte[20]));
            UtxoOutput tx1Output3T = new UtxoOutput(tx1, 3, 0, new TxOut(300, new byte[20]));

            Assert.That(set.HasExistingTransaction(tx1), Is.False);

            UtxoOutput tx1Output1 = set.FindUnspentOutput(new TxOutPoint(tx1, 1));
            UtxoOutput tx1Output2 = set.FindUnspentOutput(new TxOutPoint(tx1, 2));

            Assert.That(Format(tx1Output1), Is.EqualTo(Format(tx1Output1T)));
            Assert.That(Format(tx1Output2), Is.EqualTo(Format(tx1Output2T)));
            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx2, 1)), Is.Null);

            Assert.That(SortAndFormat(set.FindUnspentOutputs(tx1)), Is.EqualTo(SortAndFormat(new[] {tx1Output1T, tx1Output2T})));

            Assert.That(SortAndFormat(set.ExistingSpentOutputs), Is.Empty);
            Assert.That(SortAndFormat(set.CreatedUnspentOutputs), Is.EqualTo(SortAndFormat(new[] {tx1Output1T, tx1Output2T})));
            Assert.That(SortAndFormat(set.CreatedSpentOutputs), Is.Empty);

            // spent in same block
            set.Spend(tx1Output1, 0);

            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1, 1)), Is.Null);
            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1, 2)), Is.SameAs(tx1Output2));

            Assert.That(SortAndFormat(set.FindUnspentOutputs(tx1)), Is.EqualTo(SortAndFormat(new[] {tx1Output2T})));

            Assert.That(SortAndFormat(set.ExistingSpentOutputs), Is.Empty);
            Assert.That(SortAndFormat(set.CreatedUnspentOutputs), Is.EqualTo(SortAndFormat(new[] {tx1Output2T})));
            Assert.That(SortAndFormat(set.CreatedSpentOutputs), Is.EqualTo(SortAndFormat(new[] {tx1Output1T.Spend(0)})));

            Assert.That(() => set.Spend(tx1Output1, 0), Throws.Exception.Message.Contains("non-existent output"));
            Assert.That(() => set.Spend(tx1Output3T, 0), Throws.Exception.Message.Contains("non-existent output"));

            // spent in next block
            set.Spend(tx1Output2, 1);

            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1, 1)), Is.Null);
            Assert.That(set.FindUnspentOutput(new TxOutPoint(tx1, 2)), Is.Null);

            Assert.That(SortAndFormat(set.FindUnspentOutputs(tx1)), Is.Empty);

            Assert.That(SortAndFormat(set.ExistingSpentOutputs), Is.Empty);
            Assert.That(SortAndFormat(set.CreatedUnspentOutputs), Is.Empty);
            Assert.That(SortAndFormat(set.CreatedSpentOutputs), Is.EqualTo(SortAndFormat(new[] {tx1Output1T.Spend(0), tx1Output2T.Spend(1)})));
        }

        [Test]
        public void TestSpendExistingAndRecreate()
        {
            UpdatableOutputSet set = new UpdatableOutputSet();

            byte[] tx1 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("Tx 1"));

            UtxoOutput tx1Output1 = new UtxoOutput(tx1, 1, 0, new TxOut(100, new byte[20]));
            UtxoOutput tx1Output2 = new UtxoOutput(tx1, 2, 0, new TxOut(200, new byte[20]));

            set.AppendExistingUnspentOutputs(new[] {tx1Output1, tx1Output2});
            set.Spend(tx1Output1, 1);
            set.Spend(tx1Output2, 1);
            set.CreateUnspentOutput(tx1, 1, 2, new TxOut(300, new byte[20]));

            Assert.That(set.HasExistingTransaction(tx1), Is.True);

            UtxoOutput tx1Output1A = set.FindUnspentOutput(tx1Output1.OutputPoint);
            UtxoOutput tx1Output1B = new UtxoOutput(tx1, 1, 2, new TxOut(300, new byte[20]));

            Assert.That(Format(tx1Output1A), Is.EqualTo(Format(tx1Output1B)));
            Assert.That(set.FindUnspentOutput(tx1Output2.OutputPoint), Is.Null);

            Assert.That(SortAndFormat(set.FindUnspentOutputs(tx1)), Is.EqualTo(SortAndFormat(new[] {tx1Output1B})));

            Assert.That(SortAndFormat(set.ExistingSpentOutputs), Is.EqualTo(SortAndFormat(new[] {tx1Output1.Spend(1), tx1Output2.Spend(1)})));
            Assert.That(SortAndFormat(set.CreatedUnspentOutputs), Is.EqualTo(SortAndFormat(new[] {tx1Output1B})));
            Assert.That(SortAndFormat(set.CreatedSpentOutputs), Is.Empty);

            // spend again
            set.Spend(tx1Output1A, 3);

            Assert.That(set.HasExistingTransaction(tx1), Is.True);

            Assert.That(set.FindUnspentOutput(tx1Output1.OutputPoint), Is.Null);
            Assert.That(set.FindUnspentOutput(tx1Output2.OutputPoint), Is.Null);

            Assert.That(SortAndFormat(set.FindUnspentOutputs(tx1)), Is.Empty);

            Assert.That(SortAndFormat(set.ExistingSpentOutputs), Is.EqualTo(SortAndFormat(new[] {tx1Output1.Spend(1), tx1Output2.Spend(1)})));
            Assert.That(SortAndFormat(set.CreatedUnspentOutputs), Is.Empty);
            Assert.That(SortAndFormat(set.CreatedSpentOutputs), Is.EqualTo(SortAndFormat(new[] {tx1Output1B.Spend(3)})));
        }

        [Test]
        public void TestSpendModified()
        {
            UpdatableOutputSet set = new UpdatableOutputSet();

            byte[] tx1 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("Tx 1"));
            byte[] tx2 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("Tx 2"));

            UtxoOutput tx1Output1 = new UtxoOutput(tx1, 1, 0, new TxOut(100, new byte[20]));

            set.AppendExistingUnspentOutputs(new[] {tx1Output1});
            set.CreateUnspentOutput(tx2, 1, 1, new TxOut(200, new byte[20]));

            var tx2Output1 = set.FindUnspentOutput(new TxOutPoint(tx2, 1));

            Assert.That(() => set.Spend(tx1Output1.Spend(1), 2), Throws.Exception.Message.Contains("Mismatch in output version"));
            Assert.That(() => set.Spend(tx2Output1.Spend(1), 2), Throws.Exception.Message.Contains("Mismatch in output version"));

            Assert.That(SortAndFormat(set.ExistingSpentOutputs), Is.Empty);
            Assert.That(SortAndFormat(set.CreatedSpentOutputs), Is.Empty);

            set.Spend(tx1Output1, 2);
            set.Spend(tx2Output1, 2);

            Assert.That(SortAndFormat(set.ExistingSpentOutputs), Is.EqualTo(SortAndFormat(new[] {tx1Output1.Spend(2)})));
            Assert.That(SortAndFormat(set.CreatedSpentOutputs), Is.EqualTo(SortAndFormat(new[] {tx2Output1.Spend(2)})));
        }

        private string[] SortAndFormat(IEnumerable<UtxoOutput> outputs)
        {
            return outputs.Select(Format).OrderBy(t => t).ToArray();
        }

        private static string Format(UtxoOutput output)
        {
            return $"{output.OutputPoint} (H:{output.Height}, S:{output.SpentHeight}, V:{output.Value})";
        }
    }
}