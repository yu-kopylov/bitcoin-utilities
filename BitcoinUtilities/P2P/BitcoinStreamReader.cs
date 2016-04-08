using System;
using System.IO;
using System.Net;
using System.Text;

namespace BitcoinUtilities.P2P
{
    public class BitcoinStreamReader : BinaryReader
    {
        public BitcoinStreamReader(Stream input) : base(input, Encoding.ASCII)
        {
        }

        /// <summary>
        /// Reads a two-byte unsigned integer in a big-endian encoding.
        /// </summary>
        public ushort ReadUInt16BigEndian()
        {
            ushort value = ReadUInt16();
            return NumberUtils.ReverseBytes(value);
        }

        /// <summary>
        /// Reads a four-byte signed integer in a big-endian encoding.
        /// </summary>
        public int ReadInt32BigEndian()
        {
            uint value = ReadUInt32();
            return (int) NumberUtils.ReverseBytes(value);
        }

        /// <summary>
        /// Reads a variable length integer.
        /// </summary>
        public ulong ReadUInt64Compact()
        {
            byte prefix = ReadByte();

            if (prefix < 0xFD)
            {
                return prefix;
            }

            if (prefix == 0xFD)
            {
                return ReadUInt16();
            }

            if (prefix == 0xFE)
            {
                return ReadUInt32();
            }

            return ReadUInt64();
        }

        /// <summary>
        /// Reads an array of objects prefixed with the count of objects in a variable length integer format.
        /// </summary>
        /// <exception cref="IOException">If the received array has more than <see cref="maxElements"/> elements.</exception>
        public T[] ReadArray<T>(int maxElements, Func<BitcoinStreamReader, T> readMethod)
        {
            ulong elementCountLong = ReadUInt64Compact();
            if (elementCountLong > int.MaxValue)
            {
                throw new IOException($"Received an array with too many elements ({elementCountLong} elements).");
            }
            int elementCount = (int) elementCountLong;
            if (elementCount > maxElements)
            {
                throw new IOException($"Received an array with too many elements ({elementCountLong} elements).");
            }
            T[] result = new T[elementCount];
            for (int i = 0; i < elementCount; i++)
            {
                result[i] = readMethod(this);
            }
            return result;
        }

        /// <summary>
        /// Reads a length-prefixed string in the ASCII encoding.
        /// The length of the string should be a variable length integer.
        /// </summary>
        /// <exception cref="IOException">If the received string is longer than <see cref="maxLength"/>.</exception>
        public string ReadText(int maxLength)
        {
            ulong length = ReadUInt64Compact();
            if (length > int.MaxValue)
            {
                throw new IOException($"A received string is too long ({length} bytes).");
            }
            int intLength = (int) length;
            if (intLength > maxLength)
            {
                throw new IOException($"A received string is too long ({length} bytes).");
            }
            byte[] bytes = ReadBytes(intLength);
            return Encoding.ASCII.GetString(bytes);
        }

        /// <summary>
        /// Reads an IPv6 address.
        /// </summary>
        public IPAddress ReadAddress()
        {
            byte[] addressBytes = ReadBytes(16);
            return new IPAddress(addressBytes);
        }
    }
}