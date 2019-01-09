using System;

namespace BitcoinUtilities.P2P
{
    /// <summary>
    /// The exception that is thrown when there is a problem with network connection
    /// (for example, when socket is closed or when remote host sends a malformed message).
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