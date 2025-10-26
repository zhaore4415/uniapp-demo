using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            // 数据库和表结构已在Program.cs中创建

            // 创建角色
            await SeedRolesAsync(roleManager);

            // 创建默认管理员用户（如果不存在）
            await SeedAdminUserAsync(userManager, roleManager);
            
            // 添加默认公司内容
            await SeedCompanyContentsAsync(context);
            
            // 添加示例新闻
            await SeedNewsAsync(context);
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            // 定义需要的角色
            var roles = new List<string> { "Admin", "User", "Manager" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task SeedAdminUserAsync(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            // 检查是否已存在管理员用户
            var adminEmail = "admin@example.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                // 创建管理员用户
                adminUser = new User
                {
                    UserName = "admin",
                    Email = adminEmail,
                    FullName = "系统管理员",
                    Address = "系统管理部门",
                    EmailConfirmed = true,
                    CreatedAt = System.DateTime.Now
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    // 确保管理员角色存在
                    if (!await roleManager.RoleExistsAsync("Admin"))
                    {
                        await roleManager.CreateAsync(new IdentityRole("Admin"));
                    }

                    // 分配管理员角色
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
        
        private static async Task SeedCompanyContentsAsync(ApplicationDbContext context)
        {
            // 检查是否已有公司内容数据
            if (!context.CompanyContents.Any())
            {
                var defaultContents = new List<CompanyContent>
                {
                    new CompanyContent
                    {
                        Key = "about",
                        Title = "关于我们",
                        Content = "<h2>公司简介</h2><p>我们是一家致力于为客户提供优质产品和服务的企业。</p><p>成立于2024年，我们拥有一支专业的团队，致力于创新和卓越。</p>",
                        Description = "了解我们公司的历史、使命和价值观",
                        Keywords = "公司简介,关于我们,企业文化"
                    },
                    new CompanyContent
                    {
                        Key = "contact",
                        Title = "联系我们",
                        Content = "<h2>联系方式</h2><p>地址：中国上海市浦东新区张江高科技园区</p><p>电话：400-123-4567</p><p>邮箱：contact@example.com</p>",
                        Description = "通过多种方式联系我们",
                        Keywords = "联系我们,联系方式,客服"
                    },
                    new CompanyContent
                    {
                        Key = "privacy",
                        Title = "隐私政策",
                        Content = "<h2>隐私政策</h2><p>我们重视用户隐私，承诺保护您的个人信息安全。</p><p>本政策详细说明了我们如何收集、使用和保护您的数据。</p>",
                        Description = "了解我们如何保护您的个人信息",
                        Keywords = "隐私政策,个人信息,数据保护"
                    },
                    new CompanyContent
                    {
                        Key = "terms",
                        Title = "服务条款",
                        Content = "<h2>服务条款</h2><p>欢迎使用我们的服务。</p><p>本条款规定了使用我们服务的条件和限制。</p>",
                        Description = "使用我们服务的条款和条件",
                        Keywords = "服务条款,使用条件,用户协议"
                    }
                };
                
                await context.CompanyContents.AddRangeAsync(defaultContents);
                await context.SaveChangesAsync();
            }
        }
        
        private static async Task SeedNewsAsync(ApplicationDbContext context)
        {
            // 检查是否已有新闻数据
            if (!context.News.Any())
            {
                var sampleNews = new List<News>
                {
                    new News
                    {
                        Title = "公司正式成立",
                        Content = "<h2>公司正式成立</h2><p>我们很高兴地宣布，公司于2024年1月1日正式成立。</p><p>我们将致力于为客户提供最优质的产品和服务。</p>",
                        Summary = "公司正式成立，开启新的征程。",
                        Author = "系统管理员",
                        IsPublished = true,
                        PublishedAt = DateTime.Now.AddDays(-30)
                    },
                    new News
                    {
                        Title = "首款产品发布",
                        Content = "<h2>首款产品发布</h2><p>我们的首款产品已经正式发布，欢迎广大客户体验。</p><p>产品融合了最新技术，为用户带来全新体验。</p>",
                        Summary = "首款产品正式发布，欢迎体验。",
                        Author = "产品经理",
                        IsPublished = true,
                        PublishedAt = DateTime.Now.AddDays(-15)
                    }
                };
                
                await context.News.AddRangeAsync(sampleNews);
                await context.SaveChangesAsync();
            }
        }
    }
}