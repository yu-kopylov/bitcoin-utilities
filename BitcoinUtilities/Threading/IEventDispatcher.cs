namespace BitcoinUtilities.Threading
{
    public interface IEventDispatcher
    {
        void Raise(object evt);
    }
}