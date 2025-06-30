using System;
using System.Linq;
using System.Text;

namespace url_short.common
{
    /// <summary>
    /// Base62编码转换器，用于URL短链的编码和解码
    /// </summary>
    public class Base62Converter : IShortCodeGen
    {
        private static readonly char[] Base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();
        private static readonly int Base = Base62Chars.Length;
        private readonly int _fixedLength;

        /// <summary>
        /// 初始化 Base62Converter
        /// </summary>
        /// <param name="fixedLength">固定编码长度，如果设置为0则不使用固定长度</param>
        public Base62Converter(int fixedLength = 0)
        {
            if (fixedLength < 0)
                throw new ArgumentOutOfRangeException(nameof(fixedLength), "Fixed length must be non-negative.");
            _fixedLength = fixedLength;
        }

        /// <summary>
        /// 将数字编码为Base62字符串
        /// </summary>
        /// <param name="number">要编码的数字</param>
        /// <returns>编码后的Base62字符串</returns>
        public string Encode(long number)
        {
            if (number < 0) throw new ArgumentOutOfRangeException(nameof(number), "Number must be non-negative.");
            
            // 检查数字是否超出固定长度的最大值
            if (_fixedLength > 0)
            {
                long maxValue = (long)Math.Pow(Base, _fixedLength) - 1;
                if (number > maxValue)
                    throw new ArgumentOutOfRangeException(nameof(number), 
                        $"Number {number} exceeds maximum value {maxValue} for fixed length {_fixedLength}.");
            }

            if (number == 0) 
            {
                string zeroResult = Base62Chars[0].ToString();
                return _fixedLength > 0 ? zeroResult.PadLeft(_fixedLength, Base62Chars[0]) : zeroResult;
            }

            var sb = new StringBuilder();
            while (number > 0)
            {
                sb.Insert(0, Base62Chars[number % Base]);
                number /= Base;
            }
            
            string result = sb.ToString();
            
            // 如果设置了固定长度，则补齐前导零
            if (_fixedLength > 0 && result.Length < _fixedLength)
            {
                result = result.PadLeft(_fixedLength, Base62Chars[0]);
            }
            
            return result;
        }

        /// <summary>
        /// 将Base62字符串解码为数字
        /// </summary>
        /// <param name="code">要解码的Base62字符串</param>
        /// <returns>解码后的数字</returns>
        public long Decode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentNullException(nameof(code));

            long number = 0;
            foreach (char c in code)
            {
                var index = Array.IndexOf(Base62Chars, c);
                if (index == -1)
                    throw new ArgumentException("Invalid character in Base62 string.", nameof(code));
                number = number * Base + index;
            }
            return number;
        }
    }
}
