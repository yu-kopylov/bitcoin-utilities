﻿using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace BitcoinUtilities
{
    public class SecureRandomSeedGenerator
    {
        private static long seedCounter;

        /// <summary>
        /// Returns a unique array of 64 bytes.
        /// The returned value is a SHA-512 hash of a uninique set of parameters.
        /// </summary>
        /// <returns>A unique array of 64 bytes.</returns>
        public static byte[] CreateSeed()
        {
            var mem = new MemoryStream();

            using (var process = Process.GetCurrentProcess())
            {
                // relatively unique moment of time
                WriteLong(mem, DateTime.UtcNow.ToBinary());
                WriteLong(mem, Environment.TickCount);

                // exclude collisions within the same AppDomain
                var seedCounterValue = Interlocked.Increment(ref seedCounter);
                WriteLong(mem, seedCounterValue);

                // exclude collisions beteen AppDomains within the same process
                WriteLong(mem, AppDomain.CurrentDomain.Id);

                // exclude collisions between processes within the same operating system instance
                WriteLong(mem, process.Id);

                // sould exclude collisions between different machines
                var guid = Guid.NewGuid().ToByteArray();
                mem.Write(guid, 0, guid.Length);

                // relatively unique process parameters (memory state)
                WriteLong(mem, GC.GetTotalMemory(false));
                WriteLong(mem, process.PeakWorkingSet64);
                WriteLong(mem, new object().GetHashCode());

                // relatively unique process parameters (execution time)
                WriteLong(mem, process.PrivilegedProcessorTime.Ticks);
                WriteLong(mem, process.UserProcessorTime.Ticks);
            }

            // should be unique and unpredictable
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                byte[] randomBytes = new byte[512];
                rng.GetBytes(randomBytes);
                mem.Write(randomBytes, 0, randomBytes.Length);
            }

            return CryptoUtils.Sha512(mem.ToArray());
        }

        private static void WriteLong(Stream stream, long value)
        {
            var bytes = BitConverter.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}