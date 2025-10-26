using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 获取所有新闻（公开接口）
        [HttpGet]
        public async Task<ActionResult<IEnumerable<News>>> GetNews([FromQuery] bool? published = null)
        {
            var query = _context.News.AsQueryable();
            
            // 如果指定了发布状态，进行过滤
            if (published.HasValue)
            {
                query = query.Where(n => n.IsPublished == published.Value);
            }
            
            // 按创建时间倒序排列
            return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
        }

        // 获取单条新闻详情
        [HttpGet("{id}")]
        public async Task<ActionResult<News>> GetNewsById(int id)
        {
            var news = await _context.News.FindAsync(id);
            
            if (news == null)
            {
                return NotFound();
            }
            
            // 非管理员只能查看已发布的新闻
            if (!news.IsPublished && !User.IsInRole("Admin"))
            {
                return NotFound();
            }
            
            return news;
        }

        // 创建新闻（需要管理员权限）
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<News>> CreateNews(News news)
        {
            news.CreatedAt = DateTime.Now;
            
            if (news.IsPublished)
            {
                news.PublishedAt = DateTime.Now;
            }
            
            _context.News.Add(news);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetNewsById), new { id = news.Id }, news);
        }

        // 更新新闻（需要管理员权限）
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNews(int id, News news)
        {
            if (id != news.Id)
            {
                return BadRequest();
            }
            
            var existingNews = await _context.News.FindAsync(id);
            if (existingNews == null)
            {
                return NotFound();
            }
            
            // 更新字段
            existingNews.Title = news.Title;
            existingNews.Content = news.Content;
            existingNews.Summary = news.Summary;
            existingNews.Author = news.Author;
            existingNews.CoverImage = news.CoverImage;
            existingNews.UpdatedAt = DateTime.Now;
            
            // 如果从草稿变为已发布，设置发布时间
            if (!existingNews.IsPublished && news.IsPublished)
            {
                existingNews.IsPublished = true;
                existingNews.PublishedAt = DateTime.Now;
            }
            else
            {
                existingNews.IsPublished = news.IsPublished;
            }
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NewsExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            
            return NoContent();
        }

        // 删除新闻（需要管理员权限）
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNews(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null)
            {
                return NotFound();
            }
            
            _context.News.Remove(news);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }

        private bool NewsExists(int id)
        {
            return _context.News.Any(e => e.Id == id);
        }
    }
}