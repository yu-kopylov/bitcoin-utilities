using System;

namespace BitcoinUtilities.Threading
{
    public interface IEventHandlingService
    {
        /// <summary>
        /// Executed when service is started, before execution of any events.
        /// </summary>
        void OnStart();

        /// <returns>Handler for the given event if it exists; otherwise, null.</returns>
        Action<object> GetHandler(object evt);

        /// <summary>
        /// Executed when service is stopped. No events will be passed to this service after call to this method.
        /// </summary>
        void OnTearDown();
    }
}