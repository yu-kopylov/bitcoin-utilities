using System;

namespace BitcoinUtilities
{
    public static class UnixTime
    {
        private static readonly DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Converts a given UNIX timestamp to a UTC-kind date.
        /// </summary>
        public static DateTime ToDateTime(long timestamp)
        {
            return unixEpoch.AddSeconds(timestamp);
        }

        /// <summary>
        /// Converts a given date to a UNIX timestamp.
        /// </summary>
        public static long ToTimestamp(DateTime date)
        {
            TimeSpan timeSpan = date - unixEpoch;
            return (long) timeSpan.TotalSeconds;
        }
    }
}