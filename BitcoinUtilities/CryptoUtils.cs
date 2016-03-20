using System;
using System.Security.Cryptography;

namespace BitcoinUtilities
{
    //todo: test resource pool pattern
    public static class CryptoUtils
    {
        private static readonly object sha256Lock = new object();
        private static SHA256 sha256Alg;

        private static readonly object sha512Lock = new object();
        private static SHA512 sha512Alg;
        private static bool sha512AlgDirty;

        public static byte[] DoubleSha256(byte[] text)
        {
            lock (sha256Lock)
            {
                if (sha256Alg == null)
                {
                    sha256Alg = SHA256.Create();
                }
                return sha256Alg.ComputeHash(sha256Alg.ComputeHash(text));
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

                if (sha512AlgDirty)
                {
                    sha512Alg.Initialize();
                }

                sha512AlgDirty = true;

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
    }
}