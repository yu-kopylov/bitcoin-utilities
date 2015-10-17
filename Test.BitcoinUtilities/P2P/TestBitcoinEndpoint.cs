using System;
using System.IO;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P
{
    [TestFixture]
    public class TestBitcoinEndpoint
    {
        [Test]
        [Explicit]
        public void Test()
        {
            using (BitcoinEndpoint endpoint = new BitcoinEndpoint(LoggingMessageHandler))
            {
                endpoint.Connect("localhost", 8333);
                System.Threading.Thread.Sleep(30000);
            }
        }

        private bool LoggingMessageHandler(BitcoinEndpoint endpoint, BitcoinMessage message)
        {
            Console.WriteLine(">>command: {0}, payload: {1} bytes", message.Command, message.Payload.Length);

            if (message.Command == InvMessage.Command)
            {
                MemoryStream mem = new MemoryStream(message.Payload);
                using (BitcoinStreamReader reader = new BitcoinStreamReader(mem))
                {
                    Console.WriteLine(">>payload: {0}", BitConverter.ToString(message.Payload));
                    InvMessage invMessage = InvMessage.Read(reader);
                    foreach (InventoryVector vector in invMessage.Inventory)
                    {
                        Console.WriteLine("\t{0} {1}", vector.Type.ToString().PadRight(16), BitConverter.ToString(vector.Hash));
                    }
                }
            }

            return true;
        }
    }
}