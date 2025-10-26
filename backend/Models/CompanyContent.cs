using System;

namespace backend.Models
{
    public class CompanyContent
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty; // 用于标识内容类型，如 "about", "contact", "privacy" 等
        public string Title { get; set; } = string.Empty; // 页面标题
        public string Content { get; set; } = string.Empty; // 页面内容
        public string? Description { get; set; } // SEO描述
        public string? Keywords { get; set; } // SEO关键词
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}