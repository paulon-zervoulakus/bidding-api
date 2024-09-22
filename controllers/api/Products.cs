using System.Security.Claims;
using biddingServer.Models;
using DTO.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using biddingServer.services.product;

namespace biddingServer.Controllers.api
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductControllers(
        IProductService productService,
        IProductCategoryService productCategoryService,
        IProductImagesService productImagesService
    ) : ControllerBase
    {
        // private readonly IConfiguration _configuration = configuration;
        private readonly IProductService _productService = productService;
        private readonly IProductCategoryService _productCategoryService = productCategoryService;
        private readonly IProductImagesService _productImagesService = productImagesService;

        [HttpGet("categories")]
        [Authorize(Roles = nameof(RoleEnumerated.Administrator))]
        public async Task<ActionResult<ProductCategoryModel>> Categories()
        {
            List<ProductCategoryModel> mainCategories = await _productCategoryService.GetMainCategories();
            return Ok(new
            {
                mainCategories
            });
        }

        [HttpGet("products")]
        [Authorize(Roles = $"{nameof(RoleEnumerated.Administrator)}, {nameof(RoleEnumerated.Seller)}")]
        public async Task<ActionResult<List<ProductModel>>> Products()
        {
            // TODO: in the future define a limit of list result
            List<ProductModel> products = await _productService.GetAll();
            return Ok(new { products });
        }

        [HttpPost("product-update")]
        [Authorize(Roles = $"{nameof(RoleEnumerated.Administrator)}, {nameof(RoleEnumerated.Seller)}")]
        public async Task<ActionResult<ProductModel>> ProductUpdate([FromBody] ProductDTO product)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existingProduct = await _productService.GetBySKU(product.SKU);
            if (existingProduct != null && existingProduct.Id != product.Id) return BadRequest("A product with the same SKU already exists.");

            var category = await _productCategoryService.GetById(product.ProductCategoryID);
            if (category == null) return BadRequest("Invalid product category.");

            // 3. Authorization and Permissions Validation
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var productInDb = await _productService.GetById(product.Id ?? 0);
            if (productInDb == null)
            {
                return NotFound();
            }

            if (int.TryParse(userId, out int userIdAsInt))
            {
                // Ensure that the product belongs to the user or user is an admin
                if (productInDb.SellerID != userIdAsInt && !User.IsInRole(RoleEnumerated.Administrator.ToString()))
                {
                    return Forbid();
                }
            }
            else
            {
                return Forbid();
            }

            // 4. Update Product Logic
            try
            {
                productInDb.Title = product.Title;
                productInDb.Description = product.Description;
                productInDb.Price = product.Price;
                productInDb.Quantity = product.Quantity;
                productInDb.SKU = product.SKU;
                productInDb.IsSerializable = product.IsSerializable;
                productInDb.ProductCondition = product.ProductCondition;
                productInDb.ProductCategoryID = product.ProductCategoryID;

                await _productService.Update(productInDb);
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error updating product");
                return StatusCode(500, "An error occurred while updating the product.");
            }

            // 5. Return the updated product
            return Ok(productInDb);
        }
        [HttpPost("product-addnew")]
        [Authorize(Roles = $"{nameof(RoleEnumerated.Administrator)}, {nameof(RoleEnumerated.Seller)}")]
        public async Task<ActionResult<ProductModel>> ProductAddNew([FromBody] ProductDTO product)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existingProduct = await _productService.GetBySKU(product.SKU);
            if (existingProduct != null) return BadRequest("A product with the same SKU already exists.");

            var category = await _productCategoryService.GetById(product.ProductCategoryID);
            if (category == null) return BadRequest("Invalid product category.");

            try
            {
                var newProduct = new ProductModel
                {
                    Title = product.Title,
                    Description = product.Description,
                    Price = product.Price,
                    Quantity = product.Quantity,
                    SKU = product.SKU,
                    IsSerializable = product.IsSerializable,
                    ProductCondition = product.ProductCondition,
                    ProductCategoryID = product.ProductCategoryID,
                    SellerID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0") // Assuming SellerID is the user's ID
                };

                await _productService.Add(newProduct); // Assuming _productService.Add adds the new product
                return Ok(newProduct);
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error adding new product");
                return StatusCode(500, "An error occurred while adding the new product.");
            }
        }
        [HttpGet("product-images")]
        [Authorize(Roles = $"{nameof(RoleEnumerated.Administrator)}, {nameof(RoleEnumerated.Seller)}")]
        public async Task<ActionResult<ProductImagesModel>> ProductImagesList(int productId)
        {
            var images = await _productImagesService.GetByProductId(productId);
            if (images == null) return BadRequest();

            return Ok(images);
        }

        [HttpPost("upload")]
        [Authorize(Roles = $"{nameof(RoleEnumerated.Administrator)}, {nameof(RoleEnumerated.Seller)}")]
        public async Task<IActionResult> UploadProductImage(int productId, IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest("No image file provided.");
            }

            try
            {
                string uploadFolderPath = "uploads/products"; // Relative path for image storage
                string imagePath = await _productImagesService.UploadImageAsync(imageFile, uploadFolderPath, productId);

                return Ok(new { imagePath });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}