using System.Net;
using System.Threading;
using BitcoinUtilities.P2P;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P
{
    [TestFixture]
    public class TestBitcoinConnection
    {
        [Test]
        public void TestReadWriteMessage()
        {
            BitcoinMessage receivedMessage = null;
            using (AutoResetEvent messageReceived = new AutoResetEvent(false))
            using (BitcoinConnectionListener listener = new BitcoinConnectionListener(IPAddress.Loopback, 0, conn =>
            {
                receivedMessage = conn.ReadMessage();
                messageReceived.Set();
                conn.Dispose();
            }))
            {
                listener.Start();

                using (BitcoinConnection conn = BitcoinConnection.Connect("localhost", listener.Port))
                {
                    byte[] payload = new byte[] {1, 2, 3, 4, 5};

                    conn.WriteMessage(new BitcoinMessage("ABC", payload));

                    Assert.That(messageReceived.WaitOne(5000), Is.True);
                    Assert.AreEqual("ABC", receivedMessage?.Command);
                    Assert.AreEqual(payload, receivedMessage?.Payload);
                }

                listener.Stop();
            }
        }

        [Test]
        public void TestReadWriteWithClosedConnection()
        {
            using (BitcoinConnectionListener listener = new BitcoinConnectionListener(IPAddress.Loopback, 0, conn => { conn.Dispose(); }))
            {
                listener.Start();

                using (BitcoinConnection conn = BitcoinConnection.Connect("localhost", listener.Port))
                {
                    // todo: Had to disconnect on the client side. Disconnect on the server side does not stop read operation.
                    conn.Dispose();

                    Assert.Throws<BitcoinNetworkException>(() => conn.ReadMessage());
                    Assert.Throws<BitcoinNetworkException>(() => conn.WriteMessage(new BitcoinMessage("ABC", new byte[] {1, 2, 3, 4, 5})));
                }

                listener.Stop();
            }
        }

        [Test]
        public void TestWriteReadWithClosedConnection()
        {
            using (BitcoinConnectionListener listener = new BitcoinConnectionListener(IPAddress.Loopback, 0, conn => { conn.Dispose(); }))
            {
                listener.Start();

                using (BitcoinConnection conn = BitcoinConnection.Connect("localhost", listener.Port))
                {
                    Thread.Sleep(100);
                    Assert.Throws<BitcoinNetworkException>(() => conn.WriteMessage(new BitcoinMessage("ABC", new byte[] {1, 2, 3, 4, 5})));
                    Assert.Throws<BitcoinNetworkException>(() => conn.ReadMessage());
                }

                listener.Stop();
            }
        }

        [Test]
        public void TestRepeatableDispose()
        {
            using (BitcoinConnectionListener listener = new BitcoinConnectionListener(IPAddress.Loopback, 0, conn => { conn.Dispose(); }))
            {
                listener.Start();

                using (BitcoinConnection conn = BitcoinConnection.Connect("localhost", listener.Port))
                {
                    conn.Dispose();
                    Assert.Throws<BitcoinNetworkException>(() => conn.WriteMessage(new BitcoinMessage("ABC", new byte[] {1, 2, 3, 4, 5})));
                    conn.Dispose();
                }

                listener.Stop();
            }
        }

        [Test]
        public void TestFailedToConnect()
        {
            Assert.Throws<BitcoinNetworkException>(() =>
            {
                using (BitcoinConnection.Connect("0.0.0.0", -1))
                {
                    Assert.Fail("connection should not be created");
                }
            });
        }
    }
}