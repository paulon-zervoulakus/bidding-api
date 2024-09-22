using System.Runtime.CompilerServices;
using biddingServer.Models;
using Microsoft.EntityFrameworkCore;

namespace biddingServer.services.product
{
    public interface IProductService
    {
        Task<ProductModel?> GetById(int id);
        Task<ProductModel?> GetBySKU(string? sku);
        Task Update(ProductModel product);
        Task<List<ProductModel>> GetAll();
        Task<ProductModel> Add(ProductModel product);
    }

    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context; // DbContext or repository

        public ProductService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ProductModel?> GetById(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<ProductModel?> GetBySKU(string? sku)
        {
            return await _context.Products.FirstOrDefaultAsync(p => p.SKU == sku);
        }

        public async Task Update(ProductModel product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ProductModel>> GetAll()
        {
            return await _context.Products.ToListAsync();
        }
        // Method to add a new product
        public async Task<ProductModel> Add(ProductModel newProduct)
        {
            // Add the product to the context
            _context.Products.Add(newProduct);

            // Save the changes to the database
            await _context.SaveChangesAsync();

            // Return the newly added product
            return newProduct;
        }
    }
}