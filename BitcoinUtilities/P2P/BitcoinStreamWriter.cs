using System;
using System.IO;
using System.Text;

namespace BitcoinUtilities.P2P
{
    public class BitcoinStreamWriter : BinaryWriter
    {
        public BitcoinStreamWriter(Stream output) : base(output, Encoding.ASCII)
        {
        }

        /// <summary>
        /// Reads variable length integer.
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

        public void WriteArray<T>(T[] values, Action<BitcoinStreamWriter, T> writeMethod)
        {
            WriteCompact((ulong)values.Length);
            foreach (T value in values)
            {
                writeMethod(this, value);
            }
        }
    }
}