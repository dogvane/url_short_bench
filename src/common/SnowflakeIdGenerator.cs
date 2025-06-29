using System;

namespace common
{
    public class SnowflakeIdGenerator
    {
        private static readonly object SyncRoot = new object();
        private const long Twepoch = 1288834974657L;
        private const int WorkerIdBits = 5;
        private const int DatacenterIdBits = 5;
        private const int SequenceBits = 12;

        private const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);
        private const long MaxDatacenterId = -1L ^ (-1L << DatacenterIdBits);

        private const int WorkerIdShift = SequenceBits;
        private const int DatacenterIdShift = SequenceBits + WorkerIdBits;
        private const int TimestampLeftShift = SequenceBits + WorkerIdBits + DatacenterIdBits;
        private const long SequenceMask = -1L ^ (-1L << SequenceBits);

        private long _lastTimestamp = -1L;
        private long _sequence = 0L;

        public long WorkerId { get; private set; }
        public long DatacenterId { get; private set; }

        public SnowflakeIdGenerator(long workerId, long datacenterId)
        {
            if (workerId > MaxWorkerId || workerId < 0)
                throw new ArgumentException($"workerId must be between 0 and {MaxWorkerId}");
            if (datacenterId > MaxDatacenterId || datacenterId < 0)
                throw new ArgumentException($"datacenterId must be between 0 and {MaxDatacenterId}");
            WorkerId = workerId;
            DatacenterId = datacenterId;
        }

        public long NextId()
        {
            lock (SyncRoot)
            {
                var timestamp = TimeGen();
                if (timestamp < _lastTimestamp)
                {
                    throw new InvalidOperationException($"Clock moved backwards. Refusing to generate id for {_lastTimestamp - timestamp} milliseconds");
                }
                if (_lastTimestamp == timestamp)
                {
                    _sequence = (_sequence + 1) & SequenceMask;
                    if (_sequence == 0)
                    {
                        timestamp = TilNextMillis(_lastTimestamp);
                    }
                }
                else
                {
                    _sequence = 0L;
                }
                _lastTimestamp = timestamp;
                return ((timestamp - Twepoch) << TimestampLeftShift) |
                       (DatacenterId << DatacenterIdShift) |
                       (WorkerId << WorkerIdShift) |
                       _sequence;
            }
        }

        private long TilNextMillis(long lastTimestamp)
        {
            var timestamp = TimeGen();
            while (timestamp <= lastTimestamp)
            {
                timestamp = TimeGen();
            }
            return timestamp;
        }

        private long TimeGen()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
