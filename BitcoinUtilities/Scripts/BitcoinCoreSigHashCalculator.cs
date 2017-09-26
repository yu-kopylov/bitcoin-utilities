using System.IO;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Scripts
{
    public class BitcoinCoreSigHashCalculator : ISigHashCalculator
    {
        private readonly Tx transaction;
        private int inputIndex;

        public BitcoinCoreSigHashCalculator(Tx transaction)
        {
            this.transaction = transaction;
        }

        public int InputIndex
        {
            get { return inputIndex; }
            set { inputIndex = value; }
        }

        public byte[] Calculate(SigHashType sigHashType, byte[] subScript)
        {
            // todo: add XMLDOC and test
            // todo: support hashtypes other than SigHashType.All
            MemoryStream mem = new MemoryStream();
            using (BitcoinStreamWriter writer = new BitcoinStreamWriter(mem))
            {
                //todo: check if transaction exists
                writer.Write(transaction.Version);
                writer.WriteCompact((ulong) transaction.Inputs.Length);
                for (int i = 0; i < transaction.Inputs.Length; i++)
                {
                    TxIn input = transaction.Inputs[i];
                    input.PreviousOutput.Write(writer);
                    if (inputIndex == i)
                    {
                        writer.WriteCompact((ulong) subScript.Length);
                        writer.Write(subScript);
                    }
                    else
                    {
                        writer.WriteCompact(0);
                    }
                    writer.Write(input.Sequence);
                }
                writer.WriteArray(transaction.Outputs, (w, v) => v.Write(writer));
                writer.Write(transaction.LockTime);
                writer.Write((uint) sigHashType);
            }

            return mem.ToArray();
        }
    }
}