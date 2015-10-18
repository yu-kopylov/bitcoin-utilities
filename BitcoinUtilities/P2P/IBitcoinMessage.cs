namespace BitcoinUtilities.P2P
{
    //todo: interface almost clashes with BitcoinMessage class (rename BitcoinMessage to BitcoinMessageRaw ?)
    public interface IBitcoinMessage
    {
        string Command { get; } //todo: Command property clashes with Command const in imlementations
        void Write(BitcoinStreamWriter writer);
    }
}