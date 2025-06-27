using System;
using System.Threading;

namespace v1
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

                if (create == 0 && get == 0)
                {
                    return;
                }

                totalCreate = totalCreateCount;
                totalGet = totalGetCount;
                intervalSeconds = (now - lastPrintTime).TotalSeconds;
                lastPrintTime = now;
                intervalCreateCount = 0;
                intervalGetCount = 0;
                dictCount = getDictCount();
            }
            lastIntervalSeconds = intervalSeconds;
            double qps = intervalSeconds > 0 ? (create + get) / intervalSeconds : 0;
            double totalSeconds = (now - startTime).TotalSeconds;
            double totalQps = totalSeconds > 0 ? (totalCreate + totalGet) / totalSeconds : 0;
            Console.WriteLine($"[统计] {now:HH:mm:ss} 间隔创建:{create} 获取:{get}，累计创建:{totalCreate} 获取:{totalGet}，当前短链数:{dictCount}，间隔QPS:{qps:F2} 总QPS:{totalQps:F2}，已运行:{totalSeconds:F0}s");
        }
    }
}
