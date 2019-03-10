namespace BitcoinUtilities.P2P.Messages
{
    /// <summary>
    /// The sendheaders message tells the receiving peer to send new block announcements using a headers message rather than an inv message.
    /// </summary>
    public class SendHeadersMessage : IBitcoinMessage
    {
        public const string Command = "sendheaders";

        string IBitcoinMessage.Command
        {
            get { return Command; }
        }

        public void Write(BitcoinStreamWriter writer)
        {
        }

        public static SendHeadersMessage Read(BitcoinStreamReader reader)
        {
            return new SendHeadersMessage();
        }
    }
}