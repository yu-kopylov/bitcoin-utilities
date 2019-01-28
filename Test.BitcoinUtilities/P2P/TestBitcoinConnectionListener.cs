using System;
using System.Net;
using System.Threading;
using BitcoinUtilities;
using BitcoinUtilities.P2P;
using NUnit.Framework;
using TestUtilities;

namespace Test.BitcoinUtilities.P2P
{
    [TestFixture]
    [Timeout(10000)]
    public class TestBitcoinConnectionListener
    {
        [Test]
        public void TestTwoClients()
        {
            using (BitcoinConnectionListener listener = BitcoinConnectionListener.StartListener(IPAddress.Loopback, 0, NetworkParameters.BitcoinCoreMain.NetworkMagic, conn =>
            {
                conn.WriteMessage(new BitcoinMessage("TEST", new byte[] {1, 2, 3}));
                Thread.Sleep(100);
                conn.Dispose();
            }))
            {
                for (int i = 0; i < 2; i++)
                {
                    using (BitcoinConnection client = BitcoinConnection.Connect("localhost", listener.Port, NetworkParameters.BitcoinCoreMain.NetworkMagic))
                    {
                        BitcoinMessage message = client.ReadMessage();
                        Assert.That(message.Command, Is.EqualTo("TEST"));
                        Assert.That(message.Payload, Is.EqualTo(new byte[] {1, 2, 3}));
                    }
                }
            }
        }

        [Test]
        public void TestConnectionAfterFailure()
        {
            MessageLog log = new MessageLog();
            using (BitcoinConnectionListener listener = BitcoinConnectionListener.StartListener(IPAddress.Loopback, 0, NetworkParameters.BitcoinCoreMain.NetworkMagic, conn =>
            {
                log.Log("connection accepted");
                throw new Exception("Test");
            }))
            {
                using (BitcoinConnection.Connect("localhost", listener.Port, NetworkParameters.BitcoinCoreMain.NetworkMagic))
                {
                    Thread.Sleep(100);
                }

                Assert.AreEqual(new string[] {"connection accepted"}, log.GetLog());
                log.Clear();

                using (BitcoinConnection.Connect("localhost", listener.Port, NetworkParameters.BitcoinCoreMain.NetworkMagic))
                {
                    Thread.Sleep(100);
                }

                Assert.AreEqual(new string[] {"connection accepted"}, log.GetLog());
            }
        }

        [Test]
        public void TestRepeatableDispose()
        {
            using (BitcoinConnectionListener listener = BitcoinConnectionListener.StartListener(
                IPAddress.Loopback, 0, NetworkParameters.BitcoinCoreMain.NetworkMagic, conn => conn.Dispose())
            )
            {
                listener.Dispose();
                listener.Dispose();
            }
        }
    }
}