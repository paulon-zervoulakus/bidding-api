using System.Runtime.CompilerServices;
using Azure.Identity;
using biddingServer.Models;
using ResponseDTO = DTO.Response.Product;
using Microsoft.EntityFrameworkCore;
using Azure;
using DTO.Request.Product;
using Microsoft.AspNetCore.Mvc;

namespace biddingServer.services.product
{
    public interface IProductService
    {
        Task<ProductModel?> GetById(int id);
        Task<ProductModel?> GetBySKU(string? sku);
        Task Update(ProductModel product);
        Task<List<ResponseDTO.ProductResponseDTO>> GetAll();
        Task<ProductModel> Add(ProductModel product);
        Task<List<ResponseDTO.ProductResponseDTO>> GetByUser(int userId);
        Task<ProductModel?> Delete(int productId);

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
        public async Task<ProductModel?> Delete(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                product.Status = StatusEnum.Deleted; // Mark the product as deleted
                await _context.SaveChangesAsync();
                return product;
            }
            return null; // Return null if the product was not found
        }
        public async Task Update(ProductModel product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }
        public async Task<List<ResponseDTO.ProductResponseDTO>> GetByUser(int userId)
        {
            var products = await _context.Products
                .Include(c => c.ProductCategory) // Include the immediate product category
                .Include(s => s.Seller)
                .Where(u => u.Seller.Id == userId)
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
                    Status = p.Status,
                    Seller = new ResponseDTO.Seller
                    {
                        Id = p.Seller.Id,
                        UserName = p.Seller.UserName,
                        Email = p.Seller.Email
                    },
                    ProductCategoryId = p.ProductCategoryID,
                    ProductCategoryName = p.ProductCategory != null
                        ? p.ProductCategory.CategoryName
                        : null

                })
                .ToListAsync();

            return products;

        }
        public async Task<List<ResponseDTO.ProductResponseDTO>> GetAll()
        {
            var products = await _context.Products
                .Include(c => c.ProductCategory) // Include the immediate product category
                .Include(s => s.Seller)
                .Where(u => u.Status != StatusEnum.Deleted) // Exclude deleted products
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
                    ProductCategoryId = p.ProductCategoryID,
                    ProductCategoryName = p.ProductCategory != null
                        ? p.ProductCategory.CategoryName
                        : null

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