using System;

namespace BitcoinUtilities.P2P
{
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