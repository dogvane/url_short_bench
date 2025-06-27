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
            if (number == 0) return Characters[0].ToString();

            var sb = new StringBuilder();
            while (number > 0)
            {
                sb.Insert(0, Characters[(int)(number % Base)]);
                number /= Base;
            }
            return sb.ToString();
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
            return number;
        }
    }
}
