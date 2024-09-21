using System.Linq;
using biddingServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace biddingServer.Controllers.api
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductControllers(ApplicationDbContext context, IConfiguration configuration) : ControllerBase
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly ApplicationDbContext _context = context;

        [HttpGet("categories")]
        [Authorize(Roles = nameof(RoleEnumerated.Administrator))]
        public async Task<ActionResult<ProductCategoryModel>> Categories()
        {
            List<ProductCategoryModel> categories = await _context.ProductCategories
            .Include(x => x.SubCategories)
            // TODO: in the future define a limit of list result
            .ToListAsync();

            List<ProductCategoryModel> mainCategories = categories.FindAll(x => x.ParentCategoryId == null);
            return Ok(new { mainCategories });
        }

        [HttpGet("products")]
        [Authorize(Roles = $"{nameof(RoleEnumerated.Administrator)}, {nameof(RoleEnumerated.Seller)}")]
        public async Task<ActionResult<ProductModel>> products()
        {
            // TODO: in the future define a limit of list result
            List<ProductModel> products = await _context.Products.ToListAsync();
            return Ok(new { products });
        }
    }
}