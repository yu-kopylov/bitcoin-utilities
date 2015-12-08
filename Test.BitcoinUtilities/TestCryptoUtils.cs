using System.Text;
using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestCryptoUtils
    {
        [Test]
        public void Test()
        {
            //todo: test exceptions
            byte[] hash = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("hello"));
            Assert.That(hash, Is.EqualTo(new byte[]
                                         {
                                             0x95, 0x95, 0xc9, 0xdf, 0x90, 0x07, 0x51, 0x48, 0xeb, 0x06, 0x86, 0x03, 0x65, 0xdf, 0x33, 0x58,
                                             0x4b, 0x75, 0xbf, 0xf7, 0x82, 0xa5, 0x10, 0xc6, 0xcd, 0x48, 0x83, 0xa4, 0x19, 0x83, 0x3d, 0x50
                                         }));
        }
    }
}