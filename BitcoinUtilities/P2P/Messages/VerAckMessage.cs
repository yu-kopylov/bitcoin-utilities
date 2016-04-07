namespace BitcoinUtilities.P2P.Messages
{
    /// <summary>
    /// The <see cref="VerAckMessage"/> message is sent in reply to <see cref="VersionMessage"/>.
    /// </summary>
    public class VerAckMessage : IBitcoinMessage
    {
        public const string Command = "verack";

        string IBitcoinMessage.Command
        {
            get { return Command; }
        }

        public void Write(BitcoinStreamWriter writer)
        {
        }

        public static VerAckMessage Read(BitcoinStreamReader reader)
        {
            return new VerAckMessage();
        }
    }
}