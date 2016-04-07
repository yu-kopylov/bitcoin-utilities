namespace BitcoinUtilities.P2P.Messages
{
    /// <summary>
    /// The ping message is sent primarily to confirm that the TCP/IP connection is still valid.
    /// <para/>
    /// An error in transmission is presumed to be a closed connection and the address is removed as a current peer.
    /// </summary>
    public class PingMessage : IBitcoinMessage
    {
        public const string Command = "ping";

        private readonly ulong nonce;

        public PingMessage(ulong nonce)
        {
            this.nonce = nonce;
        }

        /// <summary>
        /// random nonce
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

        public static PingMessage Read(BitcoinStreamReader reader)
        {
            ulong nonce = reader.ReadUInt64();
            return new PingMessage(nonce);
        }
    }
}