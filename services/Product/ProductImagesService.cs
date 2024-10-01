using ImageMagick;
using biddingServer.Models;
using Microsoft.EntityFrameworkCore;
namespace biddingServer.services.product
{
    public class ImageProcessingService
    {
        public async Task ResizeImageAsync(string sourcePath, string outputPath, uint width, uint height)
        {
            using (var image = new MagickImage(sourcePath))
            {
                image.Resize(width, height); // Resize while maintaining aspect ratio
                image.Crop(width, height); // Crop to ensure it fits the exact dimensions
                await image.WriteAsync(outputPath);
            }
        }
    }
    public interface IProductImagesService
    {
        Task<List<ProductImagesModel>> GetByProductId(int productId);
        Task<string> UploadImageAsync(IFormFile imageFile, string uploadFolderPath, int productId);

    }

    public class ProductImagesService : ImageProcessingService, IProductImagesService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context; // DbContext or repository
        public ProductImagesService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<List<ProductImagesModel>> GetByProductId(int productId)
        {
            return await _context.ProductImages.Where(x => x.ProductId == productId).ToListAsync();
        }
        public async Task<string> UploadImageAsync(IFormFile imageFile, string uploadFolderPath, int productId)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                throw new ArgumentException("Invalid image file");
            }

            // Ensure the upload folder exists
            string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, uploadFolderPath, productId.ToString());
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Generate a unique file name for the image
            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            string filePath = Path.Combine(uploadPath, uniqueFileName);

            // Save the image file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }
            // Resize to thumbnail (100x100)
            string thumbnailPath = Path.Combine(uploadPath, "thumb_" + uniqueFileName);
            await ResizeImageAsync(filePath, thumbnailPath, 100, 100);

            // Resize to square (500x500)
            string squareImagePath = Path.Combine(uploadPath, "square_" + uniqueFileName);
            await ResizeImageAsync(filePath, squareImagePath, 500, 500);

            // Resize to icon (50x50)
            string iconPath = Path.Combine(uploadPath, "icon_" + uniqueFileName);
            await ResizeImageAsync(filePath, iconPath, 50, 50);

            // Return the relative path to store in the database
            string imgPath = Path.Combine(uploadFolderPath, productId.ToString(), uniqueFileName).Replace("\\", "/");

            await _context.ProductImages
                .Where(x => x.ProductId == productId)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.IsPrimary, y => false));

            await _context.ProductImages.AddAsync(new ProductImagesModel
            {
                ProductId = productId,
                IsPrimary = true,
                OriginalSize = imgPath,
                IconSize = iconPath,
                ThubmnailSize = thumbnailPath,
                SquareSize = squareImagePath
            });
            return imgPath;
        }

    }
}