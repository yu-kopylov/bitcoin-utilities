namespace BitcoinUtilities.P2P.Primitives
{
    /// <summary>
    /// A bitcoin transaction.
    /// </summary>
    public class Tx
    {
        private readonly uint version;
        private readonly TxIn[] inputs;
        private readonly TxOut[] outputs;
        private readonly uint lockTime;

        private byte[] hash;

        public Tx(uint version, TxIn[] inputs, TxOut[] outputs, uint lockTime)
        {
            this.version = version;
            this.inputs = inputs;
            this.outputs = outputs;
            this.lockTime = lockTime;
        }

        /// <summary>
        /// <para>Transaction data format version.</para>
        /// <para>Currently 1.</para>
        /// </summary>
        public uint Version
        {
            get { return version; }
        }

        /// <summary>
        /// A list of 1 or more transaction inputs or sources for coins.
        /// </summary>
        public TxIn[] Inputs
        {
            get { return inputs; }
        }

        /// <summary>
        /// A list of 1 or more transaction outputs or destinations for coins.
        /// </summary>
        public TxOut[] Outputs
        {
            get { return outputs; }
        }

        /// <summary>
        /// If all <see cref="Inputs"/> have final (0xffffffff) sequence numbers then <see cref="LockTime"/> is irrelevant.
        /// Otherwise, the transaction may not be added to a block until after <see cref="LockTime"/>.
        /// </summary>
        public uint LockTime
        {
            get { return lockTime; }
        }

        /// <summary>
        /// Hash of this transaction.
        /// </summary>
        public byte[] Hash
        {
            get
            {
                if (hash == null)
                {
                    hash = CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(Write));
                }

                return hash;
            }
        }

        public void Write(BitcoinStreamWriter writer)
        {
            writer.Write(version);
            writer.WriteArray(Inputs, (w, v) => v.Write(writer));
            writer.WriteArray(Outputs, (w, v) => v.Write(writer));
            writer.Write(lockTime);
        }

        public static Tx Read(BitcoinStreamReader reader)
        {
            uint version = reader.ReadUInt32();
            TxIn[] inputs = reader.ReadArray(1024 * 1024, TxIn.Read);
            TxOut[] outputs = reader.ReadArray(1024 * 1024, TxOut.Read);
            uint lockTime = reader.ReadUInt32();
            return new Tx(version, inputs, outputs, lockTime);
        }
    }
}