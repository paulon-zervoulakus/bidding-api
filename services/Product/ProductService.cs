using System.Runtime.CompilerServices;
using Azure.Identity;
using biddingServer.Models;
using ResponseDTO = DTO.Response.Product;
using Microsoft.EntityFrameworkCore;
using Azure;

namespace biddingServer.services.product
{
    public interface IProductService
    {
        Task<ProductModel?> GetById(int id);
        Task<ProductModel?> GetBySKU(string? sku);
        Task Update(ProductModel product);
        Task<List<ResponseDTO.ProductResponseDTO>> GetAll();
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

        public async Task<List<ResponseDTO.ProductResponseDTO>> GetAll()
        {
            var products = await _context.Products
                .Include(c => c.ProductCategory) // Include the immediate product category
                .Include(s => s.Seller)
                .Select(p => new ResponseDTO.ProductResponseDTO
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    Price = p.Price,
                    Quantity = p.Quantity,
                    SKU = p.SKU,
                    IsSerializable = p.IsSerializable,
                    ProductCondition = p.ProductCondition,
                    Seller = new ResponseDTO.Seller
                    {
                        Id = p.Seller.Id,
                        UserName = p.Seller.UserName,
                        Email = p.Seller.Email
                    },
                    ProductCategory = new ResponseDTO.ProductCategory
                    {
                        Id = p.ProductCategory.Id,
                        CategoryName = p.ProductCategory.CategoryName
                    }

                })
                .ToListAsync();

            return products;
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