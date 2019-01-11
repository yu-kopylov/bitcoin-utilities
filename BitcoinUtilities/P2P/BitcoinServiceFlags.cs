using System;

namespace BitcoinUtilities.P2P
{
    [Flags]
    public enum BitcoinServiceFlags : ulong
    {
        /// <summary>
        /// This node can be asked for full blocks instead of just headers.
        /// </summary>
        NodeNetwork = 1ul
    }
}