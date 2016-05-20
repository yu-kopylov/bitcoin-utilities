using System;

namespace BitcoinUtilities.Node
{
    /// <summary>
    /// The exception that is thrown when a remote endpoint violates bitcoin protocol rules.
    /// </summary>
    // todo: describe how BitcoinNode treats this exception
    public class BitcoinProtocolViolationException : Exception
    {
        public BitcoinProtocolViolationException(string message) : base(message)
        {
        }

        public BitcoinProtocolViolationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}