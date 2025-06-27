using Microsoft.Extensions.Options;
using System;

namespace v2
{
    /// <summary>
    /// 短链接仓储工厂
    /// </summary>
    public class ShortUrlRepositoryFactory
    {
        private readonly ShortUrlOptions _options;

        public ShortUrlRepositoryFactory(IOptions<ShortUrlOptions> options)
        {
            _options = options.Value;
        }

        public ShortUrlRepositoryFactory(ShortUrlOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// 创建短链接仓储实例
        /// </summary>
        /// <returns>短链接仓储实例</returns>
        public IShortUrlRepository CreateRepository()
        {
            Console.WriteLine($"=== 创建短链接仓储 ===");
            Console.WriteLine($"连接字符串: {_options.ConnectionString}");
            Console.WriteLine($"实现类型: {_options.RepositoryType}");
            Console.WriteLine($"性能监控: {(_options.EnablePerformanceLogging ? "启用" : "禁用")}");
            Console.WriteLine("=====================");

            return _options.RepositoryType switch
            {
                ShortUrlRepositoryType.Original => new DbRepository(_options.ConnectionString),
                ShortUrlRepositoryType.AutoIncrement => new DbRepositoryAutoIncrement(_options.ConnectionString),
                _ => throw new ArgumentException($"不支持的仓储类型: {_options.RepositoryType}")
            };
        }

        /// <summary>
        /// 获取当前配置的描述信息
        /// </summary>
        /// <returns>配置描述</returns>
        public string GetConfigurationDescription()
        {
            return _options.RepositoryType switch
            {
                ShortUrlRepositoryType.Original => "原始实现 - 适合多实例部署，数据一致性更好",
                ShortUrlRepositoryType.AutoIncrement => "自增ID实现 - 适合单实例部署，性能更优",
                _ => "未知实现类型"
            };
        }
    }
}
