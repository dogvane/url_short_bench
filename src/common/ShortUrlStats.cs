using System;
using System.Threading;

namespace url_short.common
{
    public class ShortUrlStats
    {
        private long totalCreateCount = 0;
        private long totalGetCount = 0;
        private long intervalCreateCount = 0;
        private long intervalGetCount = 0;
        private readonly Timer statTimer;
        private readonly Func<int> getDictCount;
        private readonly DateTime startTime = DateTime.UtcNow;
        private DateTime lastPrintTime = DateTime.UtcNow;

        public DateTime StartTime => startTime;

        public ShortUrlStats(Func<int> getDictCount)
        {
            this.getDictCount = getDictCount;
            statTimer = new Timer(_ => PrintStats(), null, 10000, 10000);
        }

        public void IncCreate()
        {
            Interlocked.Increment(ref totalCreateCount);
            Interlocked.Increment(ref intervalCreateCount);
        }

        public void IncGet()
        {
            Interlocked.Increment(ref totalGetCount);
            Interlocked.Increment(ref intervalGetCount);
        }
        private void PrintStats()
        {
            DateTime now = DateTime.UtcNow;
            
            // 原子性地获取并重置区间计数
            long create = Interlocked.Exchange(ref intervalCreateCount, 0);
            long get = Interlocked.Exchange(ref intervalGetCount, 0);
            
            // 获取总计数的快照
            long totalCreate = Interlocked.Read(ref totalCreateCount);
            long totalGet = Interlocked.Read(ref totalGetCount);
            
            int dictCount = getDictCount();
            double intervalSeconds = (now - lastPrintTime).TotalSeconds;
            lastPrintTime = now;
            
            double createQps = intervalSeconds > 0 ? create / intervalSeconds : 0;
            double getQps = intervalSeconds > 0 ? get / intervalSeconds : 0;
            double totalQps = (totalCreate + totalGet) / (now - startTime).TotalSeconds;
            
            Console.WriteLine($"[{now:HH:mm:ss}] 累计 创建:{totalCreate} 访问:{totalGet} | 当前 短链:{dictCount} 内存:{GC.GetTotalMemory(false) / 1024 / 1024:F1}MB | 区间({intervalSeconds:F1}s) 创建:{create}({createQps:F1}QPS) 访问:{get}({getQps:F1}QPS) | 总体QPS:{totalQps:F1}");
        }
    }
}
