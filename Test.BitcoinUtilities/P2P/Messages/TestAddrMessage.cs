using System;
using System.IO;
using System.Net;
using BitcoinUtilities;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P.Messages
{
    [TestFixture]
    class TestAddrMessage
    {
        [Test]
        public void Test()
        {
            byte[] inBytes = new byte[]
            {
                // 1 address in this message
                0x01,
                // Mon Dec 20 21:50:10 EST 2010 (Tue, 21 Dec 2010 02:50:10 GMT)
                0xE2, 0x15, 0x10, 0x4D,
                // 1 (NODE_NETWORK service - see version message)
                0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // IPv4: 10.0.0.1, IPv6: ::ffff:10.0.0.1(IPv4 - mapped IPv6 address)
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x0A, 0x00, 0x00, 0x01,
                // port 8333
                0x20, 0x8D
            };

            AddrMessage message;

            MemoryStream inStream = new MemoryStream(inBytes);
            using (BitcoinStreamReader reader = new BitcoinStreamReader(inStream))
            {
                message = AddrMessage.Read(reader);
            }

            Assert.That(message.AddressList.Length, Is.EqualTo(1));

            NetAddr address = message.AddressList[0];

            Assert.That(UnixTime.ToDateTime(address.Timestamp), Is.EqualTo(new DateTime(2010, 12, 21, 02, 50, 10, DateTimeKind.Utc)));
            Assert.That(address.Services, Is.EqualTo(1));
            Assert.That(address.Address, Is.EqualTo(IPAddress.Parse("10.0.0.1").MapToIPv6()));
            Assert.That(address.Port, Is.EqualTo(8333));

            byte[] outBytes = BitcoinStreamWriter.GetBytes(message.Write);
            Assert.That(outBytes, Is.EqualTo(inBytes));
        }
    }
}