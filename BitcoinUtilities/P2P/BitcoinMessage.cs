namespace BitcoinUtilities.P2P
{
    public class BitcoinMessage
    {
        public BitcoinMessage(string command, byte[] payload)
        {
            Command = command;
            Payload = payload;
        }

        public string Command { get; private set; }
        public byte[] Payload { get; private set; }
    }
}