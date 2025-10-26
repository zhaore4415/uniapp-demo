using backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {}

        // 添加新闻表
        public DbSet<News> News { get; set; }
        
        // 添加公司内容表
        public DbSet<CompanyContent> CompanyContents { get; set; }
        
        // 使用配置文件中的数据库连接字符串
        // 移除硬编码路径以避免配置冲突
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            // 保留EnableSensitiveDataLogging以方便开发调试
            optionsBuilder.EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {          
            base.OnModelCreating(builder);

            // 配置User实体
            builder.Entity<User>().Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // 配置News实体
            builder.Entity<News>().Property(n => n.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // 配置CompanyContent实体
            builder.Entity<CompanyContent>().Property(c => c.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            // 确保Key字段唯一
            builder.Entity<CompanyContent>().HasIndex(c => c.Key).IsUnique();
        }
    }
}