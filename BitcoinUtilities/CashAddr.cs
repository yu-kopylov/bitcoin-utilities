using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BitcoinUtilities
{
    /// <summary>
    /// Specification: https://github.com/bitcoincashorg/bitcoincash.org/blob/master/spec/cashaddr.md
    /// </summary>
    public static class CashAddr
    {
        private static readonly char[] base32Alphabet = "qpzry9x8gf2tvdw0s3jn54khce6mua7l".ToCharArray();

        public static string Encode(string networkPrefix, BitcoinAddressUsage addressUsage, byte[] publicKeyHash)
        {
            if (string.IsNullOrEmpty(networkPrefix))
            {
                throw new ArgumentException($"{nameof(networkPrefix)} cannot be null or empty.", nameof(networkPrefix));
            }

            if (publicKeyHash == null)
            {
                throw new ArgumentException($"{nameof(publicKeyHash)} cannot be null.", nameof(publicKeyHash));
            }

            if (publicKeyHash.Length != 20)
            {
                throw new ArgumentException("Only 160-bit hashes are supported.", nameof(publicKeyHash));
            }

            byte[] payload = new byte[1 + publicKeyHash.Length];
            payload[0] = EncodeAddressVersion(addressUsage);
            Array.Copy(publicKeyHash, 0, payload, 1, publicKeyHash.Length);

            byte[] checksum = new byte[5];

            ulong checksumValue = PolyMod(
                networkPrefix.Select(c => (byte) (c & 0x1F))
                    .Concat(new byte[1])
                    .Concat(To5BitStream(payload))
                    .Concat(To5BitStream(checksum))
            );

            for (int i = 4; i >= 0; i--)
            {
                checksum[i] = (byte) checksumValue;
                checksumValue >>= 8;
            }

            StringBuilder sb = new StringBuilder(networkPrefix.Length + 1 + (payload.Length * 8 + 4) / 5);
            sb.Append(networkPrefix);
            sb.Append(':');

            foreach (byte b in To5BitStream(payload))
            {
                sb.Append(base32Alphabet[b]);
            }

            foreach (byte b in To5BitStream(checksum))
            {
                sb.Append(base32Alphabet[b]);
            }

            return sb.ToString();
        }

        private static byte EncodeAddressVersion(BitcoinAddressUsage addressUsage)
        {
            if (addressUsage == BitcoinAddressUsage.PayToPublicKeyHash)
            {
                return 0;
            }

            if (addressUsage == BitcoinAddressUsage.PayToScriptHash)
            {
                return 8;
            }

            throw new ArgumentException($"Unexpected address version: {addressUsage}.", nameof(addressUsage));
        }

        private static IEnumerable<byte> To5BitStream(IEnumerable<byte> data)
        {
            uint value = 0;
            int bits = 0;

            int resultOfs = 0;

            foreach (byte b in data)
            {
                value = value << 8 | b;
                bits += 8;

                while (bits >= 5)
                {
                    yield return (byte) ((value >> (bits - 5)) & 0x1FU);
                    bits -= 5;
                }
            }

            if (bits != 0)
            {
                yield return (byte) ((value << (5 - bits)) & 0x1FU);
            }
        }

        private static ulong PolyMod(IEnumerable<byte> v)
        {
            ulong c = 1;
            foreach (byte d in v)
            {
                byte c0 = (byte) (c >> 35);
                c = ((c & 0x07ffffffffUL) << 5) ^ d;

                if ((c0 & 0x01) != 0) c ^= 0x98f2bc8e61;
                if ((c0 & 0x02) != 0) c ^= 0x79b76d99e2;
                if ((c0 & 0x04) != 0) c ^= 0xf33e5fb3c4;
                if ((c0 & 0x08) != 0) c ^= 0xae2eabe2a8;
                if ((c0 & 0x10) != 0) c ^= 0x1e4f43e470;
            }

            return c ^ 1;
        }
    }
}