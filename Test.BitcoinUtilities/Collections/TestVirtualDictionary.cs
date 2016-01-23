using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BitcoinUtilities.Collections;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Collections
{
    [TestFixture]
    public class TestVirtualDictionary
    {
        private string testFolder;

        [SetUp]
        public void Setup()
        {
            testFolder = Path.GetFullPath("tmp-test-VD");

            Console.WriteLine("Removing files in the test folder: : {0}", testFolder);

            Directory.CreateDirectory(testFolder);
            foreach (string pattern in new string[]{"*.tbl", "*.tbl-wal"})
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
                    for (int i = 1900000; i < 2100000; i++)
                    {
                        tx.AddOrUpdate(CreateKey(i), BitConverter.GetBytes((long) i));
                    }
                    tx.Commit();
                }

                Console.WriteLine("Insert with update took: {0}ms.", sw.ElapsedMilliseconds);
                sw.Restart();

                using (var tx = dict.BeginTransaction())
                {
                    for (int i = 15000; i < 20000; i++)
                    {
                        tx.AddOrUpdate(CreateKey(i), BitConverter.GetBytes((long) i));
                    }
                    tx.Commit();
                }

                Console.WriteLine("Small update took: {0}ms.", sw.ElapsedMilliseconds);
                sw.Restart();

                using (var tx = dict.BeginTransaction())
                {
                    List<byte[]> keys = new List<byte[]>();
                    for (int i = 15000; i < 20000; i++)
                    {
                        keys.Add(CreateKey(i));
                    }

                    keys.Add(CreateKey(999999999));

                    Dictionary<byte[], byte[]> values = tx.Find(keys);
                    Assert.That(values.Count, Is.EqualTo(keys.Count - 1));

                    for (int i = 15000; i < 20000; i++)
                    {
                        byte[] key = CreateKey(i);
                        Assert.That(values[key], Is.EqualTo(BitConverter.GetBytes((long) i)));
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
            using (VirtualDictionary dict = VirtualDictionary.Open(Path.Combine(testFolder, "TestSimpleAdd.tbl"), 1, 1))
            {
                using (var tx = dict.BeginTransaction())
                {
                    Dictionary<byte[], byte[]> foundValues = tx.Find(new List<byte[]> { new byte[] { 1 } });
                    Assert.That(foundValues, Is.Empty);

                    tx.AddOrUpdate(new byte[] { 1 }, new byte[] { 1 });

                    foundValues = tx.Find(new List<byte[]> { new byte[] { 1 } });
                    Assert.That(foundValues.Count, Is.EqualTo(1));
                    Assert.That(foundValues[new byte[] { 1 }], Is.EqualTo(new byte[] { 1 }));

                    tx.Commit();

                    foundValues = tx.Find(new List<byte[]> { new byte[] { 1 } });
                    Assert.That(foundValues.Count, Is.EqualTo(1));
                    Assert.That(foundValues[new byte[] { 1 }], Is.EqualTo(new byte[] { 1 }));
                }

                using (var tx = dict.BeginTransaction())
                {
                    Dictionary<byte[], byte[]> foundValues = tx.Find(new List<byte[]> { new byte[] { 1 } });
                    Assert.That(foundValues.Count, Is.EqualTo(1));
                    Assert.That(foundValues[new byte[] { 1 }], Is.EqualTo(new byte[] { 1 }));

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