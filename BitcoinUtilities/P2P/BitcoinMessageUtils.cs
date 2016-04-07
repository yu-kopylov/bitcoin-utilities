using System;
using System.IO;
using System.Text;

namespace BitcoinUtilities.P2P
{
    //todo: replace with BitcoinStreamWriter
    public static class BitcoinMessageUtils
    {
        public static void AppendBool(MemoryStream stream, bool value)
        {
            stream.WriteByte((byte) (value ? 1 : 0));
        }

        public static void AppendByte(MemoryStream stream, byte value)
        {
            stream.WriteByte(value);
        }

        public static void AppendUInt16LittleEndian(MemoryStream stream, ushort value)
        {
            //todo: Replace BitConverter with NuberUtils. BitConverter does not guarantee byte-order.
            byte[] bytes = BitConverter.GetBytes(value);
            stream.Write(bytes, 0, 2);
        }

        public static void AppendInt32LittleEndian(MemoryStream stream, int value)
        {
            byte[] bytes = NumberUtils.GetBytes(value);
            stream.Write(bytes, 0, 4);
        }

        public static void AppendUInt32LittleEndian(MemoryStream stream, uint value)
        {
            byte[] bytes = NumberUtils.GetBytes((int) value);
            stream.Write(bytes, 0, 4);
        }

        public static void AppendInt64LittleEndian(MemoryStream stream, long value)
        {
            //todo: Replace BitConverter with NuberUtils. BitConverter does not guarantee byte-order.
            byte[] bytes = BitConverter.GetBytes(value);
            stream.Write(bytes, 0, 8);
        }

        public static void AppendUInt64LittleEndian(MemoryStream stream, ulong value)
        {
            //todo: Replace BitConverter with NuberUtils. BitConverter does not guarantee byte-order.
            byte[] bytes = BitConverter.GetBytes(value);
            stream.Write(bytes, 0, 8);
        }

        public static void AppendUInt16BigEndian(MemoryStream stream, ushort value)
        {
            byte[] bytes = new byte[2];
            bytes[0] = (byte) (value >> 8);
            bytes[1] = (byte) value;
            stream.Write(bytes, 0, 2);
        }

        public static void AppendUInt32BigEndian(MemoryStream stream, uint value)
        {
            value = value << 16 | value >> 16;
            value = (value & 0xFF00FF00u) >> 8 | (value & 0x00FF00FFu) << 8;
            //todo: Replace BitConverter with NuberUtils. BitConverter does not guarantee byte-order.
            byte[] bytes = BitConverter.GetBytes(value);
            stream.Write(bytes, 0, 4);
        }

        public static void AppendUInt64BigEndian(MemoryStream stream, ulong value)
        {
            value = value << 32 | value >> 32;
            value = (value & 0xFFFF0000FFFF0000u) >> 16 | (value & 0x0000FFFF0000FFFFu) << 16;
            value = (value & 0xFF00FF00FF00FF00u) >> 8 | (value & 0x00FF00FF00FF00FFu) << 8;
            //todo: Replace BitConverter with NuberUtils. BitConverter does not guarantee byte-order.
            byte[] bytes = BitConverter.GetBytes(value);
            stream.Write(bytes, 0, 8);
        }

        public static void AppendCompact(MemoryStream stream, ulong value)
        {
            if (value < 0xFD)
            {
                AppendByte(stream, (byte) value);
            }
            else if (value <= 0xFFFF)
            {
                AppendByte(stream, 0xFD);
                AppendUInt16LittleEndian(stream, (ushort) value);
            }
            else if (value <= 0xFFFFFFFFu)
            {
                AppendByte(stream, 0xFE);
                AppendUInt32LittleEndian(stream, (uint) value);
            }
            else
            {
                AppendByte(stream, 0xFF);
                AppendUInt64LittleEndian(stream, value);
            }
        }

        public static void AppendText(MemoryStream stream, string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            AppendCompact(stream, (ulong) bytes.Length);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}