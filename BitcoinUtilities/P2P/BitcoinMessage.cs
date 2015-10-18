namespace BitcoinUtilities.P2P
{
    public class BitcoinMessage : IBitcoinMessage
    {
        public BitcoinMessage(string command, byte[] payload)
        {
            Command = command;
            Payload = payload;
        }

        public string Command { get; private set; }
        public byte[] Payload { get; private set; }

        public void Write(BitcoinStreamWriter writer)
        {
            writer.Write(Payload);
        }
    }
}