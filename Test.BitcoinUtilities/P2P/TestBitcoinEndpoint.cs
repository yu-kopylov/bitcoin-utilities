using System.Net;
using System.Threading;
using BitcoinUtilities;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using NUnit.Framework;
using TestUtilities;

namespace Test.BitcoinUtilities.P2P
{
    [TestFixture]
    [Timeout(10000)]
    public class TestBitcoinEndpoint
    {
        private MessageLog messageLog;

        [SetUp]
        public void SetUp()
        {
            messageLog = new MessageLog();
        }

        [Test]
        public void TestClientServerMessaging()
        {
            using (var connectionListener = BitcoinConnectionListener.StartListener(
                IPAddress.Loopback, 0, NetworkParameters.BitcoinCoreMain.NetworkMagic, conn => CreateServerEndpoint(conn))
            )
            {
                using (BitcoinEndpoint endpoint = BitcoinEndpoint.Create("localhost", connectionListener.Port, NetworkParameters.BitcoinCoreMain.NetworkMagic))
                {
                    StartMessageListener(endpoint, false);
                    Thread.Sleep(100);

                    Assert.That(messageLog.GetLog(), Is.EquivalentTo(new string[]
                    {
                        "Server accepted connection.",
                        "Server started listener.",
                        "Client started listener."
                    }));
                    messageLog.Clear();

                    endpoint.WriteMessage(new GetAddrMessage());
                    endpoint.WriteMessage(new GetAddrMessage());
                    Thread.Sleep(100);

                    Assert.That(messageLog.GetLog(), Is.EquivalentTo(new string[]
                    {
                        "Server got message: getaddr",
                        "Server got message: getaddr",
                        "Client got message: addr",
                        "Client got message: addr"
                    }));
                    messageLog.Clear();
                }

                Thread.Sleep(100);
                Assert.That(messageLog.GetLog(), Is.EquivalentTo(new string[]
                {
                    "Server endpoint disconnected.",
                    "Client endpoint disconnected."
                }));
            }
        }

        [Test]
        public void TestDisconnectByServer()
        {
            BitcoinEndpoint serverEndpoint = null;
            using (var connectionListener = BitcoinConnectionListener.StartListener(
                IPAddress.Loopback, 0, NetworkParameters.BitcoinCoreMain.NetworkMagic, conn => serverEndpoint = CreateServerEndpoint(conn)
            ))
            {
                using (BitcoinEndpoint clientEndpoint = BitcoinEndpoint.Create("localhost", connectionListener.Port, NetworkParameters.BitcoinCoreMain.NetworkMagic))
                {
                    StartMessageListener(clientEndpoint, false);
                    Thread.Sleep(100);

                    Assert.That(messageLog.GetLog(), Is.EquivalentTo(new string[]
                    {
                        "Server accepted connection.",
                        "Server started listener.",
                        "Client started listener."
                    }));
                    messageLog.Clear();

                    Assert.NotNull(serverEndpoint);
                    serverEndpoint.Dispose();
                    Thread.Sleep(100);

                    Assert.That(messageLog.GetLog(), Is.EquivalentTo(new string[]
                    {
                        "Server endpoint disconnected.",
                        "Client endpoint disconnected."
                    }));
                    messageLog.Clear();

                    Assert.Throws<BitcoinNetworkException>(() => serverEndpoint.WriteMessage(new GetAddrMessage()));
                    Assert.Throws<BitcoinNetworkException>(() => clientEndpoint.WriteMessage(new GetAddrMessage()));
                }

                Thread.Sleep(100);
                Assert.That(messageLog.GetLog(), Is.EquivalentTo(new string[0]));
            }
        }

        [Test]
        public void TestRepeatableDispose()
        {
            BitcoinEndpoint serverEndpoint = null;
            using (var connectionListener = BitcoinConnectionListener.StartListener(
                IPAddress.Loopback, 0, NetworkParameters.BitcoinCoreMain.NetworkMagic, conn => serverEndpoint = CreateServerEndpoint(conn)
            ))
            {
                using (BitcoinEndpoint clientEndpoint = BitcoinEndpoint.Create("localhost", connectionListener.Port, NetworkParameters.BitcoinCoreMain.NetworkMagic))
                {
                    StartMessageListener(clientEndpoint, false);
                    Thread.Sleep(100);

                    Assert.That(messageLog.GetLog(), Is.EquivalentTo(new string[]
                    {
                        "Server accepted connection.",
                        "Server started listener.",
                        "Client started listener."
                    }));
                    messageLog.Clear();

                    Assert.NotNull(serverEndpoint);

                    serverEndpoint.Dispose();
                    clientEndpoint.Dispose();

                    serverEndpoint.Dispose();
                    clientEndpoint.Dispose();

                    Thread.Sleep(100);

                    Assert.That(messageLog.GetLog(), Is.EquivalentTo(new string[]
                    {
                        "Server endpoint disconnected.",
                        "Client endpoint disconnected."
                    }));
                    messageLog.Clear();

                    Assert.Throws<BitcoinNetworkException>(() => serverEndpoint.WriteMessage(new GetAddrMessage()));
                    Assert.Throws<BitcoinNetworkException>(() => clientEndpoint.WriteMessage(new GetAddrMessage()));
                }

                Thread.Sleep(100);
                Assert.That(messageLog.GetLog(), Is.EquivalentTo(new string[0]));
            }
        }

        [Test]
        public void TestDisposeFromMessageHandler()
        {
            BitcoinEndpoint serverEndpoint = null;
            using (var connectionListener = BitcoinConnectionListener.StartListener(
                IPAddress.Loopback, 0, NetworkParameters.BitcoinCoreMain.NetworkMagic, conn =>
                {
                    messageLog.Log("Server accepted connection.");
                    serverEndpoint = BitcoinEndpoint.Create(conn);
                    serverEndpoint.StartListener((_, __) =>
                    {
                        serverEndpoint.Dispose();
                        messageLog.Log("Server accepted message.");
                    }, _ => { messageLog.Log("Server endpoint disconnected."); });
                    messageLog.Log("Server started listener.");
                }
            ))
            {
                using (BitcoinEndpoint clientEndpoint = BitcoinEndpoint.Create("localhost", connectionListener.Port, NetworkParameters.BitcoinCoreMain.NetworkMagic))
                {
                    StartMessageListener(clientEndpoint, false);
                    Thread.Sleep(100);

                    Assert.That(messageLog.GetLog(), Is.EquivalentTo(new string[]
                    {
                        "Server accepted connection.",
                        "Server started listener.",
                        "Client started listener."
                    }));
                    messageLog.Clear();

                    clientEndpoint.WriteMessage(new GetAddrMessage());
                    Thread.Sleep(100);

                    Assert.That(messageLog.GetLog(), Is.EquivalentTo(new string[]
                    {
                        "Server accepted message.",
                        "Server endpoint disconnected.",
                        "Client endpoint disconnected."
                    }));
                    messageLog.Clear();

                    Assert.NotNull(serverEndpoint);
                    Assert.Throws<BitcoinNetworkException>(() => serverEndpoint.WriteMessage(new GetAddrMessage()));
                    Assert.Throws<BitcoinNetworkException>(() => clientEndpoint.WriteMessage(new GetAddrMessage()));
                }

                Thread.Sleep(100);
                Assert.That(messageLog.GetLog(), Is.EquivalentTo(new string[0]));
            }
        }

        [Test]
        public void TestDisposeFromDisconnectHandler()
        {
            BitcoinEndpoint serverEndpoint = null;
            using (var connectionListener = BitcoinConnectionListener.StartListener(
                IPAddress.Loopback, 0, NetworkParameters.BitcoinCoreMain.NetworkMagic, conn =>
                {
                    messageLog.Log("Server accepted connection.");
                    serverEndpoint = BitcoinEndpoint.Create(conn);
                    serverEndpoint.StartListener((_, __) => { }, _ =>
                    {
                        serverEndpoint.Dispose();
                        messageLog.Log("Server endpoint disconnected.");
                    });
                    messageLog.Log("Server started listener.");
                }
            ))
            {
                using (BitcoinEndpoint clientEndpoint = BitcoinEndpoint.Create("localhost", connectionListener.Port, NetworkParameters.BitcoinCoreMain.NetworkMagic))
                {
                    StartMessageListener(clientEndpoint, false);
                    Thread.Sleep(100);

                    Assert.That(messageLog.GetLog(), Is.EquivalentTo(new string[]
                    {
                        "Server accepted connection.",
                        "Server started listener.",
                        "Client started listener."
                    }));
                    messageLog.Clear();

                    serverEndpoint.Dispose();
                    Thread.Sleep(100);

                    Assert.That(messageLog.GetLog(), Is.EquivalentTo(new string[]
                    {
                        "Server endpoint disconnected.",
                        "Client endpoint disconnected."
                    }));
                    messageLog.Clear();

                    Assert.NotNull(serverEndpoint);
                    Assert.Throws<BitcoinNetworkException>(() => serverEndpoint.WriteMessage(new GetAddrMessage()));
                    Assert.Throws<BitcoinNetworkException>(() => clientEndpoint.WriteMessage(new GetAddrMessage()));
                }

                Thread.Sleep(100);
                Assert.That(messageLog.GetLog(), Is.EquivalentTo(new string[0]));
            }
        }

        private BitcoinEndpoint CreateServerEndpoint(BitcoinConnection connection)
        {
            messageLog.Log("Server accepted connection.");
            BitcoinEndpoint endpoint = BitcoinEndpoint.Create(connection);
            StartMessageListener(endpoint, true);
            return endpoint;
        }

        private void StartMessageListener(BitcoinEndpoint endpoint, bool server)
        {
            string name = server ? "Server" : "Client";
            endpoint.StartListener(
                (_, message) =>
                {
                    messageLog.Log($"{name} got message: {message.Command}");
                    if (message is GetAddrMessage)
                    {
                        endpoint.WriteMessage(new AddrMessage(new NetAddr[0]));
                    }
                },
                ep => { messageLog.Log($"{name} endpoint disconnected."); }
            );
            messageLog.Log($"{name} started listener.");
        }
    }
}