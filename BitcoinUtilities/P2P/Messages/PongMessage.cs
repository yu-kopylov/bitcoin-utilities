namespace BitcoinUtilities.P2P.Messages
{
    /// <summary>
    /// The pong message is sent in response to a ping message.
    /// <para/>
    /// In modern protocol versions, a pong response is generated using a nonce included in the ping.
    /// </summary>
    public class PongMessage : IBitcoinMessage
    {
        public const string Command = "pong";

        private readonly ulong nonce;

        public PongMessage(ulong nonce)
        {
            this.nonce = nonce;
        }

        /// <summary>
        /// nonce from ping
        /// </summary>
        public ulong Nonce
        {
            get { return nonce; }
        }

        string IBitcoinMessage.Command
        {
            get { return Command; }
        }

        public void Write(BitcoinStreamWriter writer)
        {
            writer.Write(nonce);
        }

        public static PongMessage Read(BitcoinStreamReader reader)
        {
            ulong nonce = reader.ReadUInt64();
            return new PongMessage(nonce);
        }
    }
}