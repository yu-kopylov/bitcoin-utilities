using BitcoinUtilities;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;

namespace Test.BitcoinUtilities.Scripts
{
    public class TransactionInputExample
    {
        public TransactionInputExample(string name, int blockHeight, string transaction, int inputIndex, ulong inputValue, string inputScript)
        {
            Name = name;
            BlockHeight = blockHeight;
            Transaction = BitcoinStreamReader.FromBytes(HexUtils.GetBytesUnsafe(transaction), Tx.Read);
            InputIndex = inputIndex;
            InputValue = inputValue;
            InputScript = HexUtils.GetBytesUnsafe(inputScript);
        }

        public string Name { get; }

        public int BlockHeight { get; }
        public Tx Transaction { get; }

        public int InputIndex { get; }
        public ulong InputValue { get; }
        public byte[] InputScript { get; }

        public override string ToString()
        {
            return $"\"{Name}\" {{{BlockHeight} - {HexUtils.GetReversedString(Transaction.Hash)} - {InputIndex}}}";
        }
    }
}