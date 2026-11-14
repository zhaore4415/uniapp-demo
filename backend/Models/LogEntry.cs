using System;

namespace backend.Models
{
    // 业务日志：登录 / 注册 / 异常
    public class LogEntry
    {
        public int Id { get; set; }

        // 与 User/News/CompanyContent 保持一致，统一使用本地时间（D3）
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 登录 / 注册 / 异常
        public string Type { get; set; } = string.Empty;

        public string Username { get; set; } = "anonymous";

        public bool Success { get; set; }

        // 具体原因；异常时拼入 "类型: 消息 + 堆栈"（假设 1）
        public string? Message { get; set; }
    }
}
