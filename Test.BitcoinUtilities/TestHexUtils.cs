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
            Assert.That(HexUtils.GetString(new byte[] {0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF}), Is.EqualTo("0123456789abcdef"));
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

            Assert.True(HexUtils.TryGetBytes("fe", out bytes));
            Assert.That(bytes, Is.EqualTo(new byte[] {0xFE}));

            Assert.True(HexUtils.TryGetBytes("0123456789ABCDEF", out bytes));
            Assert.That(bytes, Is.EqualTo(new byte[] {0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF}));

            Assert.True(HexUtils.TryGetBytes("0123456789abcdef", out bytes));
            Assert.That(bytes, Is.EqualTo(new byte[] {0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF}));
        }
    }
}