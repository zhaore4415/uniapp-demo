using System;
using System.Security.Claims;
using backend.Data;
using backend.Models;
using backend.Utils;
using Microsoft.EntityFrameworkCore;

namespace backend.Middleware
{
    // 只兜"未被处理、最终会变成 5xx"的异常；4xx 由框架正常返回，不经过这里
    public class ExceptionLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionLoggingMiddleware> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ExceptionLoggingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionLoggingMiddleware> logger,
            IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "未处理异常 {Method} {Path}", context.Request.Method, context.Request.Path);

                if (!IsIgnored(context.Request.Path))
                {
                    await TryWriteLogAsync(context, ex);
                }

                // 响应已开始发送则无法改写，交还服务器处理
                if (context.Response.HasStarted) throw;

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json; charset=utf-8";
                // 不回显堆栈
                await context.Response.WriteAsync("{\"message\":\"服务器内部错误\"}");
            }
        }

        private static bool IsIgnored(PathString path) =>
            path.StartsWithSegments("/swagger") || path.StartsWithSegments("/favicon.ico");

        private async Task TryWriteLogAsync(HttpContext context, Exception ex)
        {
            try
            {
                // 认证中间件已在管道下游执行，异常上抛时可读到已填充的 User；
                // 未认证 / 无效 token 时回落 anonymous
                var username = context.User?.Identity?.IsAuthenticated == true
                    ? context.User.FindFirstValue(ClaimTypes.Name)
                    : null;

                var message = $"{ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}";

                // 独立 scope：不复用可能已处于异常/未提交状态的请求级 DbContext
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Logs.Add(new LogEntry
                {
                    CreatedAt = DateTime.Now,
                    Type = "异常",
                    Username = LogText.NormalizeUsername(username),
                    Success = false,
                    Message = LogText.Truncate(message, LogText.MaxMessageLength)
                });
                await db.SaveChangesAsync();
            }
            catch (Exception logEx)
            {
                // 写日志失败不影响 500 响应
                _logger.LogError(logEx, "写入异常日志失败");
            }
        }
    }
}
