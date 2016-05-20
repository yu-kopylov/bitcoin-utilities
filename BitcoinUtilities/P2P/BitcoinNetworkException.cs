using System;

namespace BitcoinUtilities.P2P
{
    /// <summary>
    /// The exception that is thrown when there is a problem with network connection.
    /// </summary>
    public class BitcoinNetworkException : Exception
    {
        public BitcoinNetworkException(string message) : base(message)
        {
        }

        public BitcoinNetworkException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}