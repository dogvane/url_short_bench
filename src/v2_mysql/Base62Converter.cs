using System;
using System.Linq;
using System.Text;

namespace v2
{
    public static class Base62Converter
    {
        private const string Characters = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static readonly int Base = Characters.Length;

        public static string Encode(long number)
        {
            if (number < 0) throw new ArgumentOutOfRangeException(nameof(number), "Number must be non-negative.");
            
            // 使用更精细的时间戳+随机数+ID的组合来确保唯一性
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // 使用毫秒级时间戳
            
            // 创建一个更复杂的组合来避免重复
            // 时间戳毫秒数的低20位 + ID的低20位 + 当前线程ID的低4位
            long timePart = timestamp & 0xFFFFF; // 20位，约17分钟周期
            long idPart = number & 0xFFFFF;      // 20位，支持1M个ID
            long threadPart = System.Threading.Thread.CurrentThread.ManagedThreadId & 0xF; // 4位
            
            // 组合：20位时间 + 20位ID + 4位线程 = 44位，确保在短时间内唯一
            long combined = (timePart << 24) | (idPart << 4) | threadPart;
            
            var sb = new StringBuilder();
            while (combined > 0)
            {
                sb.Insert(0, Characters[(int)(combined % Base)]);
                combined /= Base;
            }
            
            // 确保至少6位
            var result = sb.ToString();
            if (result.Length < 6)
            {
                result = result.PadLeft(6, Characters[1]); // 用'1'填充而不是'0'
            }
            
            return result;
        }

        public static long Decode(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) throw new ArgumentNullException(nameof(str));

            long number = 0;
            foreach (char c in str)
            {
                var index = Characters.IndexOf(c);
                if (index == -1)
                    throw new ArgumentException("Invalid character in Base62 string.", nameof(str));
                number = number * Base + index;
            }
            
            // 提取原始ID（低20位，去掉4位线程ID）
            return (number >> 4) & 0xFFFFF;
        }
    }
}
