using System;
using BitcoinUtilities.P2P;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P
{
    [TestFixture]
    public class TestBitcoinEndpoint
    {
        [Test]
        public void Test()
        {
            using (BitcoinEndpoint endpoint = new BitcoinEndpoint(LoggingMessageHandler))
            {
                endpoint.Connect("localhost", 8333);
            }
        }

        private bool LoggingMessageHandler(BitcoinEndpoint endpoint, BitcoinMessage message)
        {
            Console.WriteLine(">>command: {0}, payload: {1} bytes", message.Command, message.Payload.Length);
            return true;
        }
    }
}