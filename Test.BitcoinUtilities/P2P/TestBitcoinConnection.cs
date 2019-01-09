using System;
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
            Console.WriteLine($"{nameof(TestBitcoinConnection)}.{nameof(TestReadWriteMessage)} started.");

            BitcoinMessage receivedMessage = null;
            using (AutoResetEvent messageReceived = new AutoResetEvent(false))
            using (BitcoinConnectionListener listener = BitcoinConnectionListener.StartListener(IPAddress.Loopback, 0, conn =>
            {
                receivedMessage = conn.ReadMessage();
                messageReceived.Set();
                conn.Dispose();
            }))
            {
                using (BitcoinConnection conn = BitcoinConnection.Connect("localhost", listener.Port))
                {
                    byte[] payload = new byte[] {1, 2, 3, 4, 5};

                    conn.WriteMessage(new BitcoinMessage("ABC", payload));

                    Assert.That(messageReceived.WaitOne(5000), Is.True);
                    Assert.AreEqual("ABC", receivedMessage?.Command);
                    Assert.AreEqual(payload, receivedMessage?.Payload);
                }
            }

            Console.WriteLine($"{nameof(TestBitcoinConnection)}.{nameof(TestReadWriteMessage)} completed.");
        }

        [Test]
        public void TestReadWriteWithClosedConnection()
        {
            Console.WriteLine($"{nameof(TestBitcoinConnection)}.{nameof(TestReadWriteWithClosedConnection)} started.");

            using (BitcoinConnectionListener listener = BitcoinConnectionListener.StartListener(IPAddress.Loopback, 0, conn => { conn.Dispose(); }))
            {
                using (BitcoinConnection conn = BitcoinConnection.Connect("localhost", listener.Port))
                {
                    // todo: Had to disconnect on the client side. Disconnect on the server side does not stop read operation (happens on Windows).
                    conn.Dispose();

                    Assert.Throws<BitcoinNetworkException>(() => conn.ReadMessage());
                    Assert.Throws<BitcoinNetworkException>(() => conn.WriteMessage(new BitcoinMessage("ABC", new byte[] {1, 2, 3, 4, 5})));
                }
            }

            Console.WriteLine($"{nameof(TestBitcoinConnection)}.{nameof(TestReadWriteWithClosedConnection)} completed.");
        }

        [Test]
        public void TestWriteReadWithClosedConnection()
        {
            Console.WriteLine($"{nameof(TestBitcoinConnection)}.{nameof(TestWriteReadWithClosedConnection)} started.");

            using (BitcoinConnectionListener listener = BitcoinConnectionListener.StartListener(IPAddress.Loopback, 0, conn => { conn.Dispose(); }))
            {
                using (BitcoinConnection conn = BitcoinConnection.Connect("localhost", listener.Port))
                {
                    Thread.Sleep(100);

                    // todo: Had to disconnect on the client side. Disconnect on the server side does not stop read operation (happens on Linux/Mono).
                    conn.Dispose();

                    Assert.Throws<BitcoinNetworkException>(() => conn.WriteMessage(new BitcoinMessage("ABC", new byte[] {1, 2, 3, 4, 5})));
                    Assert.Throws<BitcoinNetworkException>(() => conn.ReadMessage());
                }
            }

            Console.WriteLine($"{nameof(TestBitcoinConnection)}.{nameof(TestWriteReadWithClosedConnection)} completed.");
        }

        [Test]
        public void TestRepeatableDispose()
        {
            Console.WriteLine($"{nameof(TestBitcoinConnection)}.{nameof(TestRepeatableDispose)} started.");

            using (BitcoinConnectionListener listener = BitcoinConnectionListener.StartListener(IPAddress.Loopback, 0, conn => { conn.Dispose(); }))
            {
                using (BitcoinConnection conn = BitcoinConnection.Connect("localhost", listener.Port))
                {
                    conn.Dispose();
                    Assert.Throws<BitcoinNetworkException>(() => conn.WriteMessage(new BitcoinMessage("ABC", new byte[] {1, 2, 3, 4, 5})));
                    conn.Dispose();
                }
            }

            Console.WriteLine($"{nameof(TestBitcoinConnection)}.{nameof(TestRepeatableDispose)} completed.");
        }

        [Test]
        public void TestFailedToConnect()
        {
            Console.WriteLine($"{nameof(TestBitcoinConnection)}.{nameof(TestFailedToConnect)} started.");

            Assert.Throws<BitcoinNetworkException>(() =>
            {
                using (BitcoinConnection.Connect("0.0.0.0", -1))
                {
                    Assert.Fail("connection should not be created");
                }
            });

            Console.WriteLine($"{nameof(TestBitcoinConnection)}.{nameof(TestFailedToConnect)} completed.");
        }
    }
}