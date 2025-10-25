// Controllers/CompanyController.cs
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CompanyController : ControllerBase
{
    [HttpGet("info")]
    public IActionResult GetCompanyInfo()
    {
        var company = new
        {
            Name = "我的公司",
            Address = "北京市朝阳区某某大厦",
            Phone = "010-12345678",
            Website = "https://example.com"
        };
        return Ok(company);
    }

    [HttpGet("products")]
    public IActionResult GetProducts()
    {
        var products = new[]
        {
            new { Id = 1, Name = "产品A", Price = 99 },
            new { Id = 2, Name = "产品B", Price = 199 }
        };
        return Ok(products);
    }
}