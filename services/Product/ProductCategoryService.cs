using biddingServer.Models;
using Microsoft.EntityFrameworkCore;

namespace biddingServer.services.product
{
    public interface IProductCategoryService
    {
        Task<List<ProductCategoryModel>> GetMainCategories();
        Task<ProductCategoryModel?> GetById(int id);
    }
    public class ProductCategoryService : IProductCategoryService
    {
        private readonly ApplicationDbContext _context; // DbContext or repository
        public ProductCategoryService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<ProductCategoryModel>> GetMainCategories()
        {
            List<ProductCategoryModel> categories = await _context.ProductCategories
                .Include(x => x.SubCategories)
                // TODO: in the future define a limit of list result
                .ToListAsync();

            var mainCategories = categories.FindAll(x => x.ParentCategoryId == null);
            return mainCategories;
        }
        public async Task<ProductCategoryModel?> GetById(int id)
        {
            return await _context.ProductCategories.FindAsync(id);
        }
    }
}