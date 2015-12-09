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
                FormatCollection("locator hashes", typedMessage.LocatorHashes, v => BitConverter.ToString(v));
                FormatValue("hash stop", typedMessage.HashStop, v => BitConverter.ToString(v));
            }
            else if (message is GetDataMessage)
            {
                GetDataMessage typedMessage = (GetDataMessage) message;
                FormatCollection("inventory items", typedMessage.Inventory, v => string.Format("{0}\t{1}", v.Type, BitConverter.ToString(v.Hash)));
            }
            else if (message is InvMessage)
            {
                InvMessage typedMessage = (InvMessage) message;
                FormatCollection("inventory items", typedMessage.Inventory, v => string.Format("{0}\t{1}", v.Type, BitConverter.ToString(v.Hash)));
            }
            else if (message is BlockMessage)
            {
                BlockMessage typedMessage = (BlockMessage) message;
                FormatValue("header hash", typedMessage.BlockHeader, v => BitConverter.ToString(CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(v.Write))));
                FormatCollection("transactions", typedMessage.Transactions, v => BitConverter.ToString(CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(v.Write))));
            }
            else
            {
                AppendLine("<Formatting is not supported for type: {0}>", message.GetType().Name);
            }

            return sb.ToString();
        }

        private void FormatValue<T>(string parameterName, T parameterValue, Func<T, string> format)
        {
            AppendLine("{0}:", parameterName);
            AppendLine("\t{0}", parameterValue == null ? "null" : format(parameterValue));
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