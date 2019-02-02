using System;
using BitcoinUtilities;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;

namespace Test.BitcoinUtilities.Scripts
{
    public class TransactionInputExample
    {
        public TransactionInputExample(string name, int blockHeight, string transaction, int inputIndex, ulong inputValue, string inputScript)
        {
            byte[] transactionText = HexUtils.GetBytesUnsafe(transaction);

            byte[] transactionHash = CryptoUtils.DoubleSha256(transactionText);
            Array.Reverse(transactionHash);

            Name = name;
            BlockHeight = blockHeight;
            Transaction = BitcoinStreamReader.FromBytes(transactionText, Tx.Read);
            TransactionHash = HexUtils.GetString(transactionHash);
            InputIndex = inputIndex;
            InputValue = inputValue;
            InputScript = HexUtils.GetBytesUnsafe(inputScript);
        }

        public string Name { get; }

        public int BlockHeight { get; }
        public Tx Transaction { get; }
        public string TransactionHash { get; }

        public int InputIndex { get; }
        public ulong InputValue { get; }
        public byte[] InputScript { get; }

        public override string ToString()
        {
            return $"\"{Name}\" {{{BlockHeight} - {TransactionHash} - {InputIndex}}}";
        }
    }
}