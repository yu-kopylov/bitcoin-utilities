using System.Net;
using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestIpUtils
    {
        [Test]
        public void Test()
        {
            Assert.That(IpUtils.MapToIPv6(IPAddress.Parse("192.168.0.1")).GetAddressBytes(), Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xC0, 0xA8, 0x00, 0x01
            }));
        }
    }
}