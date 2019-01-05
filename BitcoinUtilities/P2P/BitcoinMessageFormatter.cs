using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BitcoinUtilities.P2P.Messages;

namespace BitcoinUtilities.P2P
{
    public class BitcoinMessageFormatter
    {
        private readonly string linePrefix;

        private StringBuilder sb;
        private bool firstLine;

        public BitcoinMessageFormatter(string linePrefix)
        {
            this.linePrefix = linePrefix;
        }

        public string Format(IBitcoinMessage message)
        {
            sb = new StringBuilder();
            firstLine = true;

            if (message == null)
            {
                AppendLine("<null>");
                return sb.ToString();
            }

            FormatValue("command", message.Command, v => v);

            if (message is GetBlocksMessage)
            {
                GetBlocksMessage typedMessage = (GetBlocksMessage) message;
                FormatCollection("locator hashes", typedMessage.LocatorHashes, HexUtils.GetString);
                FormatValue("hash stop", typedMessage.HashStop, HexUtils.GetString);
            }
            else if (message is GetDataMessage)
            {
                GetDataMessage typedMessage = (GetDataMessage) message;
                FormatFullCollection("inventory items", typedMessage.Inventory, v => string.Format("{0}\t{1}", v.Type, HexUtils.GetString(v.Hash)));
            }
            else if (message is InvMessage)
            {
                InvMessage typedMessage = (InvMessage) message;
                FormatFullCollection("inventory items", typedMessage.Inventory, v => string.Format("{0}\t{1}", v.Type, HexUtils.GetString(v.Hash)));
            }
            else if (message is BlockMessage)
            {
                BlockMessage typedMessage = (BlockMessage) message;
                FormatValue("header hash", typedMessage.BlockHeader, v => HexUtils.GetString(CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(v.Write))));
                FormatCollection("transactions", typedMessage.Transactions, v => HexUtils.GetString(CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(v.Write))));
            }
            else if (message is HeadersMessage)
            {
                HeadersMessage typedMessage = (HeadersMessage) message;
                FormatCollection("headers", typedMessage.Headers, header => HexUtils.GetString(CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(header.Write))));
            }
            else if (message is GetHeadersMessage)
            {
                GetHeadersMessage typedMessage = (GetHeadersMessage) message;
                FormatValue("protocol version", typedMessage.ProtocolVersion, i => i.ToString());
                FormatCollection("locator hashes", typedMessage.LocatorHashes, HexUtils.GetString);
                FormatValue("hash stop", typedMessage.HashStop, HexUtils.GetString);
            }
            else
            {
                AppendLine("<Formatting is not supported for type: {0}>", message.GetType().Name);
            }

            return sb.ToString();
        }

        private void FormatValue<T>(string parameterName, T parameterValue, Func<T, string> format)
        {
            AppendLine("{0}: {1}", parameterName, parameterValue == null ? "null" : format(parameterValue));
        }

        private void FormatCollection<T>(string collectionName, ICollection<T> items, Func<T, string> format)
        {
            if (items.Count == 0)
            {
                AppendLine("{0} [0 items]", collectionName);
            }
            else if (items.Count == 1)
            {
                AppendLine("{0} [1 item]:", collectionName);
            }
            else
            {
                AppendLine("{0} [{1} items]:", collectionName, items.Count);
            }

            if (items.Count > 0)
            {
                T item = items.First();
                AppendLine("\t{0}", item == null ? "<null>" : format(item));
            }

            if (items.Count > 2)
            {
                AppendLine("\t...");
            }

            if (items.Count > 1)
            {
                T item = items.Last();
                AppendLine("\t{0}", item == null ? "<null>" : format(item));
            }
        }

        private void FormatFullCollection<T>(string collectionName, ICollection<T> items, Func<T, string> format)
        {
            if (items.Count == 0)
            {
                AppendLine("{0} [0 items]", collectionName);
            }
            else if (items.Count == 1)
            {
                AppendLine("{0} [1 item]:", collectionName);
            }
            else
            {
                AppendLine("{0} [{1} items]:", collectionName, items.Count);
            }

            foreach (T item in items)
            {
                AppendLine("\t{0}", item == null ? "<null>" : format(item));
            }
        }

        private void AppendLine(string format, params object[] args)
        {
            if (firstLine)
            {
                firstLine = false;
            }
            else
            {
                sb.Append("\n");
            }

            sb.Append(linePrefix);
            sb.AppendFormat(format, args);
        }
    }
}