using System.Net;
using System.Threading;
using BitcoinUtilities;
using BitcoinUtilities.P2P;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P
{
    [TestFixture]
    [Timeout(10000)]
    public class TestBitcoinConnection
    {
        [Test]
        public void TestReadWriteMessage()
        {
            BitcoinMessage receivedMessage = null;
            using (AutoResetEvent messageReceived = new AutoResetEvent(false))
            using (BitcoinConnectionListener listener = BitcoinConnectionListener.StartListener(IPAddress.Loopback, 0, NetworkParameters.BitcoinCoreMain.NetworkMagic, conn =>
            {
                receivedMessage = conn.ReadMessage();
                messageReceived.Set();
                conn.Dispose();
            }))
            {
                using (BitcoinConnection conn = BitcoinConnection.Connect("localhost", listener.Port, NetworkParameters.BitcoinCoreMain.NetworkMagic))
                {
                    byte[] payload = new byte[] {1, 2, 3, 4, 5};

                    conn.WriteMessage(new BitcoinMessage("ABC", payload));

                    Assert.That(messageReceived.WaitOne(5000), Is.True);
                    Assert.AreEqual("ABC", receivedMessage?.Command);
                    Assert.AreEqual(payload, receivedMessage?.Payload);
                }
            }
        }

        [Test]
        public void TestReadWriteWithClosedConnection()
        {
            using (BitcoinConnectionListener listener = BitcoinConnectionListener.StartListener(
                IPAddress.Loopback, 0, NetworkParameters.BitcoinCoreMain.NetworkMagic, conn => { conn.Dispose(); })
            )
            {
                using (BitcoinConnection conn = BitcoinConnection.Connect("localhost", listener.Port, NetworkParameters.BitcoinCoreMain.NetworkMagic))
                {
                    Assert.Throws<BitcoinNetworkException>(() => conn.ReadMessage());
                    Assert.Throws<BitcoinNetworkException>(() => conn.WriteMessage(new BitcoinMessage("ABC", new byte[] {1, 2, 3, 4, 5})));
                }
            }
        }

        [Test]
        public void TestWriteReadWithClosedConnection()
        {
            using (BitcoinConnectionListener listener = BitcoinConnectionListener.StartListener(
                IPAddress.Loopback, 0, NetworkParameters.BitcoinCoreMain.NetworkMagic, conn => { conn.Dispose(); })
            )
            {
                using (BitcoinConnection conn = BitcoinConnection.Connect("localhost", listener.Port, NetworkParameters.BitcoinCoreMain.NetworkMagic))
                {
                    Thread.Sleep(100);

                    Assert.Throws<BitcoinNetworkException>(() => conn.WriteMessage(new BitcoinMessage("ABC", new byte[] {1, 2, 3, 4, 5})));
                    Assert.Throws<BitcoinNetworkException>(() => conn.ReadMessage());
                }
            }
        }

        [Test]
        public void TestRepeatableDispose()
        {
            using (BitcoinConnectionListener listener = BitcoinConnectionListener.StartListener(
                IPAddress.Loopback, 0, NetworkParameters.BitcoinCoreMain.NetworkMagic, conn => { conn.Dispose(); })
            )
            {
                using (BitcoinConnection conn = BitcoinConnection.Connect("localhost", listener.Port, NetworkParameters.BitcoinCoreMain.NetworkMagic))
                {
                    conn.Dispose();
                    Assert.Throws<BitcoinNetworkException>(() => conn.WriteMessage(new BitcoinMessage("ABC", new byte[] {1, 2, 3, 4, 5})));
                    conn.Dispose();
                }
            }
        }

        [Test]
        public void TestFailedToConnect()
        {
            Assert.Throws<BitcoinNetworkException>(() =>
            {
                using (BitcoinConnection.Connect("0.0.0.0", -1, NetworkParameters.BitcoinCoreMain.NetworkMagic))
                {
                    Assert.Fail("connection should not be created");
                }
            });
        }
    }
}