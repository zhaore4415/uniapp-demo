using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public AuthController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _context = context;
        }

        // 注册新用户
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 检查邮箱是否已存在
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
            {
                return BadRequest(new { Message = "该邮箱已被注册" });
            }

            // 检查用户名是否已存在
            userExists = await _userManager.FindByNameAsync(model.Username);
            if (userExists != null)
            {
                return BadRequest(new { Message = "该用户名已被使用" });
            }

            // 创建新用户
            var user = new User()
            {
                UserName = model.Username,
                Email = model.Email,
                FullName = model.FullName,
                Address = model.Address,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new { Message = "用户创建失败", Errors = result.Errors.Select(e => e.Description) });
            }

            // 为新用户分配默认角色
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                await _roleManager.CreateAsync(new IdentityRole("User"));
            }

            await _userManager.AddToRoleAsync(user, "User");

            return Ok(new { Message = "用户注册成功", UserId = user.Id });
        }

        // 用户登录
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 查找用户
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(model.Username);
                if (user == null)
                {
                    return Unauthorized(new { Message = "用户名或密码错误" });
                }
            }

            // 验证密码
            if (!await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return Unauthorized(new { Message = "用户名或密码错误" });
            }

            // 更新最后登录时间
            user.LastLogin = DateTime.Now;
            await _userManager.UpdateAsync(user);

            // 获取用户角色
            var userRoles = await _userManager.GetRolesAsync(user);

            // 创建Claims
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            // 生成JWT令牌
            var token = GenerateToken(authClaims);

            return Ok(new
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = token.ValidTo,
                UserId = user.Id,
                Username = user.UserName,
                FullName = user.FullName,
                Email = user.Email,
                Roles = userRoles
            });
        }

        // 获取当前用户信息
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "用户不存在" });
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                FullName = user.FullName,
                BirthDate = user.BirthDate,
                Address = user.Address,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin,
                Roles = userRoles
            });
        }

        // 更新用户信息
        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "用户不存在" });
            }

            // 更新用户信息
            if (!string.IsNullOrEmpty(model.FullName))
            {
                user.FullName = model.FullName;
            }

            if (model.BirthDate.HasValue)
            {
                user.BirthDate = model.BirthDate;
            }

            if (!string.IsNullOrEmpty(model.Address))
            {
                user.Address = model.Address;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { Message = "更新失败", Errors = result.Errors.Select(e => e.Description) });
            }

            return Ok(new { Message = "更新成功", User = user });
        }

        // 修改密码
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "用户不存在" });
            }

            // 验证当前密码
            if (!await _userManager.CheckPasswordAsync(user, model.CurrentPassword))
            {
                return BadRequest(new { Message = "当前密码错误" });
            }

            // 修改密码
            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new { Message = "密码修改失败", Errors = result.Errors.Select(e => e.Description) });
            }

            return Ok(new { Message = "密码修改成功" });
        }

        // 生成JWT令牌的私有方法
        private JwtSecurityToken GenerateToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var expirationHours = Convert.ToInt32(_configuration["Jwt:ExpirationHours"]);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.Now.AddHours(expirationHours),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return token;
        }
    }

    // 数据模型类
    public class RegisterModel
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class LoginModel
    {
        public string Username { get; set; } = string.Empty; // 可以是用户名或邮箱
        public string Password { get; set; } = string.Empty;
    }

    public class UpdateProfileModel
    {
        public string? FullName { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? Address { get; set; }
    }

    public class ChangePasswordModel
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}