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
        private static readonly byte[] base32AlphabetReversed = new byte[128];

        static CashAddr()
        {
            for (int i = 0; i < base32AlphabetReversed.Length; i++)
            {
                base32AlphabetReversed[i] = 0xFF;
            }

            for (int i = 0; i < base32Alphabet.Length; i++)
            {
                char c = base32Alphabet[i];
                base32AlphabetReversed[c] = (byte) i;
                if ('a' <= c && c <= 'z')
                {
                    base32AlphabetReversed[c - 'a' + 'A'] = (byte) i;
                }
            }
        }

        public static string Encode(string networkPrefix, BitcoinAddressUsage addressUsage, byte[] publicKeyHash)
        {
            //todo: add tests for validation

            if (string.IsNullOrEmpty(networkPrefix))
            {
                throw new ArgumentException($"{nameof(networkPrefix)} cannot be null or empty.", nameof(networkPrefix));
            }

            if (publicKeyHash == null)
            {
                throw new ArgumentException($"{nameof(publicKeyHash)} cannot be null.", nameof(publicKeyHash));
            }

            byte[] payload = new byte[1 + publicKeyHash.Length];
            payload[0] = EncodeAddressVersion(addressUsage, publicKeyHash.Length);
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

        public static bool TryDecode(string address, out string networkPrefix, out BitcoinAddressUsage addressUsage, out byte[] publicKeyHash)
        {
            networkPrefix = null;
            addressUsage = default(BitcoinAddressUsage);
            publicKeyHash = null;

            //todo: add tests for validation

            if (string.IsNullOrEmpty(address))
            {
                return false;
            }

            int separatorIndex = address.IndexOf(':');
            if (separatorIndex < 0)
            {
                return false;
            }

            int payloadBase32Length = address.Length - separatorIndex - 1;
            if (payloadBase32Length < 10)
            {
                return false;
            }

            byte[] payloadBase32 = new byte[payloadBase32Length];

            bool useUpperCase = false, useLowerCase = false;

            for (int i = 0; i < payloadBase32Length; i++)
            {
                char c = address[separatorIndex + 1 + i];
                if (c >= (char) 128)
                {
                    return false;
                }

                if ('a' <= c && c <= 'z')
                {
                    useLowerCase = true;
                }

                if ('A' <= c && c <= 'Z')
                {
                    useUpperCase = true;
                }

                payloadBase32[i] = base32AlphabetReversed[c];
            }

            if (useUpperCase && useLowerCase)
            {
                // Lower case is preferred for cashaddr, but uppercase is accepted. A mixture of lower case and uppercase must be rejected.
                return false;
            }

            if (PolyMod(
                    address.Take(separatorIndex).Select(c => (byte) (c & 0x1F))
                        .Concat(new byte[1])
                        .Concat(payloadBase32)
                ) != 0)
            {
                return false;
            }

            if (!TryDecode5BitStream(payloadBase32, payloadBase32Length - 8, out byte[] payload))
            {
                return false;
            }

            if (!TryDecodeAddressVersion(payload[0], out addressUsage, out int hashSize))
            {
                return false;
            }

            if (payload.Length != hashSize + 1)
            {
                return false;
            }

            networkPrefix = address.Substring(0, separatorIndex);
            publicKeyHash = new byte[hashSize];
            Array.Copy(payload, 1, publicKeyHash, 0, hashSize);

            return true;
        }

        private static byte EncodeAddressVersion(BitcoinAddressUsage addressUsage, int hashSie)
        {
            if (hashSie != 20)
            {
                throw new ArgumentException("Only 160-bit hashes are supported.", nameof(hashSie));
            }

            if (addressUsage == BitcoinAddressUsage.PayToPublicKeyHash)
            {
                return 0;
            }

            if (addressUsage == BitcoinAddressUsage.PayToScriptHash)
            {
                return 8;
            }

            throw new ArgumentException($"Unexpected address usage: {addressUsage}.", nameof(addressUsage));
        }

        private static bool TryDecodeAddressVersion(byte addressVersion, out BitcoinAddressUsage addressUsage, out int hashSize)
        {
            addressUsage = default(BitcoinAddressUsage);
            hashSize = 0;

            int reserved = addressVersion & 0x80;
            int hashSizeCode = addressVersion & 0x07;
            int usage = addressVersion & 0x78;

            if (reserved != 0)
            {
                return false;
            }

            if (hashSizeCode == 0)
            {
                hashSize = 20;
            }
            else
            {
                return false;
            }

            if (usage == 0)
            {
                addressUsage = BitcoinAddressUsage.PayToPublicKeyHash;
            }
            else if (usage == 8)
            {
                addressUsage = BitcoinAddressUsage.PayToScriptHash;
            }
            else
            {
                return false;
            }

            return true;
        }

        private static IEnumerable<byte> To5BitStream(IEnumerable<byte> data)
        {
            uint value = 0;
            int bits = 0;

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

        private static bool TryDecode5BitStream(byte[] data, int dataLength, out byte[] payload)
        {
            uint value = 0;
            int bits = 0;

            int bitLength = dataLength * 5;
            if (bitLength % 8 >= 5)
            {
                // there are redundant digits
                payload = null;
                return false;
            }

            int payloadLength = bitLength / 8;
            byte[] tempPayload = new byte[payloadLength];
            int payloadOfs = 0;

            foreach (byte b in data.Take(dataLength))
            {
                value = value << 5 | b;
                bits += 5;

                while (bits >= 8)
                {
                    tempPayload[payloadOfs++] = (byte) (value >> (bits - 8));
                    bits -= 8;
                }
            }

            if ((value & (0xFF >> (8 - bits))) != 0)
            {
                // there are extra non-zero bits
                payload = null;
                return false;
            }

            payload = tempPayload;
            return true;
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