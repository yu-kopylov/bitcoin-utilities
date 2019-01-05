namespace BitcoinUtilities.Node.Rules
{
    public interface ISubchain<out T>
    {
        int Count { get; }
        T GetBlockByHeight(int height);
        T GetBlockByOffset(int offset);
    }
}