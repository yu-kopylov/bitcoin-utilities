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
            bool anyoneCanPay = sigHashType.HasFlag(SigHashType.AnyoneCanPay);
            SigHashType mode = sigHashType & ~SigHashType.AnyoneCanPay;

            return Calculate(sigHashType, subScript, mode, anyoneCanPay);
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

                        if (InputIndex != i && (mode == SigHashType.None || mode == SigHashType.Single))
                        {
                            writer.Write(0u);
                        }
                        else
                        {
                            writer.Write(input.Sequence);
                        }
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
                else if (mode == SigHashType.Single)
                {
                    if (InputIndex >= transaction.Outputs.Length)
                    {
                        // Note: The transaction that uses SIGHASH_SINGLE type of signature should not have more inputs than outputs.
                        // However if it does (because of the pre-existing implementation), it shall not be rejected,
                        // but instead for every "illegal" input (meaning: an input that has an index bigger than the maximum output index) the node should still verify it,
                        // though assuming the hash of 0000000000000000000000000000000000000000000000000000000000000001
                        //todo: exception type?
                        throw new InvalidOperationException("A transaction that uses SIGHASH_SINGLE type of signature should not have more inputs than outputs.");
                    }

                    writer.WriteCompact((ulong) InputIndex + 1);
                    for (int i = 0; i < InputIndex; i++)
                    {
                        writer.Write(-1L);
                        writer.WriteCompact(0UL);
                    }

                    transaction.Outputs[InputIndex].Write(writer);
                }
                else
                {
                    //todo: exception type?
                    throw new InvalidOperationException($"Unexpected signature hash type: '{mode}'.");
                }

                writer.Write(transaction.LockTime);
                writer.Write((uint) sigHashType);
            }

            return mem.ToArray();
        }
    }
}