using System;
using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.P2P;
using BitcoinUtilities.Storage;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Storage
{
    [TestFixture]
    public class TestUnspentOutputsUpdate
    {
        private static readonly byte[] Hash1 = new byte[32];
        private static readonly byte[] Hash2 = new byte[32];

        private static readonly StoredBlock Block1 = new StoredBlockBuilder(GenesisBlock.GetHeader()) {Height = 1}.Build();
        private static readonly StoredBlock Block2 = new StoredBlockBuilder(GenesisBlock.GetHeader()) {Height = 2}.Build();

        static TestUnspentOutputsUpdate()
        {
            Hash1[0] = 1;
            Hash2[0] = 2;
        }

        [Test]
        public void TestFindExistingOutput()
        {
            TestStorage storage = new TestStorage();
            storage.AddUnspentOutput(new UnspentOutput(0, Hash1, 1, 100, new byte[16]));

            UnspentOutputsUpdate update = new UnspentOutputsUpdate(storage);

            Assert.That(update.FindUnspentOutput(Hash1, 0), Is.Null);
            Assert.That(update.FindUnspentOutput(Hash1, 1).Sum, Is.EqualTo(100));
            Assert.That(update.FindUnspentOutput(Hash2, 0), Is.Null);
            Assert.That(update.FindUnspentOutput(Hash2, 1), Is.Null);
        }

        [Test]
        public void TestFindExistingOutputs()
        {
            TestStorage storage = new TestStorage();
            storage.AddUnspentOutput(new UnspentOutput(0, Hash1, 1, 100, new byte[16]));

            UnspentOutputsUpdate update = new UnspentOutputsUpdate(storage);

            Assert.That(update.FindUnspentOutputs(Hash1).Select(o => o.Sum), Is.EquivalentTo(new ulong[] {100}));
            Assert.That(update.FindUnspentOutputs(Hash2), Is.Empty);
        }

        [Test]
        public void TestAdd()
        {
            TestStorage storage = new TestStorage();
            storage.AddUnspentOutput(new UnspentOutput(0, Hash1, 0, 100, new byte[16]));

            UnspentOutputsUpdate update = new UnspentOutputsUpdate(storage);

            Assert.Throws<InvalidOperationException>(() => update.Add(new UnspentOutput(0, Hash1, 0, 200, new byte[16])));
            update.Add(new UnspentOutput(0, Hash1, 1, 201, new byte[16]));
            update.Add(new UnspentOutput(0, Hash2, 0, 300, new byte[16]));
            update.Add(new UnspentOutput(0, Hash2, 1, 301, new byte[16]));

            Assert.Throws<InvalidOperationException>(() => update.Add(new UnspentOutput(0, Hash1, 0, 250, new byte[16])));
            Assert.Throws<InvalidOperationException>(() => update.Add(new UnspentOutput(0, Hash1, 1, 251, new byte[16])));
            Assert.Throws<InvalidOperationException>(() => update.Add(new UnspentOutput(0, Hash2, 0, 350, new byte[16])));
            Assert.Throws<InvalidOperationException>(() => update.Add(new UnspentOutput(0, Hash2, 1, 351, new byte[16])));

            Assert.That(update.FindUnspentOutputs(Hash1).Select(o => o.Sum).ToList(), Is.EquivalentTo(new ulong[] {100, 201}));
            Assert.That(update.FindUnspentOutputs(Hash2).Select(o => o.Sum).ToList(), Is.EquivalentTo(new ulong[] {300, 301}));
            Assert.That(update.FindUnspentOutput(Hash1, 0).Sum, Is.EqualTo(100));
            Assert.That(update.FindUnspentOutput(Hash1, 1).Sum, Is.EqualTo(201));
            Assert.That(update.FindUnspentOutput(Hash2, 0).Sum, Is.EqualTo(300));
            Assert.That(update.FindUnspentOutput(Hash2, 1).Sum, Is.EqualTo(301));

            Assert.That(storage.UnspentOutputs.Select(o => o.Sum), Is.EquivalentTo(new ulong[] {100}));
            Assert.That(storage.SpentOutputs, Is.Empty);

            update.Persist();

            Assert.That(storage.UnspentOutputs.Select(o => o.Sum), Is.EquivalentTo(new ulong[] {100, 201, 300, 301}));
            Assert.That(storage.SpentOutputs, Is.Empty);
        }

        [Test]
        public void TestSpendNonExistent()
        {
            TestStorage storage = new TestStorage();
            storage.AddUnspentOutput(new UnspentOutput(0, Hash1, 0, 100, new byte[16]));

            UnspentOutputsUpdate update = new UnspentOutputsUpdate(storage);

            update.Spend(Hash1, 0, Block1.Height);
            Assert.Throws<InvalidOperationException>(() => update.Spend(Hash1, 0, Block1.Height));
            Assert.Throws<InvalidOperationException>(() => update.Spend(Hash1, 1, Block1.Height));
            Assert.Throws<InvalidOperationException>(() => update.Spend(Hash2, 0, Block1.Height));
            Assert.Throws<InvalidOperationException>(() => update.Spend(Hash2, 1, Block1.Height));

            Assert.That(storage.UnspentOutputs.Select(o => o.Sum), Is.EquivalentTo(new ulong[] {100}));
            Assert.That(storage.SpentOutputs, Is.Empty);

            update.Persist();

            Assert.That(storage.UnspentOutputs, Is.Empty);
            Assert.That(storage.SpentOutputs.Select(o => o.Sum), Is.EquivalentTo(new ulong[] {100}));
        }

        [Test]
        public void TestOverwrite()
        {
            TestStorage storage = new TestStorage();
            storage.AddUnspentOutput(new UnspentOutput(0, Hash1, 0, 100, new byte[16]));
            storage.AddUnspentOutput(new UnspentOutput(0, Hash1, 1, 101, new byte[16]));

            UnspentOutputsUpdate update = new UnspentOutputsUpdate(storage);

            update.Spend(Hash1, 0, Block1.Height);
            update.Spend(Hash1, 1, Block1.Height);
            update.Add(new UnspentOutput(1, Hash1, 0, 200, new byte[16]));

            Assert.That(update.FindUnspentOutputs(Hash1).Select(o => o.Sum), Is.EqualTo(new ulong[] {200}));
            Assert.That(update.FindUnspentOutput(Hash1, 0).Sum, Is.EqualTo(200));
            Assert.That(update.FindUnspentOutput(Hash1, 1), Is.Null);

            Assert.That(storage.UnspentOutputs.Select(o => o.Sum), Is.EquivalentTo(new ulong[] {100, 101}));
            Assert.That(storage.SpentOutputs, Is.Empty);

            update.Persist();

            Assert.That(storage.UnspentOutputs.Select(o => o.Sum), Is.EquivalentTo(new ulong[] {200}));
            Assert.That(storage.SpentOutputs.Select(o => o.Sum), Is.EquivalentTo(new ulong[] {100, 101}));

            update = new UnspentOutputsUpdate(storage);

            update.Spend(Hash1, 0, Block2.Height);
            update.Add(new UnspentOutput(2, Hash1, 0, 300, new byte[16]));
            update.Spend(Hash1, 0, Block2.Height);
            update.Add(new UnspentOutput(2, Hash1, 0, 350, new byte[16]));

            Assert.That(update.FindUnspentOutputs(Hash1).Select(o => o.Sum), Is.EquivalentTo(new ulong[] {350}));
            Assert.That(update.FindUnspentOutput(Hash1, 0).Sum, Is.EqualTo(350));
            Assert.That(update.FindUnspentOutput(Hash1, 1), Is.Null);

            Assert.That(storage.UnspentOutputs.Select(o => o.Sum), Is.EquivalentTo(new ulong[] {200}));
            Assert.That(storage.SpentOutputs.Select(o => o.Sum), Is.EquivalentTo(new ulong[] {100, 101}));

            update.Persist();

            Assert.That(storage.UnspentOutputs.Select(o => o.Sum), Is.EquivalentTo(new ulong[] {350}));
            Assert.That(storage.SpentOutputs.Select(o => o.Sum), Is.EquivalentTo(new ulong[] {100, 101, 200}));
        }

        [Test]
        public void TestDoublePersist()
        {
            TestStorage storage = new TestStorage();

            storage.AddUnspentOutput(new UnspentOutput(0, Hash1, 0, 100, new byte[16]));

            UnspentOutputsUpdate update = new UnspentOutputsUpdate(storage);

            update.Spend(Hash1, 0, Block1.Height);
            update.Add(new UnspentOutput(1, Hash1, 0, 200, new byte[16]));
            update.Add(new UnspentOutput(1, Hash1, 1, 201, new byte[16]));
            update.Add(new UnspentOutput(1, Hash2, 0, 300, new byte[16]));

            Assert.That(storage.UnspentOutputs.Select(o => o.Sum), Is.EquivalentTo(new ulong[] {100}));
            Assert.That(storage.SpentOutputs, Is.Empty);

            update.Persist();

            Assert.That(storage.UnspentOutputs.Select(o => o.Sum), Is.EquivalentTo(new ulong[] {200, 201, 300}));
            Assert.That(storage.SpentOutputs.Select(o => o.Sum), Is.EquivalentTo(new ulong[] {100}));

            update.Persist();

            Assert.That(storage.UnspentOutputs.Select(o => o.Sum), Is.EquivalentTo(new ulong[] {200, 201, 300}));
            Assert.That(storage.SpentOutputs.Select(o => o.Sum), Is.EquivalentTo(new ulong[] {100}));
        }

        public class TestStorage : IUnspentOutputStorage
        {
            private readonly List<UnspentOutput> unspentOutputs = new List<UnspentOutput>();

            private readonly List<SpentOutput> spentOutputs = new List<SpentOutput>();

            public List<UnspentOutput> UnspentOutputs
            {
                get { return unspentOutputs; }
            }

            public List<SpentOutput> SpentOutputs
            {
                get { return spentOutputs; }
            }

            public List<UnspentOutput> FindUnspentOutputs(byte[] transactionHash)
            {
                return unspentOutputs.Where(o => o.TransactionHash.SequenceEqual(transactionHash)).ToList();
            }

            public void AddUnspentOutput(UnspentOutput unspentOutput)
            {
                unspentOutputs.Add(unspentOutput);
            }

            public void RemoveUnspentOutput(byte[] transactionHash, int outputNumber)
            {
                unspentOutputs.RemoveAll(o => o.TransactionHash.SequenceEqual(transactionHash) && o.OutputNumber == outputNumber);
            }

            public void AddSpentOutput(SpentOutput spentOutput)
            {
                spentOutputs.Add(spentOutput);
            }
        }
    }
}