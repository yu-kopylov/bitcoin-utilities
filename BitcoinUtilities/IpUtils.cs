using System;
using System.Net;
using System.Net.Sockets;

namespace BitcoinUtilities
{
    public static class IpUtils
    {
        /// <summary>
        /// Maps the IPAddress object to an IPv6 address.
        /// </summary>
        /// <remark>
        /// Mono 4.2.3 does not have IPAddress.MapToIPv6 method.
        /// It was introduced in Mono in the patch: https://github.com/mono/mono/commit/bbd8a219345e0c2d629d39dba030975d0a639ff1
        /// </remark>
        public static IPAddress MapToIPv6(IPAddress address)
        {
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
                return address;
            if (address.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException("Only IPv4 addresses can be converted to IPv6.", nameof(address));

            byte[] ipv4Bytes = address.GetAddressBytes();
            byte[] ipv6Bytes = new byte[16]
            {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xFF, 0xFF,
                ipv4Bytes[0], ipv4Bytes[1], ipv4Bytes[2], ipv4Bytes[3]
            };
            return new IPAddress(ipv6Bytes);
        }
    }
}