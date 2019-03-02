namespace BitcoinUtilities
{
    public interface IAddressConverter
    {
        string ToDefaultAddress(byte[] publicKeyHash);
        bool TryGetPublicKeyHash(string address, out byte[] publicKeyHash);
    }

    public class CashAddrConverter : IAddressConverter
    {
        private readonly string networkPrefix;

        public CashAddrConverter(string networkPrefix)
        {
            this.networkPrefix = networkPrefix;
        }

        public string ToDefaultAddress(byte[] publicKeyHash)
        {
            return CashAddr.Encode(networkPrefix, BitcoinAddressUsage.PayToPublicKeyHash, publicKeyHash);
        }

        public bool TryGetPublicKeyHash(string address, out byte[] publicKeyHash)
        {
            // todo: check network prefix somewhere
            return CashAddr.TryDecode(address, out _, out _, out publicKeyHash);
        }
    }

    public class BitcoinCoreAddressConverter : IAddressConverter
    {
        private readonly BitcoinNetworkKind networkKind;

        public BitcoinCoreAddressConverter(BitcoinNetworkKind networkKind)
        {
            this.networkKind = networkKind;
        }

        public string ToDefaultAddress(byte[] publicKeyHash)
        {
            return BitcoinAddress.Encode(networkKind, BitcoinAddressUsage.PayToPublicKeyHash, publicKeyHash);
        }

        public bool TryGetPublicKeyHash(string address, out byte[] publicKeyHash)
        {
            // todo: check network prefix somewhere
            return BitcoinAddress.TryDecode(address, out _, out _, out publicKeyHash);
        }
    }
}