using System;
using System.Diagnostics;
using System.IO;

namespace BitcoinUtilities
{
    /// <summary>
    /// This class converts information from user input to a random byte array that can be used as a seed material in the SecureRandom.
    /// </summary>
    public class BioRandom
    {
        private MemoryStream hashSource;
        private BinaryWriter writer;
        private Stopwatch stopwatch;

        private float previousX;
        private float previousY;

        public BioRandom()
        {
            Reset();
        }

        /// <summary>
        /// An estimated bits of entropy in the collected data.
        /// </summary>
        public int Entropy { get; private set; }

        /// <summary>
        /// Adds the mouse coordinates to the collected data.
        /// </summary>
        /// <param name="x">The x-coordinate of the mouse.</param>
        /// <param name="y">The y-coordinate of the mouse.</param>
        public void AddPoint(float x, float y)
        {
            float distance = GetDistanceFromLastPoint(x, y);

            if (distance >= 12)
            {
                writer.Write(x);
                writer.Write(y);
                writer.Write(stopwatch.ElapsedTicks);

                Entropy += 3;
                previousX = x;
                previousY = y;
            }
        }

        /// <summary>
        /// This method creates seed material from the collected input and resets internal buffers.
        /// </summary>
        /// <returns>A byte array with seed material.</returns>
        public byte[] CreateSeedMaterial()
        {
            byte[] res = CryptoUtils.Sha512(hashSource.ToArray());
            Reset();
            return res;
        }

        private void Reset()
        {
            hashSource = new MemoryStream();
            writer = new BinaryWriter(hashSource);
            stopwatch = Stopwatch.StartNew();
            Entropy = 0;
            previousX = 0;
            previousY = 0;
        }

        private float GetDistanceFromLastPoint(float x, float y)
        {
            float distanceX = Math.Abs(x - previousX);
            float distanceY = Math.Abs(y - previousY);
            return distanceX + distanceY;
        }
    }
}