using backend.Data;
using backend.Middleware;
using backend.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ✅ 必须添加这一行！
builder.Services.AddControllers();
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// 添加Swagger UI支持
builder.Services.AddEndpointsApiExplorer();

// 添加CORS支持
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// 添加数据库上下文
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 添加身份验证服务
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 配置密码策略
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
    
    // 锁定策略
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // 用户设置
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
});

// 配置JWT认证
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // 开发环境可以设为false
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

// 添加授权服务
builder.Services.AddAuthorization();



var app = builder.Build();

// 添加这行：允许所有来源（仅开发用）
app.Urls.Add("http://0.0.0.0:5000");

// 初始化数据库
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    
    // 首先创建数据库和表结构
    var context = services.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();

    // 补建日志表：EnsureCreated 对已存在的库不会补建新表，
    // 因此用幂等 DDL 补建；不删除、不重建任何现有表，保留 app.db 既有数据（D1）。
    try
    {
        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS Logs (
    Id        INTEGER NOT NULL CONSTRAINT PK_Logs PRIMARY KEY AUTOINCREMENT,
    CreatedAt TEXT    NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    Type      TEXT    NOT NULL,
    Username  TEXT    NOT NULL DEFAULT 'anonymous',
    Success   INTEGER NOT NULL,
    Message   TEXT    NULL
);");
        context.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS IX_Logs_CreatedAt ON Logs (CreatedAt);");
    }
    catch (System.Exception ex)
    {
        // 日志是可观测性附加能力，不应成为启动单点故障（D13）
        services.GetRequiredService<ILogger<Program>>()
                .LogError(ex, "创建 Logs 表失败，日志功能将不可用（不影响其它接口）");
    }

    // 然后执行数据初始化
    try
    {
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        
        await DbInitializer.Initialize(context, userManager, roleManager);
    }
    catch (System.Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

// 异常日志中间件必须最靠前，才能兜住后续所有中间件与控制器的未处理异常
app.UseMiddleware<ExceptionLoggingMiddleware>();

// 启用静态文件中间件
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // 配置Swagger
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

// 在身份验证之前启用CORS
app.UseCors("AllowAll");

// 使用身份验证和授权中间件
app.UseAuthentication();
app.UseAuthorization();

// ✅ 添加这一行！否则 Controller 不会被加载
app.MapControllers(); // 必须有这一句！

// 添加根路径响应
app.MapGet("/", () => Results.Ok(new
{
    message = "AppDemo API 服务正在运行",
    version = "v1",
    documentation = "/swagger/v1/swagger.json",
    availableEndpoints = new string[]
    {
        "/api/auth/register - 用户注册",
        "/api/auth/login - 用户登录",
        "/api/auth/profile - 获取/更新用户资料",
        "/api/auth/change-password - 修改密码",
        "/api/company/info - 获取公司信息",
        "/api/company/products - 获取产品列表"
    }
}));

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
