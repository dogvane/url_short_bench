namespace v2
{
    /// <summary>
    /// 短链接仓储类型枚举
    /// </summary>
    public enum ShortUrlRepositoryType
    {
        /// <summary>
        /// 原始实现：先插入临时数据，再更新真实短码
        /// </summary>
        Original,
        
        /// <summary>
        /// 自增ID实现：内存自增ID，直接插入完整数据
        /// </summary>
        AutoIncrement
    }

    /// <summary>
    /// 短链接配置选项
    /// </summary>
    public class ShortUrlOptions
    {
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string ConnectionString { get; set; } = "Data Source=shortlinks.db";

        /// <summary>
        /// 仓储实现类型
        /// </summary>
        public ShortUrlRepositoryType RepositoryType { get; set; } = ShortUrlRepositoryType.Original;

        /// <summary>
        /// 是否启用性能监控日志
        /// </summary>
        public bool EnablePerformanceLogging { get; set; } = false;
    }
}
