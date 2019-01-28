using System;
using System.IO;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Scripts
{
    /// <summary>
    /// Specification: https://en.bitcoin.it/wiki/OP_CHECKSIG
    /// </summary>
    public class BitcoinCoreSigHashCalculator : ISigHashCalculator
    {
        private readonly Tx transaction;

        public BitcoinCoreSigHashCalculator(Tx transaction)
        {
            this.transaction = transaction;
        }

        public int InputIndex { get; set; }

        /// <summary>
        /// Bitcoin Core does not sign output values from original transactions.
        /// </summary>
        public ulong Amount
        {
            get { return 0; }
            set { }
        }

        public byte[] Calculate(SigHashType sigHashType, byte[] subScript)
        {
            if (sigHashType == SigHashType.All)
            {
                return Calculate(sigHashType, subScript, mode: SigHashType.All, anyoneCanPay: false);
            }
            else if (sigHashType == (SigHashType.All | SigHashType.AnyoneCanPay))
            {
                return Calculate(sigHashType, subScript, mode: SigHashType.All, anyoneCanPay: true);
            }
            else if (sigHashType == (SigHashType.None | SigHashType.AnyoneCanPay))
            {
                return Calculate(sigHashType, subScript, mode: SigHashType.None, anyoneCanPay: true);
            }
            else
            {
                //todo: exception type?
                throw new InvalidOperationException($"Unexpected sigHashType: '{sigHashType}'.");
            }
        }

        private byte[] Calculate(SigHashType sigHashType, byte[] subScript, SigHashType mode, bool anyoneCanPay)
        {
            MemoryStream mem = new MemoryStream();

            using (BitcoinStreamWriter writer = new BitcoinStreamWriter(mem))
            {
                writer.Write(transaction.Version);
                if (anyoneCanPay)
                {
                    writer.WriteCompact(1ul);

                    TxIn input = transaction.Inputs[InputIndex];
                    input.PreviousOutput.Write(writer);
                    writer.WriteCompact((ulong) subScript.Length);
                    writer.Write(subScript);
                    writer.Write(input.Sequence);
                }
                else
                {
                    writer.WriteCompact((ulong) transaction.Inputs.Length);
                    for (int i = 0; i < transaction.Inputs.Length; i++)
                    {
                        TxIn input = transaction.Inputs[i];
                        input.PreviousOutput.Write(writer);
                        if (InputIndex == i)
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
                }

                if (mode == SigHashType.All)
                {
                    writer.WriteArray(transaction.Outputs, (w, v) => v.Write(writer));
                }
                else if (mode == SigHashType.None)
                {
                    writer.WriteCompact(0ul);
                }
                else
                {
                    //todo: exception type?
                    throw new InvalidOperationException($"Unexpected sigHashType mode: '{mode}'.");
                }

                writer.Write(transaction.LockTime);
                writer.Write((uint) sigHashType);
            }

            return mem.ToArray();
        }
    }
}