using System;
using System.IO;
using BitcoinUtilities.P2P;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P
{
    [TestFixture]
    public class TestBitcoinMessageUtils
    {
        [Test]
        public void TestAppendBool()
        {
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendBool(mem, true)), Is.EqualTo(new byte[] {0x01}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendBool(mem, false)), Is.EqualTo(new byte[] {0x00}));
        }

        [Test]
        public void TestAppendByte()
        {
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendByte(mem, 0x00)), Is.EqualTo(new byte[] {0x00}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendByte(mem, 0x12)), Is.EqualTo(new byte[] {0x12}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendByte(mem, 0x7F)), Is.EqualTo(new byte[] {0x7F}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendByte(mem, 0xFF)), Is.EqualTo(new byte[] {0xFF}));
        }

        [Test]
        public void TestAppendUInt16LittleEndian()
        {
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt16LittleEndian(mem, 0x0000)), Is.EqualTo(new byte[] {0x00, 0x00}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt16LittleEndian(mem, 0x1234)), Is.EqualTo(new byte[] {0x34, 0x12}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt16LittleEndian(mem, 0x7FFF)), Is.EqualTo(new byte[] {0xFF, 0x7F}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt16LittleEndian(mem, 0xFFFF)), Is.EqualTo(new byte[] {0xFF, 0xFF}));
        }

        [Test]
        public void TestAppendInt32LittleEndian()
        {
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendInt32LittleEndian(mem, 0x00000000)), Is.EqualTo(new byte[] {0x00, 0x00, 0x00, 0x00}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendInt32LittleEndian(mem, 0x12345678)), Is.EqualTo(new byte[] {0x78, 0x56, 0x34, 0x12}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendInt32LittleEndian(mem, 0x7FFFFFFF)), Is.EqualTo(new byte[] {0xFF, 0xFF, 0xFF, 0x7F}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendInt32LittleEndian(mem, -1)), Is.EqualTo(new byte[] {0xFF, 0xFF, 0xFF, 0xFF}));
        }

        [Test]
        public void TestAppendUInt32LittleEndian()
        {
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt32LittleEndian(mem, 0x00000000)), Is.EqualTo(new byte[] {0x00, 0x00, 0x00, 0x00}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt32LittleEndian(mem, 0x12345678)), Is.EqualTo(new byte[] {0x78, 0x56, 0x34, 0x12}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt32LittleEndian(mem, 0x7FFFFFFF)), Is.EqualTo(new byte[] {0xFF, 0xFF, 0xFF, 0x7F}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt32LittleEndian(mem, 0xFFFFFFFF)), Is.EqualTo(new byte[] {0xFF, 0xFF, 0xFF, 0xFF}));
        }

        [Test]
        public void TestAppendInt64LittleEndian()
        {
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendInt64LittleEndian(mem, 0x0000000000000000)), Is.EqualTo(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendInt64LittleEndian(mem, 0x1234567890123456)), Is.EqualTo(new byte[] {0x56, 0x34, 0x12, 0x90, 0x78, 0x56, 0x34, 0x12}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendInt64LittleEndian(mem, 0x7FFFFFFFFFFFFFFF)), Is.EqualTo(new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendInt64LittleEndian(mem, -1)), Is.EqualTo(new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF}));
        }

        [Test]
        public void TestAppendUInt64LittleEndian()
        {
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt64LittleEndian(mem, 0x0000000000000000)), Is.EqualTo(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt64LittleEndian(mem, 0x1234567890123456)), Is.EqualTo(new byte[] {0x56, 0x34, 0x12, 0x90, 0x78, 0x56, 0x34, 0x12}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt64LittleEndian(mem, 0x7FFFFFFFFFFFFFFF)), Is.EqualTo(new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt64LittleEndian(mem, 0xFFFFFFFFFFFFFFFF)), Is.EqualTo(new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF}));
        }

        [Test]
        public void TestAppendAppendUInt16BigEndian()
        {
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt16BigEndian(mem, 0x0000)), Is.EqualTo(new byte[] {0x00, 0x00}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt16BigEndian(mem, 0x1234)), Is.EqualTo(new byte[] {0x12, 0x34}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt16BigEndian(mem, 0x7FFF)), Is.EqualTo(new byte[] {0x7F, 0xFF}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt16BigEndian(mem, 0xFFFF)), Is.EqualTo(new byte[] {0xFF, 0xFF}));
        }

        [Test]
        public void TestAppendUInt32BigEndian()
        {
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt32BigEndian(mem, 0x00000000)), Is.EqualTo(new byte[] {0x00, 0x00, 0x00, 0x00}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt32BigEndian(mem, 0x12345678)), Is.EqualTo(new byte[] {0x12, 0x34, 0x56, 0x78}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt32BigEndian(mem, 0x7FFFFFFF)), Is.EqualTo(new byte[] {0x7F, 0xFF, 0xFF, 0xFF}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt32BigEndian(mem, 0xFFFFFFFF)), Is.EqualTo(new byte[] {0xFF, 0xFF, 0xFF, 0xFF}));
        }

        [Test]
        public void TestAppendUInt64BigEndian()
        {
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt64BigEndian(mem, 0x0000000000000000)), Is.EqualTo(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt64BigEndian(mem, 0x1234567890123456)), Is.EqualTo(new byte[] {0x12, 0x34, 0x56, 0x78, 0x90, 0x12, 0x34, 0x56}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt64BigEndian(mem, 0x7FFFFFFFFFFFFFFF)), Is.EqualTo(new byte[] {0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendUInt64BigEndian(mem, 0xFFFFFFFFFFFFFFFF)), Is.EqualTo(new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF}));
        }

        [Test]
        public void TestAppendCompact()
        {
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendCompact(mem, 0)), Is.EqualTo(new byte[] {0x00}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendCompact(mem, 1)), Is.EqualTo(new byte[] {0x01}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendCompact(mem, 0xFC)), Is.EqualTo(new byte[] {0xFC}));

            Assert.That(Apply(mem => BitcoinMessageUtils.AppendCompact(mem, 0x00FD)), Is.EqualTo(new byte[] {0xFD, 0xFD, 0x00}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendCompact(mem, 0x1234)), Is.EqualTo(new byte[] {0xFD, 0x34, 0x12}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendCompact(mem, 0x7FFF)), Is.EqualTo(new byte[] {0xFD, 0xFF, 0x7F}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendCompact(mem, 0xFFFF)), Is.EqualTo(new byte[] {0xFD, 0xFF, 0xFF}));

            Assert.That(Apply(mem => BitcoinMessageUtils.AppendCompact(mem, 0x00010000)), Is.EqualTo(new byte[] {0xFE, 0x00, 0x00, 0x01, 0x00}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendCompact(mem, 0x12345678)), Is.EqualTo(new byte[] {0xFE, 0x78, 0x56, 0x34, 0x12}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendCompact(mem, 0x7FFFFFFF)), Is.EqualTo(new byte[] {0xFE, 0xFF, 0xFF, 0xFF, 0x7F}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendCompact(mem, 0xFFFFFFFF)), Is.EqualTo(new byte[] {0xFE, 0xFF, 0xFF, 0xFF, 0xFF}));

            Assert.That(Apply(mem => BitcoinMessageUtils.AppendCompact(mem, 0x0000000100000000)), Is.EqualTo(new byte[] {0xFF, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendCompact(mem, 0x1234567890123456)), Is.EqualTo(new byte[] {0xFF, 0x56, 0x34, 0x12, 0x90, 0x78, 0x56, 0x34, 0x12}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendCompact(mem, 0x7FFFFFFFFFFFFFFF)), Is.EqualTo(new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendCompact(mem, 0xFFFFFFFFFFFFFFFF)), Is.EqualTo(new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF}));
        }

        [Test]
        public void TestAppendText()
        {
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendText(mem, "")), Is.EqualTo(new byte[] {0x00}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendText(mem, "1")), Is.EqualTo(new byte[] {0x01, 0x31}));
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendText(mem, "12")), Is.EqualTo(new byte[] {0x02, 0x31, 0x32}));

            byte[] expectedResult = new byte[256 + 3];
            expectedResult[0] = 0xFD;
            expectedResult[1] = 0x00;
            expectedResult[2] = 0x01;
            for (int i = 0; i < 256; i++)
            {
                expectedResult[i + 3] = 0x20;
            }
            expectedResult[expectedResult.Length - 2] = 0x31;
            expectedResult[expectedResult.Length - 1] = 0x32;
            Assert.That(Apply(mem => BitcoinMessageUtils.AppendText(mem, "12".PadLeft(256))), Is.EqualTo(expectedResult));
        }

        private byte[] Apply(Action<MemoryStream> action)
        {
            MemoryStream mem = new MemoryStream();
            action(mem);
            return mem.ToArray();
        }
    }
}