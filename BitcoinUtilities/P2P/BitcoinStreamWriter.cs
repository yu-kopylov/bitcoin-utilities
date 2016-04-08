using System;
using System.IO;
using System.Net;
using System.Text;

namespace BitcoinUtilities.P2P
{
    public class BitcoinStreamWriter : BinaryWriter
    {
        public BitcoinStreamWriter(Stream output) : base(output, Encoding.ASCII)
        {
        }

        /// <summary>
        /// Writes a two-byte unsigned integer in a big-endian encoding.
        /// </summary>
        public void WriteBigEndian(ushort value)
        {
            value = NumberUtils.ReverseBytes(value);
            Write(value);
        }

        /// <summary>
        /// Writes variable length integer.
        /// </summary>
        public void WriteCompact(ulong value)
        {
            if (value < 0xFD)
            {
                Write((byte) value);
            }
            else if (value <= 0xFFFF)
            {
                Write((byte) 0xFD);
                Write((ushort) value);
            }
            else if (value <= 0xFFFFFFFFu)
            {
                Write((byte) 0xFE);
                Write((uint) value);
            }
            else
            {
                Write((byte) 0xFF);
                Write(value);
            }
        }

        /// <summary>
        /// Writes an array of objects prepending it with a count of objects in a variable length integer format.
        /// </summary>
        /// <typeparam name="T">The type of objects to be written.</typeparam>
        /// <param name="values">The array of objects.</param>
        /// <param name="writeMethod">A method that can write a single object to a stream.</param>
        public void WriteArray<T>(T[] values, Action<BitcoinStreamWriter, T> writeMethod)
        {
            WriteCompact((ulong) values.Length);
            foreach (T value in values)
            {
                writeMethod(this, value);
            }
        }

        /// <summary>
        /// Writes a length-prefixed string in the ASCII encoding.
        /// The length of the string is written as a variable length integer.
        /// </summary>
        public void WriteText(string value)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(value);
            WriteCompact((ulong) bytes.Length);
            Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Writes an IP address as an array of 16 bytes.
        /// IPv4 addresses are mapped to IPv6 addresses before processing.
        /// </summary>
        public void WriteAddress(IPAddress address)
        {
            byte[] addressBytes = address.MapToIPv6().GetAddressBytes();
            Write(addressBytes, 0, addressBytes.Length);
        }

        public static byte[] GetBytes(Action<BitcoinStreamWriter> writeMethod)
        {
            MemoryStream stream = new MemoryStream();
            using (BitcoinStreamWriter writer = new BitcoinStreamWriter(stream))
            {
                writeMethod(writer);
            }
            return stream.ToArray();
        }
    }
}