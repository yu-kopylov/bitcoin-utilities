using System;
using System.IO;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Scripts
{
    /// <summary>
    /// Specifivation: https://github.com/Bitcoin-UAHF/spec/blob/master/replay-protected-sighash.md
    /// </summary>
    public class BitcoinCashSigHashCalculator : ISigHashCalculator
    {
        private readonly Tx transaction;
        private int inputIndex;
        private ulong value;

        private byte[] cachedPrevoutHash;
        private byte[] cachedSequenceHash;
        private byte[] cachedOutputsHash;

        public BitcoinCashSigHashCalculator(Tx transaction)
        {
            this.transaction = transaction;
        }

        public int InputIndex
        {
            get { return inputIndex; }
            set { inputIndex = value; }
        }

        public ulong Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public byte[] Calculate(SigHashType sigHashType, byte[] subScript)
        {
            if (!sigHashType.HasFlag(SigHashType.ForkId))
            {
                //todo: exception type?
                throw new InvalidOperationException("Bitcoin Cash transactions should have SIGHASH_FORKID flag set in hash type.");
            }

            sigHashType = sigHashType & ~SigHashType.ForkId;

            MemoryStream mem = new MemoryStream();
            using (BitcoinStreamWriter writer = new BitcoinStreamWriter(mem))
            {
                TxIn input = transaction.Inputs[inputIndex];

                byte[] prevoutHash;
                byte[] sequenceHash;
                byte[] outputsHash;

                if (sigHashType == SigHashType.All)
                {
                    prevoutHash = GetPrevoutHash();
                    sequenceHash = GetSequenceHash();
                    outputsHash = GetOutputsHash();
                }
                else if (sigHashType == SigHashType.None)
                {
                    // todo: test this branch
                    prevoutHash = GetPrevoutHash();
                    sequenceHash = new byte[32];
                    outputsHash = new byte[32];
                }
                else if (sigHashType == SigHashType.Single)
                {
                    // todo: test this branch
                    prevoutHash = GetPrevoutHash();
                    sequenceHash = new byte[32];
                    outputsHash = InputIndex < transaction.Outputs.Length ? GetSingleOutputHash() : new byte[32];
                }
                else if (sigHashType == SigHashType.AnyoneCanPay)
                {
                    // todo: test this branch
                    prevoutHash = new byte[32];
                    sequenceHash = new byte[32];
                    outputsHash = GetOutputsHash();
                }
                else
                {
                    //todo: exception type?
                    throw new InvalidOperationException($"Unexpected sigHashType: '{sigHashType}'.");
                }

                //todo: check if transaction exists
                writer.Write(transaction.Version);
                writer.Write(prevoutHash);
                writer.Write(sequenceHash);
                input.PreviousOutput.Write(writer);
                writer.WriteCompact((ulong) subScript.Length);
                writer.Write(subScript);
                writer.Write(value);
                writer.Write(input.Sequence);
                writer.Write(outputsHash);
                writer.Write(transaction.LockTime);
                //todo: where to apply SigHashType.ForkId (here or in consumer class)
                writer.Write((uint) (sigHashType | SigHashType.ForkId));
            }

            return mem.ToArray();
        }

        private byte[] GetPrevoutHash()
        {
            if (cachedPrevoutHash != null)
            {
                return cachedPrevoutHash;
            }
            MemoryStream mem = new MemoryStream();
            using (BitcoinStreamWriter writer = new BitcoinStreamWriter(mem))
            {
                foreach (TxIn input in transaction.Inputs)
                {
                    input.PreviousOutput.Write(writer);
                }
            }
            cachedPrevoutHash = CryptoUtils.DoubleSha256(mem.ToArray());
            return cachedPrevoutHash;
        }

        private byte[] GetSequenceHash()
        {
            if (cachedSequenceHash != null)
            {
                return cachedSequenceHash;
            }
            MemoryStream mem = new MemoryStream();
            using (BitcoinStreamWriter writer = new BitcoinStreamWriter(mem))
            {
                foreach (TxIn input in transaction.Inputs)
                {
                    writer.Write(input.Sequence);
                }
            }
            cachedSequenceHash = CryptoUtils.DoubleSha256(mem.ToArray());
            return cachedSequenceHash;
        }

        private byte[] GetOutputsHash()
        {
            if (cachedOutputsHash != null)
            {
                return cachedOutputsHash;
            }
            MemoryStream mem = new MemoryStream();
            using (BitcoinStreamWriter writer = new BitcoinStreamWriter(mem))
            {
                foreach (TxOut output in transaction.Outputs)
                {
                    output.Write(writer);
                }
            }
            cachedOutputsHash = CryptoUtils.DoubleSha256(mem.ToArray());
            return cachedOutputsHash;
        }

        private byte[] GetSingleOutputHash()
        {
            MemoryStream mem = new MemoryStream();
            using (BitcoinStreamWriter writer = new BitcoinStreamWriter(mem))
            {
                transaction.Outputs[InputIndex].Write(writer);
            }
            return CryptoUtils.DoubleSha256(mem.ToArray());
        }
    }
}