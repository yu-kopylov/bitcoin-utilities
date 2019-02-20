using System;
using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestHexUtils
    {
        [Test]
        public void TestGetString()
        {
            Assert.Throws<ArgumentNullException>(() => HexUtils.GetString(null));

            Assert.That(HexUtils.GetString(new byte[0]), Is.EqualTo(""));
            Assert.That(HexUtils.GetString(new byte[] {0x01}), Is.EqualTo("01"));
            Assert.That(HexUtils.GetString(new byte[] {0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF}), Is.EqualTo("0123456789abcdef"));
        }

        [Test]
        public void TestGetReversedString()
        {
            Assert.Throws<ArgumentNullException>(() => HexUtils.GetReversedString(null));

            Assert.That(HexUtils.GetReversedString(new byte[0]), Is.EqualTo(""));
            Assert.That(HexUtils.GetReversedString(new byte[] {0x01}), Is.EqualTo("01"));
            Assert.That(HexUtils.GetReversedString(new byte[] {0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01}), Is.EqualTo("0123456789abcdef"));
        }

        [Test]
        public void TestGetBytesUnsafe()
        {
            Assert.Throws<ArgumentException>(() => HexUtils.GetBytesUnsafe(null));
            Assert.Throws<ArgumentException>(() => HexUtils.GetBytesUnsafe("a"));
            Assert.Throws<ArgumentException>(() => HexUtils.GetBytesUnsafe("aaa"));
            Assert.Throws<ArgumentException>(() => HexUtils.GetBytesUnsafe("0Z"));
            Assert.Throws<ArgumentException>(() => HexUtils.GetBytesUnsafe("Z0"));

            Assert.That(HexUtils.GetBytesUnsafe(""), Is.EqualTo(new byte[0]));
            Assert.That(HexUtils.GetBytesUnsafe("01"), Is.EqualTo(new byte[] {0x01}));
            Assert.That(HexUtils.GetBytesUnsafe("FE"), Is.EqualTo(new byte[] {0xFE}));
            Assert.That(HexUtils.GetBytesUnsafe("FeeF"), Is.EqualTo(new byte[] {0xFE, 0xEF}));
            Assert.That(HexUtils.GetBytesUnsafe("0123456789ABCDEF"), Is.EqualTo(new byte[] {0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF}));
            Assert.That(HexUtils.GetBytesUnsafe("0123456789abcdef"), Is.EqualTo(new byte[] {0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF}));
        }

        [Test]
        public void TestTryGetBytes()
        {
            byte[] bytes;

            Assert.False(HexUtils.TryGetBytes(null, out bytes));
            Assert.That(bytes, Is.Null);

            Assert.False(HexUtils.TryGetBytes("a", out bytes));
            Assert.That(bytes, Is.Null);

            Assert.False(HexUtils.TryGetBytes("aaa", out bytes));
            Assert.That(bytes, Is.Null);

            Assert.False(HexUtils.TryGetBytes("0Z", out bytes));
            Assert.That(bytes, Is.Null);

            Assert.False(HexUtils.TryGetBytes("Z0", out bytes));
            Assert.That(bytes, Is.Null);

            Assert.True(HexUtils.TryGetBytes("", out bytes));
            Assert.That(bytes, Is.EqualTo(new byte[0]));

            Assert.True(HexUtils.TryGetBytes("01", out bytes));
            Assert.That(bytes, Is.EqualTo(new byte[] {0x01}));

            Assert.True(HexUtils.TryGetBytes("FE", out bytes));
            Assert.That(bytes, Is.EqualTo(new byte[] {0xFE}));

            Assert.True(HexUtils.TryGetBytes("FeeF", out bytes));
            Assert.That(bytes, Is.EqualTo(new byte[] {0xFE, 0xEF}));

            Assert.True(HexUtils.TryGetBytes("0123456789ABCDEF", out bytes));
            Assert.That(bytes, Is.EqualTo(new byte[] {0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF}));

            Assert.True(HexUtils.TryGetBytes("0123456789abcdef", out bytes));
            Assert.That(bytes, Is.EqualTo(new byte[] {0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF}));
        }

        [Test]
        public void TestTryGetReversedBytes()
        {
            byte[] bytes;

            Assert.False(HexUtils.TryGetReversedBytes(null, out bytes));
            Assert.That(bytes, Is.Null);

            Assert.False(HexUtils.TryGetReversedBytes("a", out bytes));
            Assert.That(bytes, Is.Null);

            Assert.False(HexUtils.TryGetReversedBytes("aaa", out bytes));
            Assert.That(bytes, Is.Null);

            Assert.False(HexUtils.TryGetReversedBytes("0Z", out bytes));
            Assert.That(bytes, Is.Null);

            Assert.False(HexUtils.TryGetReversedBytes("Z0", out bytes));
            Assert.That(bytes, Is.Null);

            Assert.True(HexUtils.TryGetReversedBytes("", out bytes));
            Assert.That(bytes, Is.EqualTo(new byte[0]));

            Assert.True(HexUtils.TryGetReversedBytes("01", out bytes));
            Assert.That(bytes, Is.EqualTo(new byte[] {0x01}));

            Assert.True(HexUtils.TryGetReversedBytes("FE", out bytes));
            Assert.That(bytes, Is.EqualTo(new byte[] {0xFE}));

            Assert.True(HexUtils.TryGetReversedBytes("FeeF", out bytes));
            Assert.That(bytes, Is.EqualTo(new byte[] {0xEF, 0xFE}));

            Assert.True(HexUtils.TryGetReversedBytes("0123456789ABCDEF", out bytes));
            Assert.That(bytes, Is.EqualTo(new byte[] {0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01}));

            Assert.True(HexUtils.TryGetReversedBytes("0123456789abcdef", out bytes));
            Assert.That(bytes, Is.EqualTo(new byte[] {0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01}));
        }
    }
}