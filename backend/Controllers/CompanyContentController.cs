using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanyContentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CompanyContentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 获取所有公司内容（公开接口）
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CompanyContent>>> GetCompanyContents()
        {
            return await _context.CompanyContents.OrderBy(c => c.Key).ToListAsync();
        }

        // 根据Key获取特定内容
        [HttpGet("{key}")]
        public async Task<ActionResult<CompanyContent>> GetCompanyContentByKey(string key)
        {
            var content = await _context.CompanyContents.FirstOrDefaultAsync(c => c.Key == key);
            
            if (content == null)
            {
                return NotFound();
            }
            
            return content;
        }

        // 创建或更新公司内容（需要管理员权限）
        [Authorize(Roles = "Admin")]
        [HttpPut("{key}")]
        public async Task<ActionResult<CompanyContent>> UpdateCompanyContent(string key, CompanyContent content)
        {
            // 确保Key一致
            content.Key = key;
            
            // 查找现有内容
            var existingContent = await _context.CompanyContents.FirstOrDefaultAsync(c => c.Key == key);
            
            if (existingContent == null)
            {
                // 创建新内容
                content.CreatedAt = DateTime.Now;
                _context.CompanyContents.Add(content);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetCompanyContentByKey), new { key = content.Key }, content);
            }
            else
            {
                // 更新现有内容
                existingContent.Title = content.Title;
                existingContent.Content = content.Content;
                existingContent.Description = content.Description;
                existingContent.Keywords = content.Keywords;
                existingContent.UpdatedAt = DateTime.Now;
                
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw;
                }
                
                return existingContent;
            }
        }

        // 删除公司内容（需要管理员权限）
        [Authorize(Roles = "Admin")]
        [HttpDelete("{key}")]
        public async Task<IActionResult> DeleteCompanyContent(string key)
        {
            var content = await _context.CompanyContents.FirstOrDefaultAsync(c => c.Key == key);
            if (content == null)
            {
                return NotFound();
            }
            
            _context.CompanyContents.Remove(content);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }
    }
}