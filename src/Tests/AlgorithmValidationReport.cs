using System;
using System.Collections.Generic;
using System.Diagnostics;
using url_short.common;

namespace Tests
{
    /// <summary>
    /// Base62编码算法性能和唯一性测试报告
    /// </summary>
    public class AlgorithmValidationReport
    {
        public static void GenerateReport()
        {
            Console.WriteLine("=== Base62编码算法验证报告 ===");
            Console.WriteLine($"测试时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            // 1. 基本唯一性验证
            Console.WriteLine("1. 大规模唯一性验证");
            TestUniqueness();
            Console.WriteLine();

            // 2. 性能测试
            Console.WriteLine("2. 编码解码性能测试");
            TestPerformance();
            Console.WriteLine();

            // 3. 固定长度验证
            Console.WriteLine("3. 固定长度编码验证");
            TestFixedLength();
            Console.WriteLine();

            // 4. 编码分布验证
            Console.WriteLine("4. 编码长度分布验证");
            TestLengthDistribution();
        }

        private static void TestUniqueness()
        {
            var converter = new Base62Converter();
            var codes = new HashSet<string>();
            int duplicates = 0;

            var sw = Stopwatch.StartNew();

            // 范围1: 1-10000
            for (long i = 1; i <= 10000; i++)
            {
                string code = converter.Encode(i);
                if (!codes.Add(code))
                    duplicates++;
            }

            // 范围2: 10001-1000000 (等差数列，间隔99)
            for (long i = 10001; i <= 1000000; i += 99)
            {
                string code = converter.Encode(i);
                if (!codes.Add(code))
                    duplicates++;
            }

            sw.Stop();

            Console.WriteLine($"  测试数量: {codes.Count:N0} 个唯一编码");
            Console.WriteLine($"  重复数量: {duplicates}");
            Console.WriteLine($"  测试耗时: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"  平均速度: {codes.Count / (sw.ElapsedMilliseconds + 1):N0} 编码/ms");
        }

        private static void TestPerformance()
        {
            var converter = new Base62Converter();
            const int testCount = 100000;

            // 编码性能测试
            var sw = Stopwatch.StartNew();
            for (long i = 1; i <= testCount; i++)
            {
                converter.Encode(i);
            }
            sw.Stop();
            long encodeTime = sw.ElapsedMilliseconds;

            // 解码性能测试
            var codes = new List<string>();
            for (long i = 1; i <= testCount; i++)
            {
                codes.Add(converter.Encode(i));
            }

            sw.Restart();
            foreach (string code in codes)
            {
                converter.Decode(code);
            }
            sw.Stop();
            long decodeTime = sw.ElapsedMilliseconds;

            Console.WriteLine($"  编码 {testCount:N0} 个数字: {encodeTime} ms");
            Console.WriteLine($"  解码 {testCount:N0} 个字符串: {decodeTime} ms");
            Console.WriteLine($"  编码速度: {testCount / (encodeTime + 1):N0} ops/ms");
            Console.WriteLine($"  解码速度: {testCount / (decodeTime + 1):N0} ops/ms");
        }

        private static void TestFixedLength()
        {
            var fixedConverter = new Base62Converter(6);
            var codes = new HashSet<string>();

            for (long i = 1; i <= 10000; i++)
            {
                string code = fixedConverter.Encode(i);
                codes.Add(code);
                
                if (code.Length != 6)
                {
                    Console.WriteLine($"  错误: 数字 {i} 编码为 '{code}' (长度 {code.Length})");
                    return;
                }
            }

            Console.WriteLine($"  测试数量: {codes.Count:N0} 个6位编码");
            Console.WriteLine($"  长度一致性: 100% (所有编码都是6位)");
            Console.WriteLine($"  唯一性: {codes.Count} 个唯一编码");
        }

        private static void TestLengthDistribution()
        {
            var converter = new Base62Converter();
            var lengthDistribution = new Dictionary<int, int>();

            for (long i = 1; i <= 100000; i++)
            {
                string code = converter.Encode(i);
                int length = code.Length;
                
                if (!lengthDistribution.ContainsKey(length))
                    lengthDistribution[length] = 0;
                lengthDistribution[length]++;
            }

            Console.WriteLine("  编码长度分布:");
            foreach (var kvp in lengthDistribution)
            {
                double percentage = (double)kvp.Value / 100000 * 100;
                Console.WriteLine($"    {kvp.Key}位: {kvp.Value:N0} 个 ({percentage:F2}%)");
            }
        }
    }
}
