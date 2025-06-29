using System.Diagnostics;

namespace v2
{
    public class MonitoringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly MonitoringService _monitoringService;

        public MonitoringMiddleware(RequestDelegate next, MonitoringService monitoringService)
        {
            _next = next;
            _monitoringService = monitoringService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? "";
            
            // 简化路径，将动态部分归类
            var endpoint = NormalizeEndpoint(path);

            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();
                var statusCode = context.Response.StatusCode;
                var duration = sw.Elapsed.TotalSeconds;

                _monitoringService.RecordHttpRequest(method, endpoint, statusCode, duration);
            }
        }

        private static string NormalizeEndpoint(string path)
        {
            // 将具体的短链接代码替换为通用标识符
            if (path.StartsWith("/") && path.Length > 1 && !path.Contains("/create") && !path.Contains("/metrics"))
            {
                return "/redirect/{code}";
            }

            if (path.Contains("/create"))
            {
                return "/create";
            }

            if (path.Contains("/metrics"))
            {
                return "/metrics";
            }

            return path;
        }
    }
}
