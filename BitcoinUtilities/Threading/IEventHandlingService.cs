using System;

namespace BitcoinUtilities.Threading
{
    public interface IEventHandlingService
    {
        Action<object> GetHandler(object evt);
    }
}