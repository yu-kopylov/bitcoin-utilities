using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestMurmur3x86
    {
        [Test]
        public void TestGetHash32()
        {
            Assert.AreEqual(0x00000000u, Murmur3x86.GetHash32(0x00000000, HexUtils.GetBytesUnsafe("")));
            Assert.AreEqual(0x6A396F08u, Murmur3x86.GetHash32(0xFBA4C795, HexUtils.GetBytesUnsafe("")));
            Assert.AreEqual(0x81F16F39u, Murmur3x86.GetHash32(0xffffffff, HexUtils.GetBytesUnsafe("")));

            Assert.AreEqual(0x514E28B7u, Murmur3x86.GetHash32(0x00000000, HexUtils.GetBytesUnsafe("00")));
            Assert.AreEqual(0xEA3F0B17u, Murmur3x86.GetHash32(0xFBA4C795, HexUtils.GetBytesUnsafe("00")));
            Assert.AreEqual(0xFD6CF10Du, Murmur3x86.GetHash32(0x00000000, HexUtils.GetBytesUnsafe("ff")));

            Assert.AreEqual(0x16C6B7ABu, Murmur3x86.GetHash32(0x00000000, HexUtils.GetBytesUnsafe("0011")));
            Assert.AreEqual(0x8EB51C3Du, Murmur3x86.GetHash32(0x00000000, HexUtils.GetBytesUnsafe("001122")));
            Assert.AreEqual(0xB4471BF8u, Murmur3x86.GetHash32(0x00000000, HexUtils.GetBytesUnsafe("00112233")));
            Assert.AreEqual(0xE2301FA8u, Murmur3x86.GetHash32(0x00000000, HexUtils.GetBytesUnsafe("0011223344")));
            Assert.AreEqual(0xFC2E4A15u, Murmur3x86.GetHash32(0x00000000, HexUtils.GetBytesUnsafe("001122334455")));
            Assert.AreEqual(0xB074502Cu, Murmur3x86.GetHash32(0x00000000, HexUtils.GetBytesUnsafe("00112233445566")));
            Assert.AreEqual(0x8034D2A0u, Murmur3x86.GetHash32(0x00000000, HexUtils.GetBytesUnsafe("0011223344556677")));
            Assert.AreEqual(0xB4698DEFu, Murmur3x86.GetHash32(0x00000000, HexUtils.GetBytesUnsafe("001122334455667788")));
        }
    }
}