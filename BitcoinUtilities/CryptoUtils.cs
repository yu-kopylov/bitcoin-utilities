using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace BitcoinUtilities
{
    //todo: add XMLDOC
    public static class CryptoUtils
    {
        private static readonly ConcurrentBag<SHA256> sha256Algs = new ConcurrentBag<SHA256>();

        private static readonly object sha512Lock = new object();
        private static SHA512 sha512Alg;

        private static readonly object sha1Lock = new object();
        private static SHA1 sha1Alg;

        private static readonly object ripeMd160Lock = new object();
        private static RIPEMD160 ripeMd160Alg;

        public static byte[] DoubleSha256(byte[] text)
        {
            if (!sha256Algs.TryTake(out var sha256Alg))
            {
                sha256Alg = SHA256.Create();
            }

            try
            {
                sha256Alg.Initialize();
                return sha256Alg.ComputeHash(sha256Alg.ComputeHash(text));
            }
            finally
            {
                sha256Algs.Add(sha256Alg);
            }
        }

        public static byte[] Sha256(params byte[][] sources)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources), "The sources array is null.");
            }

            foreach (byte[] source in sources)
            {
                if (source == null)
                {
                    throw new ArgumentNullException(nameof(sources), "The sources array contains null element.");
                }
            }

            if (!sha256Algs.TryTake(out var sha256Alg))
            {
                sha256Alg = SHA256.Create();
            }

            try
            {
                sha256Alg.Initialize();

                foreach (byte[] source in sources)
                {
                    int offset = 0;
                    while (offset < source.Length)
                    {
                        offset += sha256Alg.TransformBlock(source, offset, source.Length - offset, source, offset);
                    }
                }

                sha256Alg.TransformFinalBlock(new byte[0], 0, 0);

                return sha256Alg.Hash;
            }
            finally
            {
                sha256Algs.Add(sha256Alg);
            }
        }

        public static byte[] Sha512(params byte[][] sources)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources), "The sources array is null.");
            }

            foreach (byte[] source in sources)
            {
                if (source == null)
                {
                    throw new ArgumentNullException(nameof(sources), "The sources array contains null element.");
                }
            }

            lock (sha512Lock)
            {
                if (sha512Alg == null)
                {
                    sha512Alg = SHA512.Create();
                }

                sha512Alg.Initialize();

                foreach (byte[] source in sources)
                {
                    int offset = 0;
                    while (offset < source.Length)
                    {
                        offset += sha512Alg.TransformBlock(source, offset, source.Length - offset, source, offset);
                    }
                }

                sha512Alg.TransformFinalBlock(new byte[0], 0, 0);

                return sha512Alg.Hash;
            }
        }

        public static byte[] Sha1(byte[] text)
        {
            lock (sha1Lock)
            {
                if (sha1Alg == null)
                {
                    sha1Alg = SHA1.Create();
                }

                sha1Alg.Initialize();
                return sha1Alg.ComputeHash(text);
            }
        }

        public static byte[] RipeMd160(byte[] text)
        {
            lock (ripeMd160Lock)
            {
                if (ripeMd160Alg == null)
                {
                    ripeMd160Alg = RIPEMD160.Create();
                }

                ripeMd160Alg.Initialize();
                return ripeMd160Alg.ComputeHash(text);
            }
        }
    }
}