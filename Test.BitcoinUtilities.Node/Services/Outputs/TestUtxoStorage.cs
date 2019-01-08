using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BitcoinUtilities;
using BitcoinUtilities.Node.Services.Outputs;
using NUnit.Framework;
using TestUtilities;

namespace Test.BitcoinUtilities.Node.Services.Outputs
{
    [TestFixture]
    public class TestUtxoStorage
    {
        // todo: test revert and truncate, including revert to future

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

            UtxoOutput header0Tx1Out0 = new UtxoOutput(CreateOutPoint(header0Tx1, 0), 0, 100, new byte[] {0, 1, 0}, -1);
            UtxoOutput header0Tx1Out1 = new UtxoOutput(CreateOutPoint(header0Tx1, 1), 0, 200, new byte[] {0, 1, 1}, -1);
            UtxoOutput header0Tx2Out0 = new UtxoOutput(CreateOutPoint(header0Tx2, 0), 0, 300, new byte[] {0, 2, 0}, -1);
            UtxoOutput header0Tx2Out1 = new UtxoOutput(CreateOutPoint(header0Tx2, 1), 0, 400, new byte[] {0, 2, 1}, -1);

            UtxoOutput header1Tx1Out0 = new UtxoOutput(CreateOutPoint(header1Tx1, 0), 1, 500, new byte[] {1, 1, 0}, -1);
            UtxoOutput header1Tx2Out0 = new UtxoOutput(CreateOutPoint(header1Tx2, 0), 1, 600, new byte[] {1, 2, 0}, -1);
            UtxoOutput header1Tx2Out1 = new UtxoOutput(CreateOutPoint(header1Tx2, 1), 1, 700, new byte[] {1, 2, 1}, -1);

            UtxoOutput header2Tx1Out0 = new UtxoOutput(CreateOutPoint(header2Tx1, 0), 2, 0x1234567890123450ul, new byte[] {1, 2, 0}, -1);
            UtxoOutput header2Tx1Out1 = new UtxoOutput(CreateOutPoint(header2Tx1, 1), 2, 0xF234567890123450ul, new byte[] {2, 1, 1}, -1);

            var allOutputs = new List<UtxoOutput>
            {
                header0Tx1Out0,
                header0Tx1Out1,
                header0Tx2Out0,
                header0Tx2Out1,

                header1Tx1Out0,
                header1Tx2Out0,
                header1Tx2Out1,

                header2Tx1Out0,
                header2Tx1Out1
            };

            using (UtxoStorage storage1 = UtxoStorage.Open(filename))
            {
                var update0 = new UtxoUpdate(0, header0, new byte[32]);
                update0.NewOutputs.Add(header0Tx1Out0);
                update0.NewOutputs.Add(header0Tx1Out1);
                update0.NewOutputs.Add(header0Tx2Out0);
                update0.NewOutputs.Add(header0Tx2Out1);

                storage1.Update(update0);

                var update1 = new UtxoUpdate(1, header1, header0);
                update1.SpentOutputs.Add(header0Tx1Out0);
                update1.SpentOutputs.Add(header0Tx2Out1);
                update1.NewOutputs.Add(header1Tx1Out0);
                update1.NewOutputs.Add(header1Tx2Out0);
                update1.NewOutputs.Add(header1Tx2Out1);

                storage1.Update(update1);

                var update2 = new UtxoUpdate(2, header2, header1);
                update2.SpentOutputs.Add(header0Tx1Out1);
                update2.SpentOutputs.Add(header1Tx2Out0);
                update2.NewOutputs.Add(header2Tx1Out0);
                update2.NewOutputs.Add(header2Tx1Out1);

                storage1.Update(update2);
            }

            using (UtxoStorage storage2 = UtxoStorage.Open(filename))
            {
                List<byte[]> allOutputPoints = allOutputs.Select(o => o.OutputPoint).ToList();

                Assert.AreEqual
                (
                    new UtxoOutput[]
                    {
                        header0Tx2Out0,
                        header1Tx1Out0,
                        header1Tx2Out1,
                        header2Tx1Out0,
                        header2Tx1Out1
                    }.OrderBy(o => o.Height).ThenBy(o => HexUtils.GetString(o.OutputPoint)).Select(FormatOutput).ToArray(),
                    storage2.GetUnspentOutputs(
                        allOutputPoints
                    ).OrderBy(o => o.Height).ThenBy(o => HexUtils.GetString(o.OutputPoint)).Select(FormatOutput).ToArray()
                );

                storage2.RevertTo(header1);

                Assert.AreEqual
                (
                    new UtxoOutput[]
                    {
                        header0Tx2Out0,
                        header1Tx1Out0,
                        header1Tx2Out1,
                        header0Tx1Out1,
                        header1Tx2Out0
                    }.OrderBy(o => o.Height).ThenBy(o => HexUtils.GetString(o.OutputPoint)).Select(FormatOutput).ToArray(),
                    storage2.GetUnspentOutputs(
                        allOutputPoints
                    ).OrderBy(o => o.Height).ThenBy(o => HexUtils.GetString(o.OutputPoint)).Select(FormatOutput).ToArray()
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

            UtxoOutput header0Tx1Out0 = new UtxoOutput(CreateOutPoint(header0Tx1, 0), 0, 100, new byte[] {0, 1, 0}, -1);
            UtxoOutput header0Tx1Out1 = new UtxoOutput(CreateOutPoint(header0Tx1, 1), 0, 200, new byte[] {0, 1, 1}, -1);
            UtxoOutput header0Tx2Out0 = new UtxoOutput(CreateOutPoint(header0Tx2, 0), 0, 300, new byte[] {0, 2, 0}, -1);
            UtxoOutput header0Tx2Out1 = new UtxoOutput(CreateOutPoint(header0Tx2, 1), 0, 400, new byte[] {0, 2, 1}, -1);

            UtxoOutput header1Tx1Out0 = new UtxoOutput(CreateOutPoint(header1Tx1, 0), 1, 500, new byte[] {1, 1, 0}, -1);
            UtxoOutput header1Tx2Out0 = new UtxoOutput(CreateOutPoint(header1Tx2, 0), 1, 600, new byte[] {1, 2, 0}, -1);
            UtxoOutput header1Tx2Out1 = new UtxoOutput(CreateOutPoint(header1Tx2, 1), 1, 700, new byte[] {1, 2, 1}, -1);

            UtxoOutput header2Tx1Out0 = new UtxoOutput(CreateOutPoint(header2Tx1, 0), 2, 0x1234567890123450ul, new byte[] {1, 2, 0}, -1);
            UtxoOutput header2Tx1Out1 = new UtxoOutput(CreateOutPoint(header2Tx1, 1), 2, 0xF234567890123450ul, new byte[] {2, 1, 1}, -1);

            var allOutputs = new List<UtxoOutput>
            {
                header0Tx1Out0,
                header0Tx1Out1,
                header0Tx2Out0,
                header0Tx2Out1,

                header1Tx1Out0,
                header1Tx2Out0,
                header1Tx2Out1,

                header2Tx1Out0,
                header2Tx1Out1
            };

            using (UtxoStorage storage1 = UtxoStorage.Open(filename))
            {
                var update0 = new UtxoUpdate(0, header0, new byte[32]);
                update0.NewOutputs.Add(header0Tx1Out0);
                update0.NewOutputs.Add(header0Tx1Out1);
                update0.NewOutputs.Add(header0Tx2Out0);
                update0.NewOutputs.Add(header0Tx2Out1);


                var update1 = new UtxoUpdate(1, header1, header0);
                update1.SpentOutputs.Add(header0Tx1Out0);
                update1.SpentOutputs.Add(header0Tx2Out1);
                update1.NewOutputs.Add(header1Tx1Out0);
                update1.NewOutputs.Add(header1Tx2Out0);
                update1.NewOutputs.Add(header1Tx2Out1);


                var update2 = new UtxoUpdate(2, header2, header1);
                update2.SpentOutputs.Add(header0Tx1Out1);
                update2.SpentOutputs.Add(header1Tx2Out0);
                update2.NewOutputs.Add(header2Tx1Out0);
                update2.NewOutputs.Add(header2Tx1Out1);


                storage1.Update(new UtxoUpdate[] {update0, update1, update2});
            }

            using (UtxoStorage storage2 = UtxoStorage.Open(filename))
            {
                List<byte[]> allOutputPoints = allOutputs.Select(o => o.OutputPoint).ToList();

                Assert.AreEqual
                (
                    new UtxoOutput[]
                    {
                        header0Tx2Out0,
                        header1Tx1Out0,
                        header1Tx2Out1,
                        header2Tx1Out0,
                        header2Tx1Out1
                    }.OrderBy(o => o.Height).ThenBy(o => HexUtils.GetString(o.OutputPoint)).Select(FormatOutput).ToArray(),
                    storage2.GetUnspentOutputs(
                        allOutputPoints
                    ).OrderBy(o => o.Height).ThenBy(o => HexUtils.GetString(o.OutputPoint)).Select(FormatOutput).ToArray()
                );

                storage2.RevertTo(header1);

                Assert.AreEqual
                (
                    new UtxoOutput[]
                    {
                        header0Tx2Out0,
                        header1Tx1Out0,
                        header1Tx2Out1,
                        header0Tx1Out1,
                        header1Tx2Out0
                    }.OrderBy(o => o.Height).ThenBy(o => HexUtils.GetString(o.OutputPoint)).Select(FormatOutput).ToArray(),
                    storage2.GetUnspentOutputs(
                        allOutputPoints
                    ).OrderBy(o => o.Height).ThenBy(o => HexUtils.GetString(o.OutputPoint)).Select(FormatOutput).ToArray()
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
                            byte[] outPoint = CreateOutPoint(txHash, outputIndex);
                            UtxoOutput output = new UtxoOutput(outPoint, height, 123, new byte[] {32}, -1);
                            update.NewOutputs.Add(output);
                        }
                    }

                    if (existingOutputs.Count > 0)
                    {
                        int spentCount = random.Next(Math.Min(existingOutputs.Count, 2000));
                        for (int i = 0; i < spentCount; i++)
                        {
                            int outputIndex = random.Next(existingOutputs.Count);

                            update.SpentOutputs.Add(existingOutputs[outputIndex]);

                            existingOutputs[outputIndex] = existingOutputs[existingOutputs.Count - 1];
                            existingOutputs.RemoveAt(existingOutputs.Count - 1);
                        }

                        Stopwatch sw = Stopwatch.StartNew();
                        int fountOutputs = storage.GetUnspentOutputs(update.SpentOutputs.Select(o => o.OutputPoint)).Count;
                        File.AppendAllText(logFilename, $"Read {fountOutputs} of {update.SpentOutputs.Count} outputs to spend in header {height} in {sw.ElapsedMilliseconds} ms.\r\n");
                    }

                    while (existingOutputs.Count > 200_000)
                    {
                        int outputIndex = random.Next(existingOutputs.Count);
                        existingOutputs[outputIndex] = existingOutputs[existingOutputs.Count - 1];
                        existingOutputs.RemoveAt(existingOutputs.Count - 1);
                    }

                    pendingUpdates.Add(update);
                    existingOutputs.AddRange(update.NewOutputs);

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

        private string FormatOutput(UtxoOutput output)
        {
            return string.Join(", ", new string[]
            {
                $"Height: {output.Height}",
                $"OutputPoint: {HexUtils.GetString(output.OutputPoint)}",
                $"Value: {output.Value}",
                $"Script: {HexUtils.GetString(output.Script)}",
                $"SpentHeight: {output.SpentHeight}"
            });
        }

        private byte[] CreateOutPoint(byte[] txHash, int outputIndex)
        {
            int hashLength = txHash.Length;
            byte[] outPoint = new byte[hashLength + 4];
            Array.Copy(txHash, outPoint, hashLength);
            outPoint[hashLength + 0] = (byte) outputIndex;
            outPoint[hashLength + 1] = (byte) (outputIndex >> 8);
            outPoint[hashLength + 2] = (byte) (outputIndex >> 16);
            outPoint[hashLength + 3] = (byte) (outputIndex >> 24);
            return outPoint;
        }
    }
}