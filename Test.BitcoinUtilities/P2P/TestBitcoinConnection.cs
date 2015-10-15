﻿using System;
using BitcoinUtilities.P2P;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P
{
    [TestFixture]
    public class TestBitcoinConnection
    {
        [Test]
        public void Test()
        {
            byte[] hello = new byte[]
                           {
                               0x62, 0xea, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11, 0xb2, 0xd0, 0x50,
                               0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                               0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                               0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                               0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3b, 0x2e, 0xb3, 0x5d, 0x8c, 0xe6, 0x17, 0x65,
                               0x0f, 0x2f, 0x53, 0x61, 0x74, 0x6f, 0x73, 0x68, 0x69, 0x3a, 0x30, 0x2e, 0x37, 0x2e, 0x32, 0x2f,
                               0xc0, 0x3e, 0x03, 0x00
                           };

            using (BitcoinConnection conn = new BitcoinConnection())
            {
                conn.Connect("localhost", 8333);

                conn.WriteMessage(new BitcoinMessage("version", hello));

                BitcoinMessage incVersionMessage = conn.ReadMessage();
                //todo: move to const
                if (incVersionMessage.Command != "version")
                {
                    //todo: handle correctly
                    throw new Exception("Handshake should start with Version message.");
                }

                conn.WriteMessage(new BitcoinMessage("verack", new byte[0]));

                BitcoinMessage incVerAckMessage = conn.ReadMessage();
                //todo: move to const
                if (incVerAckMessage.Command != "verack")
                {
                    //todo: handle correctly
                    throw new Exception("Version message should be followed by VerAck message.");
                }

                BitcoinMessage message = conn.ReadMessage();
                Console.WriteLine(message.Command);
            }
        }
    }
}