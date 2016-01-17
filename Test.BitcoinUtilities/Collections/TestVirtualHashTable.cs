using System;
using System.Diagnostics;
using BitcoinUtilities.Collections;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Collections
{
    [TestFixture]
    public class TestVirtualHashTable
    {
        [Test]
        public void Test()
        {
            VHTSettings tableSettings = new VHTSettings();
            //todo: use temp file
            //todo: is *.tbl extension save (does not cause problems like *.sdb)
            tableSettings.Filename = @"E:\Temp\Blockchain\hash.tbl";
            tableSettings.KeyLength = 20;
            tableSettings.ValueLength = 8;
            using (VirtualHashTable table = VirtualHashTable.Open(tableSettings))
            {
                Stopwatch sw = Stopwatch.StartNew();

                using (var tx = table.BeginTransaction())
                {
                    for (int i = 0; i < 1000000; i++)
                    {
                        tx.AddOrUpdate(CreateKey(i), new byte[8]);
                    }
                    tx.Commit();
                }

                long time1 = sw.ElapsedMilliseconds;
                sw.Restart();

                using (var tx = table.BeginTransaction())
                {
                    for (int i = 1000000; i < 2010000; i++)
                    {
                        tx.AddOrUpdate(CreateKey(i), new byte[8]);
                    }
                    tx.Commit();
                }

                long time2 = sw.ElapsedMilliseconds;
                sw.Restart();

                using (var tx = table.BeginTransaction())
                {
                    for (int i = 990000; i < 1010000; i++)
                    {
                        //todo: test lookup
                    }
                    tx.Commit();
                }

                long time3 = sw.ElapsedMilliseconds;

                Console.WriteLine("Time1: {0}, Time2: {1}, Time3: {2}", time1, time2, time3);
            }
        }

        private static byte[] CreateKey(int val)
        {
            byte[] key = new byte[20];
            for (int i = 0; i < 20; i++)
            {
                key[i] = (byte)((val >> 8 * (i % 4)) * (31 + i * 2));
            }
            return key;
        }
    }
}