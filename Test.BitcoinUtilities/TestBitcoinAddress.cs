using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestBitcoinAddress
    {
        [Test]
        public void TestFromPrivateKey()
        {
            // Reference example
            Assert.That(BitcoinAddress.FromPrivateKey(new byte[]
                                                      {
                                                          0x18, 0xE1, 0x4A, 0x7B, 0x6A, 0x30, 0x7F, 0x42, 0x6A, 0x94, 0xF8, 0x11, 0x47, 0x01, 0xE7, 0xC8,
                                                          0xE7, 0x74, 0xE7, 0xF9, 0xA4, 0x7E, 0x2C, 0x20, 0x35, 0xDB, 0x29, 0xA2, 0x06, 0x32, 0x17, 0x25
                                                      }), Is.EqualTo("16UwLL9Risc3QfPqBUvKofHmBQ7wMtjvM"));
            // X starts with 0 
            Assert.That(BitcoinAddress.FromPrivateKey(new byte[]
                                                      {
                                                          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x99
                                                      }), Is.EqualTo("1FVgHxdCE4sjHoNTVmoygXRWcKzdyXvGhm"));

            // Y starts with 0 
            Assert.That(BitcoinAddress.FromPrivateKey(new byte[]
                                                      {
                                                          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7A
                                                      }), Is.EqualTo("1NwhxtuUW2dg7RPkaypDpLW9JS2dqfEzNb"));
        }
    }
}