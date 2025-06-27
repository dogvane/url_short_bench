using System;
using System.Threading;

namespace v2
{
    public class ShortUrlStats
    {
        private int totalCreateCount = 0;
        private int totalGetCount = 0;
        private int intervalCreateCount = 0;
        private int intervalGetCount = 0;
        private readonly object statLock = new();
        private readonly Timer statTimer;
        private readonly Func<int> getDictCount;
        private readonly DateTime startTime = DateTime.UtcNow;
        private double lastIntervalSeconds = 10;
        private DateTime lastPrintTime = DateTime.UtcNow;

        public ShortUrlStats(Func<int> getDictCount)
        {
            this.getDictCount = getDictCount;
            statTimer = new Timer(_ => PrintStats(), null, 10000, 10000);
        }

        public void IncCreate()
        {
            lock (statLock)
            {
                totalCreateCount++;
                intervalCreateCount++;
            }
        }
        public void IncGet()
        {
            lock (statLock)
            {
                totalGetCount++;
                intervalGetCount++;
            }
        }
        private void PrintStats()
        {
            int create, get, totalCreate, totalGet, dictCount;
            DateTime now = DateTime.UtcNow;
            double intervalSeconds;
            lock (statLock)
            {
                create = intervalCreateCount;
                get = intervalGetCount;
                totalCreate = totalCreateCount;
                totalGet = totalGetCount;
                dictCount = getDictCount();
                intervalCreateCount = 0;
                intervalGetCount = 0;
                intervalSeconds = (now - lastPrintTime).TotalSeconds;
                lastPrintTime = now;
            }
            lastIntervalSeconds = intervalSeconds;
            Console.WriteLine($"[{now:HH:mm:ss}] 总创建: {totalCreate}, 总访问: {totalGet}, 当前短链数: {dictCount}, 10s内创建: {create}, 10s内访问: {get}");
        }
    }
}
