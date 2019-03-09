namespace BitcoinUtilities.Threading
{
    public interface IEventDispatcher
    {
        void Raise<T>(T evt) where T : IEvent;
    }
}