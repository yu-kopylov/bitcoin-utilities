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
    public class TestVirtualDictionary
    {
        private string testFolder;

        [TestFixtureSetUp]
        public void Setup()
        {
            //todo: use one [SetUpFixture] for all test to delete files
            testFolder = Path.GetFullPath("tmp-test-VD");

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
            //todo: use temp file
            //todo: is *.tbl extension save (does not cause problems like *.sdb)
            using (VirtualDictionary dict = VirtualDictionary.Open(Path.Combine(testFolder, "perf.tbl"), 20, 8))
            {
                Stopwatch sw = Stopwatch.StartNew();

                using (var tx = dict.BeginTransaction())
                {
                    for (int i = 0; i < 1000000; i++)
                    {
                        tx.AddOrUpdate(CreateKey(i), BitConverter.GetBytes((long) i));
                    }
                    tx.Commit();
                }

                Console.WriteLine("Insert took: {0}ms.", sw.ElapsedMilliseconds);
                sw.Restart();

                using (var tx = dict.BeginTransaction())
                {
                    for (int i = 1000000; i < 2000000; i++)
                    {
                        tx.AddOrUpdate(CreateKey(i), BitConverter.GetBytes((long) i));
                    }
                    tx.Commit();
                }

                Console.WriteLine("Insert took: {0}ms.", sw.ElapsedMilliseconds);
                sw.Restart();

                using (var tx = dict.BeginTransaction())
                {
                    for (int i = 1500000; i < 2500000; i++)
                    {
                        tx.AddOrUpdate(CreateKey(i), BitConverter.GetBytes((long) i));
                    }
                    tx.Commit();
                }

                Console.WriteLine("Insert with update took: {0}ms.", sw.ElapsedMilliseconds);
                sw.Restart();

                using (var tx = dict.BeginTransaction())
                {
                    for (int i = 20000; i < 25000; i++)
                    {
                        tx.AddOrUpdate(CreateKey(i), BitConverter.GetBytes((long) i));
                    }
                    tx.Commit();
                }

                Console.WriteLine("Small update took: {0}ms.", sw.ElapsedMilliseconds);
                sw.Restart();

                using (var tx = dict.BeginTransaction())
                {
                    for (int i = 2500000; i < 2505000; i++)
                    {
                        tx.AddOrUpdate(CreateKey(i), BitConverter.GetBytes((long) i));
                    }
                    tx.Commit();
                }

                Console.WriteLine("Small insert took: {0}ms.", sw.ElapsedMilliseconds);
                sw.Restart();

                using (var tx = dict.BeginTransaction())
                {
                    List<byte[]> keys = new List<byte[]>();
                    for (int i = 30000; i < 35000; i++)
                    {
                        keys.Add(CreateKey(i));
                    }

                    Dictionary<byte[], byte[]> values = tx.Find(keys);
                    Assert.That(values.Count == keys.Count);

                    for (int i = 30000; i < 35000; i++)
                    {
                        byte[] key = CreateKey(i);
                        Assert.That(ByteArrayComparer.Instance.Equals(values[key], BitConverter.GetBytes((long) i)));
                    }

                    tx.Commit();
                }

                Console.WriteLine("Lookup existing took: {0}ms.", sw.ElapsedMilliseconds);
                sw.Restart();

                using (var tx = dict.BeginTransaction())
                {
                    List<byte[]> keys = new List<byte[]>();
                    for (int i = 2505000; i < 2510000; i++)
                    {
                        keys.Add(CreateKey(i));
                    }

                    Dictionary<byte[], byte[]> values = tx.Find(keys);
                    Assert.That(values.Count == 0);

                    tx.Commit();
                }

                Console.WriteLine("Lookup missing took: {0}ms.", sw.ElapsedMilliseconds);
                sw.Restart();

                for (int j = 0; j < 10; j++)
                {
                    Stopwatch innerSw = Stopwatch.StartNew();
                    using (var tx = dict.BeginTransaction())
                    {
                        List<byte[]> keys = new List<byte[]>();
                        for (int i = j*100000; i < j*100000 + 2000; i++)
                        {
                            keys.Add(CreateKey(i));
                        }
                        for (int i = 2505000 + (j - 1)*2500; i < 2505500 + (j - 1)*2500; i++)
                        {
                            keys.Add(CreateKey(i));
                        }
                        for (int i = 2505000 + j*2500; i < 2505000 + (j + 1)*2500; i++)
                        {
                            keys.Add(CreateKey(i));
                        }

                        Dictionary<byte[], byte[]> values = tx.Find(keys);
                        Assert.That(values.Count == 2500);

                        for (int i = 2505000 + j * 2500; i < 2505000 + (j + 1) * 2500; i++)
                        {
                            tx.AddOrUpdate(CreateKey(i), BitConverter.GetBytes((long)i));
                        }

                        tx.Commit();
                    }
                    Console.WriteLine("\tMix(Lookup: (Old: 2000, Recent: 500, Mising:2500), Add New: 2500) took {0}ms.", innerSw.ElapsedMilliseconds);
                }

                Console.WriteLine("Mix(Lookup: (Old: 2000, Recent: 500, Mising:2500), Add New: 2500) x 10 took: {0}ms.", sw.ElapsedMilliseconds);
                sw.Restart();
            }
        }

        [Test]
        public void TestSimpleAdd()
        {
            using (VirtualDictionary dict = VirtualDictionary.Open(Path.Combine(testFolder, "TestSimpleAdd.tbl"), 1, 1))
            {
                using (var tx = dict.BeginTransaction())
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

                using (var tx = dict.BeginTransaction())
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