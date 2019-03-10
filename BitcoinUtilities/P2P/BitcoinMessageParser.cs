using System;
using System.Collections.Generic;
using System.IO;
using BitcoinUtilities.P2P.Messages;

namespace BitcoinUtilities.P2P
{
    public static class BitcoinMessageParser
    {
        private static readonly Dictionary<string, Func<BitcoinStreamReader, IBitcoinMessage>> messageReadMethods =
            new Dictionary<string, Func<BitcoinStreamReader, IBitcoinMessage>>();

        static BitcoinMessageParser()
        {
            messageReadMethods.Add(AddrMessage.Command, AddrMessage.Read);
            messageReadMethods.Add(BlockMessage.Command, BlockMessage.Read);
            messageReadMethods.Add(GetAddrMessage.Command, GetAddrMessage.Read);
            messageReadMethods.Add(GetBlocksMessage.Command, GetBlocksMessage.Read);
            messageReadMethods.Add(GetDataMessage.Command, GetDataMessage.Read);
            messageReadMethods.Add(GetHeadersMessage.Command, GetHeadersMessage.Read);
            messageReadMethods.Add(HeadersMessage.Command, HeadersMessage.Read);
            messageReadMethods.Add(InvMessage.Command, InvMessage.Read);
            messageReadMethods.Add(MerkleBlockMessage.Command, MerkleBlockMessage.Read);
            messageReadMethods.Add(PingMessage.Command, PingMessage.Read);
            messageReadMethods.Add(PongMessage.Command, PongMessage.Read);
            messageReadMethods.Add(RejectMessage.Command, RejectMessage.Read);
            messageReadMethods.Add(SendHeadersMessage.Command, SendHeadersMessage.Read);
            messageReadMethods.Add(VerAckMessage.Command, VerAckMessage.Read);
            messageReadMethods.Add(VersionMessage.Command, VersionMessage.Read);
        }

        public static IBitcoinMessage Parse(BitcoinMessage message)
        {
            Func<BitcoinStreamReader, IBitcoinMessage> readMethod;
            if (!messageReadMethods.TryGetValue(message.Command, out readMethod))
            {
                return null;
            }

            MemoryStream mem = new MemoryStream(message.Payload);
            using (BitcoinStreamReader reader = new BitcoinStreamReader(mem))
            {
                return readMethod(reader);
            }
        }
    }
}