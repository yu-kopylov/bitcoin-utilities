namespace BitcoinUtilities.P2P.Messages
{
    /// <summary>
    /// The getaddr message sends a request to a node asking for information about known active peers to help with finding potential nodes in the network.
    /// The response to receiving this message is to transmit one or more addr messages with one or more peers from a database of known active peers.
    /// <para/>
    /// The typical presumption is that a node is likely to be active if it has been sending a message within the last three hours.
    /// <para/>
    /// No additional data is transmitted with this message.
    /// </summary>
    public class GetAddrMessage : IBitcoinMessage
    {
        public const string Command = "getaddr";

        string IBitcoinMessage.Command
        {
            get { return Command; }
        }

        public void Write(BitcoinStreamWriter writer)
        {
        }

        public static GetAddrMessage Read(BitcoinStreamReader reader)
        {
            return new GetAddrMessage();
        }
    }
}