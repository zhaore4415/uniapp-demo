namespace backend.Utils
{
    // 日志字段的长度保护与用户名归一化（D4）
    public static class LogText
    {
        public const int MaxTypeLength = 20;
        public const int MaxUsernameLength = 256;
        public const int MaxMessageLength = 4000;

        public static string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        public static string NormalizeUsername(string? username)
        {
            var name = Truncate(username?.Trim(), MaxUsernameLength);
            return string.IsNullOrEmpty(name) ? "anonymous" : name;
        }
    }
}
