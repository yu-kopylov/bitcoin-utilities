using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BitcoinUtilities;
using BitcoinUtilities.Node.Modules.Outputs;
using BitcoinUtilities.P2P.Primitives;
using NUnit.Framework;
using TestUtilities;

namespace Test.BitcoinUtilities.Node.Modules.Outputs
{
    [TestFixture]
    public class TestUtxoStorage
    {
        // todo: test revert and truncate, including revert to future
        // todo: test GetLastHeader
        // todo: test outputs that was created and spent in same block

        [Test]
        public void TestSaveAndReopen()
        {
            string testFolder = TestUtils.PrepareTestFolder("*.db");
            string filename = Path.Combine(testFolder, "utxo.db");

            byte[] header0 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("header0"));
            byte[] header0Tx1 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("header0Tx1"));
            byte[] header0Tx2 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("header0Tx2"));

            byte[] header1 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("header1"));
            byte[] header1Tx1 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("header1Tx1"));
            byte[] header1Tx2 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("header1Tx2"));

            byte[] header2 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("header2"));
            byte[] header2Tx1 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("header2Tx1"));

            UtxoOutput header0Tx1Out0 = new UtxoOutput(new TxOutPoint(header0Tx1, 0), 100, new byte[] {0, 1, 0});
            UtxoOutput header0Tx1Out1 = new UtxoOutput(new TxOutPoint(header0Tx1, 1), 200, new byte[] {0, 1, 1});
            UtxoOutput header0Tx2Out0 = new UtxoOutput(new TxOutPoint(header0Tx2, 0), 300, new byte[] {0, 2, 0});
            UtxoOutput header0Tx2Out1 = new UtxoOutput(new TxOutPoint(header0Tx2, 1), 400, new byte[] {0, 2, 1});

            UtxoOutput header1Tx1Out0 = new UtxoOutput(new TxOutPoint(header1Tx1, 0), 500, new byte[] {1, 1, 0});
            UtxoOutput header1Tx2Out0 = new UtxoOutput(new TxOutPoint(header1Tx2, 0), 600, new byte[] {1, 2, 0});
            UtxoOutput header1Tx2Out1 = new UtxoOutput(new TxOutPoint(header1Tx2, 1), 700, new byte[] {1, 2, 1});

            UtxoOutput header2Tx1Out0 = new UtxoOutput(new TxOutPoint(header2Tx1, 0), 0x1234567890123450ul, new byte[] {1, 2, 0});
            UtxoOutput header2Tx1Out1 = new UtxoOutput(new TxOutPoint(header2Tx1, 1), 0xF234567890123450ul, new byte[] {2, 1, 1});

            var allTxHashes = new HashSet<byte[]>(ByteArrayComparer.Instance)
            {
                header0Tx1,
                header0Tx2,

                header1Tx1,
                header1Tx2,

                header2Tx1
            };

            using (UtxoStorage storage1 = UtxoStorage.Open(filename))
            {
                var update0 = new UtxoUpdate(0, header0, new byte[32]);
                update0.Operations.Add(UtxoOperation.Create(header0Tx1Out0));
                update0.Operations.Add(UtxoOperation.Create(header0Tx1Out1));
                update0.Operations.Add(UtxoOperation.Create(header0Tx2Out0));
                update0.Operations.Add(UtxoOperation.Create(header0Tx2Out1));

                storage1.Update(update0);

                var update1 = new UtxoUpdate(1, header1, header0);
                update1.Operations.Add(UtxoOperation.Spend(header0Tx1Out0));
                update1.Operations.Add(UtxoOperation.Spend(header0Tx2Out1));
                update1.Operations.Add(UtxoOperation.Create(header1Tx1Out0));
                update1.Operations.Add(UtxoOperation.Create(header1Tx2Out0));
                update1.Operations.Add(UtxoOperation.Create(header1Tx2Out1));

                storage1.Update(update1);

                var update2 = new UtxoUpdate(2, header2, header1);
                update2.Operations.Add(UtxoOperation.Spend(header0Tx1Out1));
                update2.Operations.Add(UtxoOperation.Spend(header1Tx2Out0));
                update2.Operations.Add(UtxoOperation.Create(header2Tx1Out0));
                update2.Operations.Add(UtxoOperation.Create(header2Tx1Out1));

                storage1.Update(update2);
            }

            using (UtxoStorage storage2 = UtxoStorage.Open(filename))
            {
                Assert.AreEqual
                (
                    SortAndFormat(new UtxoOutput[]
                    {
                        header0Tx2Out0,
                        header1Tx1Out0,
                        header1Tx2Out1,
                        header2Tx1Out0,
                        header2Tx1Out1
                    }),
                    SortAndFormat(storage2.GetUnspentOutputs(allTxHashes))
                );

                storage2.RevertTo(header1);

                Assert.AreEqual
                (
                    SortAndFormat(new UtxoOutput[]
                    {
                        header0Tx2Out0,
                        header1Tx1Out0,
                        header1Tx2Out1,
                        header0Tx1Out1,
                        header1Tx2Out0
                    }),
                    SortAndFormat(storage2.GetUnspentOutputs(allTxHashes))
                );
            }
        }

        [Test]
        public void TestSaveAndReopenWithAggregateUpdate()
        {
            string testFolder = TestUtils.PrepareTestFolder("*.db");
            string filename = Path.Combine(testFolder, "utxo.db");

            byte[] header0 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("header0"));
            byte[] header0Tx1 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("header0Tx1"));
            byte[] header0Tx2 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("header0Tx2"));

            byte[] header1 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("header1"));
            byte[] header1Tx1 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("header1Tx1"));
            byte[] header1Tx2 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("header1Tx2"));

            byte[] header2 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("header2"));
            byte[] header2Tx1 = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("header2Tx1"));

            UtxoOutput header0Tx1Out0 = new UtxoOutput(new TxOutPoint(header0Tx1, 0), 100, new byte[] {0, 1, 0});
            UtxoOutput header0Tx1Out1 = new UtxoOutput(new TxOutPoint(header0Tx1, 1), 200, new byte[] {0, 1, 1});
            UtxoOutput header0Tx2Out0 = new UtxoOutput(new TxOutPoint(header0Tx2, 0), 300, new byte[] {0, 2, 0});
            UtxoOutput header0Tx2Out1 = new UtxoOutput(new TxOutPoint(header0Tx2, 1), 400, new byte[] {0, 2, 1});

            UtxoOutput header1Tx1Out0 = new UtxoOutput(new TxOutPoint(header1Tx1, 0), 500, new byte[] {1, 1, 0});
            UtxoOutput header1Tx2Out0 = new UtxoOutput(new TxOutPoint(header1Tx2, 0), 600, new byte[] {1, 2, 0});
            UtxoOutput header1Tx2Out1 = new UtxoOutput(new TxOutPoint(header1Tx2, 1), 700, new byte[] {1, 2, 1});

            UtxoOutput header2Tx1Out0 = new UtxoOutput(new TxOutPoint(header2Tx1, 0), 0x1234567890123450ul, new byte[] {1, 2, 0});
            UtxoOutput header2Tx1Out1 = new UtxoOutput(new TxOutPoint(header2Tx1, 1), 0xF234567890123450ul, new byte[] {2, 1, 1});

            var allTxHashes = new HashSet<byte[]>(ByteArrayComparer.Instance)
            {
                header0Tx1,
                header0Tx2,

                header1Tx1,
                header1Tx2,

                header2Tx1
            };

            using (UtxoStorage storage1 = UtxoStorage.Open(filename))
            {
                var update0 = new UtxoUpdate(0, header0, new byte[32]);
                update0.Operations.Add(UtxoOperation.Create(header0Tx1Out0));
                update0.Operations.Add(UtxoOperation.Create(header0Tx1Out1));
                update0.Operations.Add(UtxoOperation.Create(header0Tx2Out0));
                update0.Operations.Add(UtxoOperation.Create(header0Tx2Out1));


                var update1 = new UtxoUpdate(1, header1, header0);
                update1.Operations.Add(UtxoOperation.Spend(header0Tx1Out0));
                update1.Operations.Add(UtxoOperation.Spend(header0Tx2Out1));
                update1.Operations.Add(UtxoOperation.Create(header1Tx1Out0));
                update1.Operations.Add(UtxoOperation.Create(header1Tx2Out0));
                update1.Operations.Add(UtxoOperation.Create(header1Tx2Out1));


                var update2 = new UtxoUpdate(2, header2, header1);
                update2.Operations.Add(UtxoOperation.Spend(header0Tx1Out1));
                update2.Operations.Add(UtxoOperation.Spend(header1Tx2Out0));
                update2.Operations.Add(UtxoOperation.Create(header2Tx1Out0));
                update2.Operations.Add(UtxoOperation.Create(header2Tx1Out1));


                storage1.Update(new UtxoUpdate[] {update0, update1, update2});
            }

            using (UtxoStorage storage2 = UtxoStorage.Open(filename))
            {
                Assert.AreEqual
                (
                    SortAndFormat(new UtxoOutput[]
                    {
                        header0Tx2Out0,
                        header1Tx1Out0,
                        header1Tx2Out1,
                        header2Tx1Out0,
                        header2Tx1Out1
                    }),
                    SortAndFormat(storage2.GetUnspentOutputs(allTxHashes))
                );

                storage2.RevertTo(header1);

                Assert.AreEqual
                (
                    SortAndFormat(new UtxoOutput[]
                    {
                        header0Tx2Out0,
                        header1Tx1Out0,
                        header1Tx2Out1,
                        header0Tx1Out1,
                        header1Tx2Out0
                    }),
                    SortAndFormat(storage2.GetUnspentOutputs(allTxHashes))
                );
            }
        }

        [Test]
        [Explicit]
        public void GenerateLargeStorage()
        {
            string testFolder = TestUtils.PrepareTestFolder("*.db", "utxo.log");
            string filename = Path.Combine(testFolder, "utxo.db");
            string logFilename = Path.Combine(testFolder, "utxo.log");

            Random random = new Random();
            List<UtxoUpdate> pendingUpdates = new List<UtxoUpdate>();

            using (UtxoStorage storage = UtxoStorage.Open(filename))
            {
                List<UtxoOutput> existingOutputs = new List<UtxoOutput>();

                byte[] parentHash = new byte[32];
                for (int height = 0; height < 100_000; height++)
                {
                    byte[] headerHash = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes($"Header: {height}"));
                    UtxoUpdate update = new UtxoUpdate(height, headerHash, parentHash);
                    parentHash = headerHash;

                    for (int txNum = 0; txNum < 1000; txNum++)
                    {
                        byte[] txHash = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes($"Header: {height}, Tx: {txNum}"));
                        for (int outputIndex = 0; outputIndex < 2; outputIndex++)
                        {
                            TxOutPoint outPoint = new TxOutPoint(txHash, outputIndex);
                            UtxoOutput output = new UtxoOutput(outPoint, 123, new byte[] {32});
                            update.Operations.Add(UtxoOperation.Create(output));
                        }
                    }

                    if (existingOutputs.Count > 0)
                    {
                        int spentCount = random.Next(Math.Min(existingOutputs.Count, 2000));
                        for (int i = 0; i < spentCount; i++)
                        {
                            int outputIndex = random.Next(existingOutputs.Count);

                            update.Operations.Add(UtxoOperation.Spend(existingOutputs[outputIndex]));

                            existingOutputs[outputIndex] = existingOutputs[existingOutputs.Count - 1];
                            existingOutputs.RemoveAt(existingOutputs.Count - 1);
                        }

                        var spendingOperations = update.Operations.Where(o => o.Spent).ToList();
                        var requiredOutPoints = new HashSet<TxOutPoint>(spendingOperations.Select(o => o.Output.OutPoint));
                        var requiredTxHashes = new HashSet<byte[]>(requiredOutPoints.Select(o => o.Hash), ByteArrayComparer.Instance);
                        // emulating searches for already spent transactions
                        requiredTxHashes.Add(CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes($"Header: {height}, Tx: Fake1")));
                        requiredTxHashes.Add(CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes($"Header: {height}, Tx: Fake2")));
                        requiredTxHashes.Add(CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes($"Header: {height}, Tx: Fake3")));
                        requiredTxHashes.Add(CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes($"Header: {height}, Tx: Fake4")));

                        Stopwatch sw = Stopwatch.StartNew();

                        IReadOnlyCollection<UtxoOutput> foundOutputs = storage.GetUnspentOutputs(requiredTxHashes)
                            .Where(o => requiredOutPoints.Contains(o.OutPoint))
                            .ToList();
                        File.AppendAllText(logFilename,
                            $"Read {foundOutputs.Count} of {spendingOperations.Count} outputs" +
                            $" to spend in header {height} in {sw.ElapsedMilliseconds} ms.\r\n");

                        sw.Restart();

                        Dictionary<TxOutPoint, UtxoOutput> foundOutputsByOutPoint = foundOutputs.ToDictionary(o => o.OutPoint);

                        foreach (UtxoUpdate pendingUpdate in pendingUpdates)
                        {
                            foreach (var operation in pendingUpdate.Operations.Where(op => requiredOutPoints.Contains(op.Output.OutPoint)))
                            {
                                if (operation.Spent)
                                {
                                    if (!foundOutputsByOutPoint.Remove(operation.Output.OutPoint))
                                    {
                                        throw new InvalidOperationException("Spending non-existing output.");
                                    }
                                }
                                else
                                {
                                    foundOutputsByOutPoint.Add(operation.Output.OutPoint, operation.Output);
                                }
                            }
                        }

                        File.AppendAllText(logFilename,
                            $"Constructed set of {foundOutputsByOutPoint.Count} outputs by replaying {pendingUpdates.Count} updates" +
                            $" on fetched outputs in {sw.ElapsedMilliseconds} ms.\r\n");
                    }

                    while (existingOutputs.Count > 200_000)
                    {
                        int outputIndex = random.Next(existingOutputs.Count);
                        existingOutputs[outputIndex] = existingOutputs[existingOutputs.Count - 1];
                        existingOutputs.RemoveAt(existingOutputs.Count - 1);
                    }

                    pendingUpdates.Add(update);
                    var tempAggregate = new UtxoAggregateUpdate();
                    tempAggregate.Add(update.Operations);
                    existingOutputs.AddRange(tempAggregate.CreatedOutputs);

                    if (pendingUpdates.Count >= 100)
                    {
                        Stopwatch sw = Stopwatch.StartNew();
                        storage.Update(pendingUpdates);
                        File.AppendAllText(logFilename, $"Saved {pendingUpdates.Count} headers up to {height} in {sw.ElapsedMilliseconds} ms.\r\n");

                        pendingUpdates.Clear();
                    }
                }
            }
        }

        private string[] SortAndFormat(IEnumerable<UtxoOutput> outputs)
        {
            return outputs
                .OrderBy(o => HexUtils.GetString(o.OutPoint.Hash))
                .ThenBy(o => o.OutPoint.Index)
                .Select(FormatOutput)
                .ToArray();
        }

        private string FormatOutput(UtxoOutput output)
        {
            return string.Join(", ",
                $"TxHash: {HexUtils.GetString(output.OutPoint.Hash)}",
                $"OutputIndex: {output.OutPoint.Index}",
                $"Value: {output.Value}",
                $"PubkeyScript: {HexUtils.GetString(output.PubkeyScript)}"
            );
        }
    }
}