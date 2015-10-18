using System;

namespace BitcoinUtilities.P2P.Primitives
{
    /// <summary>
    /// A bitcoin transaction input.
    /// </summary>
    public struct TxIn
    {
        private readonly TxOutPoint previousOutput;
        private readonly byte[] signatureScript;
        private readonly uint sequence;

        public TxIn(TxOutPoint previousOutput, byte[] signatureScript, uint sequence)
        {
            this.previousOutput = previousOutput;
            this.signatureScript = signatureScript;
            this.sequence = sequence;
        }

        /// <summary>
        /// The previous output transaction reference, as an <see cref="TxOutPoint"/> structure.
        /// </summary>
        public TxOutPoint PreviousOutput
        {
            get { return previousOutput; }
        }

        /// <summary>
        /// Computational Script for confirming transaction authorization.
        /// </summary>
        public byte[] SignatureScript
        {
            get { return signatureScript; }
        }

        /// <summary>
        /// Transaction version as defined by the sender. Intended for "replacement" of transactions when information is updated before inclusion into a block.
        /// </summary>
        public uint Sequence
        {
            get { return sequence; }
        }

        public void Write(BitcoinStreamWriter writer)
        {
            previousOutput.Write(writer);
            writer.WriteCompact((ulong) signatureScript.Length);
            writer.Write(signatureScript);
            writer.Write(sequence);
        }

        public static TxIn Read(BitcoinStreamReader reader)
        {
            TxOutPoint previousOutput = TxOutPoint.Read(reader);
            ulong signatureScriptLength = reader.ReadUInt64Compact();
            if (signatureScriptLength > 1024*1024) //todo: see if there is actual limitation for this field 
            {
                //todo: handle correctly
                throw new Exception("Too many transactions.");
            }
            byte[] signatureScript = reader.ReadBytes((int) signatureScriptLength);
            uint sequence = reader.ReadUInt32();

            return new TxIn(previousOutput, signatureScript, sequence);
        }
    }
}