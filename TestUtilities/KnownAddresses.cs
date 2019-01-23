using BitcoinUtilities;

namespace TestUtilities
{
    public static class KnownAddresses
    {
        /// <summary>
        /// Address from: https://en.bitcoinwiki.org/wiki/Wallet_import_format
        /// </summary>
        public static KnownAddress ReferenceExample { get; } = new KnownAddress("0C28FCA386C7A227600B2FE50B7CAE11EC86D3BF1FBE471BE89827E19D72AA1D");

        /// <summary>
        /// Address from: https://github.com/bitcoin/bips/blob/master/bip-0038.mediawiki
        /// </summary>
        public static KnownAddress Bip38Example { get; } = new KnownAddress("64EEAB5F9BE2A01A8365A579511EB3373C87C40DA6D2A25F05BDA68FE077B66E");

        /// <summary>
        /// Uncompressed encoded public key has X component that starts with zero.
        /// </summary>
        public static KnownAddress UncompressedX0 { get; } = new KnownAddress("0000000000000000000000000000000000000000000000000000000000000099");

        /// <summary>
        /// Uncompressed encoded public key has Y component that starts with zero.
        /// </summary>
        public static KnownAddress UncompressedY0 { get; } = new KnownAddress("000000000000000000000000000000000000000000000000000000000000007A");

        /// <summary>
        /// Corresponding EC point has X and Y coordinates that start with zero.
        /// </summary>
        public static KnownAddress LowPoint { get; } = new KnownAddress("400000000000000000000000000000000000000000000000000000000028A636");

        public class KnownAddress
        {
            public KnownAddress(string privateKey)
            {
                HexUtils.TryGetBytes(privateKey, out var privateKeyBytes);
                PrivateKey = privateKeyBytes;
            }

            public byte[] PrivateKey { get; }
        }
    }
}