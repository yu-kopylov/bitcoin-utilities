using System.Diagnostics;
using System.Text;
using NLog;

namespace BitcoinUtilities.Node.Services.Outputs
{
    public partial class UtxoUpdateService
    {
        private class PerformanceCounters
        {
            private readonly ILogger logger;

            private readonly Stopwatch runningTime = new Stopwatch();
            private readonly Stopwatch blockFetchingTime = new Stopwatch();
            private readonly Stopwatch blockProcessingTime = new Stopwatch();
            private long blockRequestCount;
            private long blockResponsesCount;
            private long processedBlocksCount;
            private long processedTxCount;

            private long runningTimeSnapshot;
            private long blockFetchingTimeSnapshot;
            private long blockProcessingTimeSnapshot;
            private long blockRequestCountSnapshot;
            private long blockResponsesCountSnapshot;
            private long processedBlocksCountSnapshot;
            private long processedTxCountSnapshot;

            public PerformanceCounters(ILogger logger)
            {
                this.logger = logger;
            }

            public void StartRunning()
            {
                runningTime.Start();
            }

            public void BlockRequestSent()
            {
                blockFetchingTime.Start();
                blockRequestCount++;
            }

            public void BlockReceived()
            {
                blockFetchingTime.Stop();
                blockProcessingTime.Start();
                blockResponsesCount++;
            }

            public void BlockProcessed(int txCount)
            {
                blockProcessingTime.Stop();
                processedBlocksCount++;
                processedTxCount += txCount;
                LogStatistic();
            }

            public void BlockDiscarded()
            {
                blockProcessingTime.Stop();
                LogStatistic();
            }

            private void LogStatistic()
            {
                long runningTimeValue = runningTime.ElapsedMilliseconds;
                long blockFetchingTimeValue = blockFetchingTime.ElapsedMilliseconds;
                long blockProcessingTimeValue = blockProcessingTime.ElapsedMilliseconds;

                if (runningTimeSnapshot + 60000 <= runningTimeValue || blockResponsesCountSnapshot + 1000 <= blockResponsesCount)
                {
                    if (logger.IsDebugEnabled)
                    {
                        StringBuilder sb = new StringBuilder();

                        sb.AppendLine($"{nameof(UtxoUpdateService)} Performance");

                        sb.AppendLine(FormatTimeCounter("Running Time", runningTimeValue, runningTimeSnapshot, blockResponsesCount, blockResponsesCountSnapshot));
                        sb.AppendLine(FormatTimeCounter("Block Fetching Time", blockFetchingTimeValue, blockFetchingTimeSnapshot, blockResponsesCount, blockResponsesCountSnapshot));
                        sb.AppendLine(FormatTimeCounter("Block Processing Time", blockProcessingTimeValue, blockProcessingTimeSnapshot, blockResponsesCount, blockResponsesCountSnapshot));
                        sb.AppendLine(FormatCounter("Block Request Count", blockRequestCount, blockRequestCountSnapshot, runningTimeValue, runningTimeSnapshot));
                        sb.AppendLine(FormatCounter("Block Response Count", blockResponsesCount, blockResponsesCountSnapshot, runningTimeValue, runningTimeSnapshot));
                        sb.AppendLine(FormatCounter("Processed Blocks", processedBlocksCount, processedBlocksCountSnapshot, runningTimeValue, runningTimeSnapshot));
                        sb.AppendLine(FormatCounter("Processed Transactions", processedTxCount, processedTxCountSnapshot, runningTimeValue, runningTimeSnapshot));

                        logger.Debug(sb.ToString);
                    }

                    runningTimeSnapshot = runningTimeValue;
                    blockFetchingTimeSnapshot = blockFetchingTimeValue;
                    blockProcessingTimeSnapshot = blockProcessingTimeValue;
                    blockRequestCountSnapshot = blockRequestCount;
                    blockResponsesCountSnapshot = blockResponsesCount;
                    processedBlocksCountSnapshot = processedBlocksCount;
                    processedTxCountSnapshot = processedTxCount;
                }
            }

            private string FormatTimeCounter(string name, long totalTime, long snapshotTime, long totalOpCount, long snapshotOpCount)
            {
                long time = totalTime - snapshotTime;
                long opCount = totalOpCount - snapshotOpCount;

                var perOp = opCount == 0 ? "(0.0 ms/op)" : $"({time / (double) opCount:F1} ms/op)";
                var totalPerOp = totalOpCount == 0 ? "(0.0 ms/op)" : $"({totalTime / (double) totalOpCount:F1} ms/op)";

                return $"\t{name + ":",-24} {totalTime,8} ms {totalPerOp,14} | {time,8} ms {perOp,14}";
            }

            private string FormatCounter(string name, long totalOpCount, long snapshotOpCount, long totalTime, long snapshotTime)
            {
                long time = totalTime - snapshotTime;
                long opCount = totalOpCount - snapshotOpCount;

                var perSec = time == 0 ? "(INF op/s)" : $"({opCount * 1000d / time:F1} op/s)";
                var totalPerSec = totalTime == 0 ? "(INF op/s)" : $"({totalOpCount * 1000d / totalTime:F1} op/s)";

                return $"\t{name + ":",-24} {totalOpCount,8} op {totalPerSec,14} | {opCount,8} op {perSec,14}";
            }
        }
    }
}