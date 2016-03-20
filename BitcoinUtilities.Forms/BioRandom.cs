using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace BitcoinUtilities.Forms
{
    public class BioRandom
    {
        //todo: clear stream
        //todo: use BinaryWriter
        private MemoryStream hashSource;
        private Stopwatch stopwatch;

        private int entropy;

        public BioRandom()
        {
            Reset();
        }

        public int Entropy
        {
            get { return entropy; }
        }

        private void Reset()
        {
            hashSource = new MemoryStream();
            stopwatch = Stopwatch.StartNew();
            entropy = 0;

            byte[] randomSeed = LongGuid.NewGuid();
            hashSource.Write(randomSeed, 0, randomSeed.Length);
        }

        public void AddPoint(float x, float y)
        {
            WriteFloat(x);
            WriteFloat(y);
            WriteLong(stopwatch.ElapsedTicks);
            entropy++;
        }

        private void WriteFloat(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            hashSource.Write(bytes, 0, bytes.Length);
        }

        private void WriteLong(long value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            hashSource.Write(bytes, 0, bytes.Length);
        }

        public byte[] CreateValue(int length)
        {
            //todo: control max length
            byte[] hash;
            using (SHA512 sha512 = SHA512.Create())
            {
                hash = sha512.ComputeHash(hashSource.ToArray());
            }
            byte[] result = new byte[length];
            Array.Copy(hash, result, length);
            return result;
        }
    }
}