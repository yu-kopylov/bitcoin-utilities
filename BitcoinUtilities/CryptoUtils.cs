using System.Security.Cryptography;

namespace BitcoinUtilities
{
    //todo: test resource pool pattern
    public static class CryptoUtils
    {
        private static readonly object sha256Lock = new object();
        private static SHA256 sha256Alg;

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
    }
}