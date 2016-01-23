using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BitcoinUtilities;
using BitcoinUtilities.Collections;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Collections
{
    [TestFixture]
    public class TestVirtualHashTable
    {
        private string testFolder;

        [SetUp]
        public void Setup()
        {
            testFolder = Path.GetFullPath("tmp-test-VHT");

            Console.WriteLine("Removing files in the test folder: : {0}", testFolder);

            Directory.CreateDirectory(testFolder);
            foreach (string pattern in new string[] {"*.tbl", "*.tbl-wal"})
            {
                foreach (string filename in Directory.GetFiles(testFolder, pattern, SearchOption.TopDirectoryOnly))
                {
                    Console.WriteLine("Removing: {0}", filename);
                    File.Delete(filename);
                }
            }
        }

        [Test]
        [Explicit]
        public void TestPerformance()
        {
            VHTSettings tableSettings = new VHTSettings();
            //todo: use temp file
            //todo: is *.tbl extension save (does not cause problems like *.sdb)
            tableSettings.Filename = Path.Combine(testFolder, "perf.tbl");
            tableSettings.KeyLength = 20;
            tableSettings.ValueLength = 8;
            using (VirtualHashTable table = VirtualHashTable.Open(tableSettings))
            {
                Stopwatch sw = Stopwatch.StartNew();

                using (var tx = table.BeginTransaction())
                {
                    for (int i = 0; i < 1000000; i++)
                    {
                        tx.AddOrUpdate(CreateKey(i), BitConverter.GetBytes((long) i));
                    }
                    tx.Commit();
                }

                Console.WriteLine("Insert took: {0}ms.", sw.ElapsedMilliseconds);
                sw.Restart();

                using (var tx = table.BeginTransaction())
                {
                    for (int i = 1000000; i < 2000000; i++)
                    {
                        tx.AddOrUpdate(CreateKey(i), BitConverter.GetBytes((long) i));
                    }
                    tx.Commit();
                }

                Console.WriteLine("Insert took: {0}ms.", sw.ElapsedMilliseconds);
                sw.Restart();

                using (var tx = table.BeginTransaction())
                {
                    for (int i = 1900000; i < 2100000; i++)
                    {
                        tx.AddOrUpdate(CreateKey(i), BitConverter.GetBytes((long) i));
                    }
                    tx.Commit();
                }

                Console.WriteLine("Insert with update took: {0}ms.", sw.ElapsedMilliseconds);
                sw.Restart();

                using (var tx = table.BeginTransaction())
                {
                    for (int i = 15000; i < 20000; i++)
                    {
                        tx.AddOrUpdate(CreateKey(i), BitConverter.GetBytes((long) i));
                    }
                    tx.Commit();
                }

                Console.WriteLine("Small update took: {0}ms.", sw.ElapsedMilliseconds);
                sw.Restart();

                using (var tx = table.BeginTransaction())
                {
                    List<byte[]> keys = new List<byte[]>();
                    for (int i = 15000; i < 20000; i++)
                    {
                        keys.Add(CreateKey(i));
                    }

                    keys.Add(CreateKey(999999999));

                    Dictionary<byte[], byte[]> values = tx.Find(keys);
                    Assert.That(values.Count == keys.Count - 1);

                    for (int i = 15000; i < 20000; i++)
                    {
                        byte[] key = CreateKey(i);
                        Assert.That(ByteArrayComparer.Instance.Equals(values[key], BitConverter.GetBytes((long) i)));
                    }

                    tx.Commit();
                }

                Console.WriteLine("Lookup took: {0}ms.", sw.ElapsedMilliseconds);
                sw.Restart();
            }
        }

        [Test]
        public void TestSimpleAdd()
        {
            VHTSettings settings = new VHTSettings();
            settings.Filename = Path.Combine(testFolder, "TestSimpleAdd.tbl");
            settings.KeyLength = 1;
            settings.ValueLength = 1;

            using (VirtualHashTable table = VirtualHashTable.Open(settings))
            {
                using (var tx = table.BeginTransaction())
                {
                    Dictionary<byte[], byte[]> foundValues = tx.Find(new List<byte[]> {new byte[] {1}});
                    Assert.That(foundValues, Is.Empty);

                    tx.AddOrUpdate(new byte[] {1}, new byte[] {1});

                    foundValues = tx.Find(new List<byte[]> {new byte[] {1}});
                    Assert.That(foundValues.Count, Is.EqualTo(1));
                    Assert.That(foundValues[new byte[] {1}], Is.EqualTo(new byte[] {1}));

                    tx.Commit();

                    foundValues = tx.Find(new List<byte[]> {new byte[] {1}});
                    Assert.That(foundValues.Count, Is.EqualTo(1));
                    Assert.That(foundValues[new byte[] {1}], Is.EqualTo(new byte[] {1}));
                }

                using (var tx = table.BeginTransaction())
                {
                    Dictionary<byte[], byte[]> foundValues = tx.Find(new List<byte[]> {new byte[] {1}});
                    Assert.That(foundValues.Count, Is.EqualTo(1));
                    Assert.That(foundValues[new byte[] {1}], Is.EqualTo(new byte[] {1}));

                    tx.Commit();
                }
            }
        }

        private static byte[] CreateKey(int val)
        {
            byte[] key = new byte[20];
            for (int i = 0; i < 20; i++)
            {
                key[i] = (byte) ((val >> 8*(i%4))*(31 + i/4*8) + (val >> 8*((i + 1)%4))*(33 + i/4*8));
            }
            return key;
        }
    }
}