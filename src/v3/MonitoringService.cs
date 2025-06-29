using Prometheus;
using System.Diagnostics;

namespace v2
{
    public class MonitoringService
    {
        // HTTP 请求相关指标
        private static readonly Counter HttpRequestsTotal = Metrics
            .CreateCounter("http_requests_total", "Total HTTP requests", new[] { "method", "endpoint", "status" });

        private static readonly Histogram HttpRequestDuration = Metrics
            .CreateHistogram("http_request_duration_seconds", "HTTP request duration in seconds", new[] { "method", "endpoint" });

        // URL 短链接业务指标
        private static readonly Counter ShortUrlCreatedTotal = Metrics
            .CreateCounter("shorturl_created_total", "Total short URLs created");

        private static readonly Counter ShortUrlQueriedTotal = Metrics
            .CreateCounter("shorturl_queried_total", "Total short URL queries", new[] { "cache_hit" });

        private static readonly Histogram ShortUrlCreationDuration = Metrics
            .CreateHistogram("shorturl_creation_duration_seconds", "Short URL creation duration in seconds");

        private static readonly Histogram ShortUrlQueryDuration = Metrics
            .CreateHistogram("shorturl_query_duration_seconds", "Short URL query duration in seconds", new[] { "cache_hit" });

        // 数据库相关指标
        private static readonly Counter DatabaseOperationsTotal = Metrics
            .CreateCounter("database_operations_total", "Total database operations", new[] { "operation", "status" });

        private static readonly Histogram DatabaseOperationDuration = Metrics
            .CreateHistogram("database_operation_duration_seconds", "Database operation duration in seconds", new[] { "operation" });

        // Redis 缓存相关指标
        private static readonly Counter CacheOperationsTotal = Metrics
            .CreateCounter("cache_operations_total", "Total cache operations", new[] { "operation", "status" });

        private static readonly Histogram CacheOperationDuration = Metrics
            .CreateHistogram("cache_operation_duration_seconds", "Cache operation duration in seconds", new[] { "operation" });

        // 系统性能指标
        private static readonly Gauge ActiveConnections = Metrics
            .CreateGauge("active_connections", "Current active connections");

        private static readonly Gauge MemoryUsageBytes = Metrics
            .CreateGauge("memory_usage_bytes", "Current memory usage in bytes");

        private static readonly Gauge CpuUsagePercent = Metrics
            .CreateGauge("cpu_usage_percent", "Current CPU usage percentage");

        // 错误和异常指标
        private static readonly Counter ErrorsTotal = Metrics
            .CreateCounter("errors_total", "Total errors", new[] { "type", "operation" });

        // HTTP 请求监控
        public void RecordHttpRequest(string method, string endpoint, int statusCode, double durationSeconds)
        {
            HttpRequestsTotal.WithLabels(method, endpoint, statusCode.ToString()).Inc();
            HttpRequestDuration.WithLabels(method, endpoint).Observe(durationSeconds);
        }

        // 短链接创建监控
        public void RecordShortUrlCreated(double durationSeconds)
        {
            ShortUrlCreatedTotal.Inc();
            ShortUrlCreationDuration.Observe(durationSeconds);
        }

        // 短链接查询监控
        public void RecordShortUrlQueried(bool cacheHit, double durationSeconds)
        {
            var cacheHitLabel = cacheHit ? "true" : "false";
            ShortUrlQueriedTotal.WithLabels(cacheHitLabel).Inc();
            ShortUrlQueryDuration.WithLabels(cacheHitLabel).Observe(durationSeconds);
        }

        // 数据库操作监控
        public void RecordDatabaseOperation(string operation, bool success, double durationSeconds)
        {
            var status = success ? "success" : "error";
            DatabaseOperationsTotal.WithLabels(operation, status).Inc();
            DatabaseOperationDuration.WithLabels(operation).Observe(durationSeconds);
        }

        // 缓存操作监控
        public void RecordCacheOperation(string operation, bool success, double durationSeconds)
        {
            var status = success ? "success" : "error";
            CacheOperationsTotal.WithLabels(operation, status).Inc();
            CacheOperationDuration.WithLabels(operation).Observe(durationSeconds);
        }

        // 错误记录
        public void RecordError(string errorType, string operation)
        {
            ErrorsTotal.WithLabels(errorType, operation).Inc();
        }

        // 系统指标更新（可以在后台服务中定期调用）
        public void UpdateSystemMetrics()
        {
            // 内存使用
            var process = Process.GetCurrentProcess();
            MemoryUsageBytes.Set(process.WorkingSet64);

            // CPU 使用率需要更复杂的计算，这里简化处理
            // 在实际项目中可能需要使用 PerformanceCounter 或其他方式
        }

        // 活跃连接数更新
        public void SetActiveConnections(int count)
        {
            ActiveConnections.Set(count);
        }

        // 创建计时器辅助方法
        public static IDisposable TimeOperation(Histogram histogram, params string[] labels)
        {
            return histogram.WithLabels(labels).NewTimer();
        }

        // 用于测量数据库操作的辅助方法
        public async Task<T> MeasureDatabaseOperation<T>(string operation, Func<Task<T>> operation_func)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await operation_func();
                RecordDatabaseOperation(operation, true, sw.Elapsed.TotalSeconds);
                return result;
            }
            catch (Exception)
            {
                RecordDatabaseOperation(operation, false, sw.Elapsed.TotalSeconds);
                RecordError("database_exception", operation);
                throw;
            }
        }

        // 用于测量缓存操作的辅助方法
        public async Task<T> MeasureCacheOperation<T>(string operation, Func<Task<T>> operation_func)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await operation_func();
                RecordCacheOperation(operation, true, sw.Elapsed.TotalSeconds);
                return result;
            }
            catch (Exception)
            {
                RecordCacheOperation(operation, false, sw.Elapsed.TotalSeconds);
                RecordError("cache_exception", operation);
                throw;
            }
        }
    }
}
