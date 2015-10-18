using System;

namespace BitcoinUtilities.P2P.Primitives
{
    /// <summary>
    /// A bitcoin transaction output.
    /// </summary>
    public struct TxOut
    {
        private readonly ulong value;
        private readonly byte[] pubkeyScript;

        public TxOut(ulong value, byte[] pubkeyScript)
        {
            this.value = value;
            this.pubkeyScript = pubkeyScript;
        }

        /// <summary>
        /// Number of satoshis to spend. May be zero.
        /// <para/>
        /// The sum of all outputs may not exceed the sum of satoshis previously spent to the outpoints provided in the input section. 
        /// </summary>
        public ulong Value
        {
            get { return value; }
        }

        /// <summary>
        /// Pubkey script.
        /// <para/> 
        /// Maximum is 10,000 bytes.
        /// </summary>
        public byte[] PubkeyScript
        {
            get { return pubkeyScript; }
        }

        public void Write(BitcoinStreamWriter writer)
        {
            writer.Write(value);
            writer.WriteCompact((ulong) pubkeyScript.Length);
            writer.Write(pubkeyScript);
        }

        public static TxOut Read(BitcoinStreamReader reader)
        {
            ulong value = reader.ReadUInt64();
            ulong pubkeyScriptLength = reader.ReadUInt64Compact();
            if (pubkeyScriptLength > 10000)
            {
                //todo: handle correctly
                throw new Exception("Pubkey script is too long.");
            }
            byte[] pubkeyScript = reader.ReadBytes((int) pubkeyScriptLength);
            return new TxOut(value, pubkeyScript);
        }
    }
}