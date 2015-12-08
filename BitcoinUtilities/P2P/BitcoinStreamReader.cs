﻿using System;
using System.IO;
using System.Text;

namespace BitcoinUtilities.P2P
{
    public class BitcoinStreamReader : BinaryReader
    {
        public BitcoinStreamReader(Stream input) : base(input, Encoding.ASCII)
        {
        }

        public int ReadInt32BigEndian()
        {
            uint value = ReadUInt32();
            return (int) NumberUtils.ReverseBytes(value);
        }

        /// <summary>
        /// Reads variable length integer.
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

        public T[] ReadArray<T>(int maxElements, Func<BitcoinStreamReader, T> readMethod)
        {
            ulong elementCountLong = ReadUInt64Compact();
            if (elementCountLong > int.MaxValue)
            {
                //todo: handle correctly
                throw new Exception("Too many elements in array.");
            }
            int elementCount = (int) elementCountLong;
            if (elementCount > maxElements)
            {
                //todo: handle correctly
                throw new Exception("Too many elements in array.");
            }
            T[] result = new T[elementCount];
            for (int i = 0; i < elementCount; i++)
            {
                result[i] = readMethod(this);
            }
            return result;
        }
    }
}