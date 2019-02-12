using System.Diagnostics;
using System.Text;
using NLog;

namespace BitcoinUtilities.Node.Modules.Blocks
{
    public partial class BlockDownloadService
    {
        private class PerformanceCounters
        {
            private readonly object monitor = new object();
            private readonly ILogger logger;

            private readonly Stopwatch runningTime = new Stopwatch();
            private long receivedBlockCount;
            private long receivedTransactionCount;
            private long receivedNewBlockCount;
            private long receivedNewTransactionCount;

            private long runningTimeSnapshot;
            private long receivedBlockCountSnapshot;
            private long receivedTransactionCountSnapshot;
            private long receivedNewBlockCountSnapshot;
            private long receivedNewTransactionCountSnapshot;

            public PerformanceCounters(ILogger logger)
            {
                this.logger = logger;
                runningTime.Start();
            }

            public void BlockReceived(bool isNewBlock, int transactionCount)
            {
                lock (monitor)
                {
                    receivedBlockCount++;
                    receivedTransactionCount += transactionCount;
                    if (isNewBlock)
                    {
                        receivedNewBlockCount++;
                        receivedNewTransactionCount += transactionCount;
                    }
                }

                LogStatistic();
            }

            private void LogStatistic()
            {
                lock (monitor)
                {
                    long runningTimeValue = runningTime.ElapsedMilliseconds;
                    long receivedBlockCountValue = receivedBlockCount;
                    long receivedTransactionCountValue = receivedTransactionCount;
                    long receivedNewBlockCountValue = receivedNewBlockCount;
                    long receivedNewTransactionCountValue = receivedNewTransactionCount;

                    if (runningTimeSnapshot + 60000 <= runningTimeValue)
                    {
                        if (logger.IsDebugEnabled)
                        {
                            StringBuilder sb = new StringBuilder();

                            sb.AppendLine($"{nameof(BlockDownloadService)} Performance");

                            sb.AppendLine(FormatTimeCounter("Running Time", runningTimeValue, runningTimeSnapshot, receivedNewBlockCountValue, receivedNewBlockCountSnapshot));
                            sb.AppendLine(FormatCounter("Received Blocks (new)", receivedNewBlockCountValue, receivedNewBlockCountSnapshot, runningTimeValue, runningTimeSnapshot));
                            sb.AppendLine(FormatCounter("Received Transactions (new)", receivedNewTransactionCountValue, receivedNewTransactionCountSnapshot, runningTimeValue, runningTimeSnapshot));
                            sb.AppendLine(FormatCounter("Received Blocks (all)", receivedBlockCountValue, receivedBlockCountSnapshot, runningTimeValue, runningTimeSnapshot));
                            sb.AppendLine(FormatCounter("Received Transactions (all)", receivedTransactionCountValue, receivedTransactionCountSnapshot, runningTimeValue, runningTimeSnapshot));
                            sb.AppendLine(FormatRatio("Received Blocks (all / new)", receivedBlockCountValue, receivedBlockCountSnapshot, receivedNewBlockCountValue, receivedNewBlockCountSnapshot));
                            sb.AppendLine(FormatRatio("Received Transactions (all / new)",
                                receivedTransactionCountValue, receivedTransactionCountSnapshot, receivedNewTransactionCountValue, receivedNewTransactionCountSnapshot
                            ));

                            logger.Debug(sb.ToString);
                        }

                        runningTimeSnapshot = runningTimeValue;
                        receivedBlockCountSnapshot = receivedBlockCountValue;
                        receivedTransactionCountSnapshot = receivedTransactionCountValue;
                        receivedNewBlockCountSnapshot = receivedNewBlockCountValue;
                        receivedNewTransactionCountSnapshot = receivedNewTransactionCountValue;
                    }
                }
            }

            private string FormatTimeCounter(string name, long totalTime, long snapshotTime, long totalOpCount, long snapshotOpCount)
            {
                long time = totalTime - snapshotTime;
                long opCount = totalOpCount - snapshotOpCount;

                var perOp = opCount == 0 ? "(0.0 ms/op)" : $"({time / (double) opCount:F1} ms/op)";
                var totalPerOp = totalOpCount == 0 ? "(0.0 ms/op)" : $"({totalTime / (double) totalOpCount:F1} ms/op)";

                return $"\t{name + ":",-34} {totalTime,8} ms {totalPerOp,15} | {time,8} ms {perOp,15}";
            }

            private string FormatCounter(string name, long totalOpCount, long snapshotOpCount, long totalTime, long snapshotTime)
            {
                long time = totalTime - snapshotTime;
                long opCount = totalOpCount - snapshotOpCount;

                var perSec = time == 0 ? "(INF op/s)" : $"({opCount * 1000d / time:F1} op/s)";
                var totalPerSec = totalTime == 0 ? "(INF op/s)" : $"({totalOpCount * 1000d / totalTime:F1} op/s)";

                return $"\t{name + ":",-34} {totalOpCount,8} op {totalPerSec,15} | {opCount,8} op {perSec,15}";
            }

            private string FormatRatio(string name, long bigValue, long bigValueSnapshot, long smallValue, long smallValueSnapshot)
            {
                long latestBigValue = bigValue - bigValueSnapshot;
                long latestSmallValue = smallValue - smallValueSnapshot;

                var totalRatio = smallValue == 0 ? "INF" : $"{bigValue / (double) smallValue:F3}";
                var totalDelta = bigValue > smallValue ? $"(+{bigValue - smallValue})" : $"({bigValue - smallValue})";

                var ratio = latestSmallValue == 0 ? "INF" : $"{latestBigValue / (double) latestSmallValue:F3}";
                var delta = latestBigValue > latestSmallValue ? $"(+{latestBigValue - latestSmallValue})" : $"({latestBigValue - latestSmallValue})";

                return $"\t{name + ":",-34} {totalRatio,11} {totalDelta,15} | {ratio,11} {delta,15}";
            }
        }
    }
}