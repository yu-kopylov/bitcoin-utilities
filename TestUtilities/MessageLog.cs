using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace TestUtilities
{
    public class MessageLog
    {
        private readonly object logLock = new StringBuilder();
        private readonly List<string> log = new List<string>();
        private readonly Stopwatch timer = Stopwatch.StartNew();

        public MessageLog()
        {
            Console.WriteLine($"{timer.ElapsedMilliseconds,4}: >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
        }

        public string[] GetLog()
        {
            lock (logLock)
            {
                return log.ToArray();
            }
        }

        public void Log(string message)
        {
            lock (logLock)
            {
                log.Add(message);
                Console.WriteLine($"{timer.ElapsedMilliseconds,4}: {message}");
            }
        }

        public void Clear()
        {
            lock (logLock)
            {
                log.Clear();
                Console.WriteLine($"{timer.ElapsedMilliseconds,4}: =========================================");
            }
        }
    }
}