using System.Security.Claims;
using backend.Data;
using backend.Models;
using backend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private const int MaxQueryCount = 100;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LogsController> _logger;

        public LogsController(ApplicationDbContext context, ILogger<LogsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 前端异常上报：允许匿名；有有效 JWT 时用户名为 token 中的用户名
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> PostLog([FromBody] LogReportRequest request)
        {
            // 手工校验（不写 [Required]，避免 [ApiController] 返回 ProblemDetails 形状的 400）
            if (request == null || string.IsNullOrWhiteSpace(request.Type))
            {
                return BadRequest(new { message = "type 不能为空" });
            }

            // 客户端传的 username 一律忽略，只信任 token
            var username = User?.Identity?.IsAuthenticated == true
                ? User.FindFirstValue(ClaimTypes.Name)
                : null;

            try
            {
                _context.Logs.Add(new LogEntry
                {
                    CreatedAt = DateTime.Now,
                    Type = LogText.Truncate(request.Type, LogText.MaxTypeLength),
                    Username = LogText.NormalizeUsername(username),
                    Success = request.Success,
                    Message = LogText.Truncate(request.Message, LogText.MaxMessageLength)
                });
                await _context.SaveChangesAsync();
                return Ok(new { message = "日志已记录" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "写入前端上报日志失败");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "日志写入失败" });
            }
        }

        // 查询日志：任何有效 JWT 可读，倒序最近 100 条，可按 type 精确过滤
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetLogs([FromQuery] string? type = null)
        {
            var query = _context.Logs.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(type))
            {
                var t = type.Trim();
                query = query.Where(l => l.Type == t);
            }

            var items = await query
                .OrderByDescending(l => l.CreatedAt)
                .ThenByDescending(l => l.Id)
                .Take(MaxQueryCount)
                // 显式 camelCase 投影，固化接口契约（D12）
                .Select(l => new
                {
                    id = l.Id,
                    createdAt = l.CreatedAt,
                    type = l.Type,
                    username = l.Username,
                    success = l.Success,
                    message = l.Message
                })
                .ToListAsync();

            return Ok(items);
        }
    }

    public class LogReportRequest
    {
        public string? Type { get; set; }

        // 缺省 false，与"上报异常"语义一致
        public bool Success { get; set; }

        public string? Message { get; set; }

        // 故意不定义 Username：客户端传入会被忽略
    }
}
