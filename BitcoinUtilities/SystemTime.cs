﻿using System;

namespace BitcoinUtilities
{
    /// <summary>
    /// A thread-safe wrapper over the <see cref="DateTime.UtcNow"/> property, that allows testing with arbitrary date settings.
    /// </summary>
    public static class SystemTime
    {
        private static readonly object lockObject = new object();

        private static DateTime? overridedDateTime = null;

        /// <summary>
        /// Sets the date returned by the UtcNow property to the given date.
        /// </summary>
        /// <remarks>
        /// Should be used only in tests.
        /// </remarks>
        /// <param name="dateTime">The date which sholud be returned by the UtcNow property.</param>
        internal static void Override(DateTime dateTime)
        {
            lock (lockObject)
            {
                overridedDateTime = dateTime;
            }
        }

        /// <summary>
        /// Restores the default behaviour of the UtcNow property.
        /// </summary>
        internal static void Reset()
        {
            lock (lockObject)
            {
                overridedDateTime = null;
            }
        }

        /// <summary>
        /// Unless overriden by the method <see cref="Override"/>, returns the value returned by the <see cref="DateTime.UtcNow"/> property.
        /// </summary>
        public static DateTime UtcNow
        {
            get
            {
                lock (lockObject)
                {
                    return overridedDateTime ?? DateTime.UtcNow;
                }
            }
        }
    }
}