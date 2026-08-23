using Darbak.Data;
using Darbak.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Darbak.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductImagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProductImagesController> _logger;

        private const long MaxImageSize = 5 * 1024 * 1024;
        private const string ProductImagesRelativeFolder = "images/products";

        public ProductImagesController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<ProductImagesController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // INDEX
        [HttpGet]
        public async Task<IActionResult> Index(
            int productId,
            CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Images)
                .FirstOrDefaultAsync(
                    p => p.Id == productId,
                    cancellationToken);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // CREATE GET
        [HttpGet]
        public async Task<IActionResult> Create(
            int productId,
            CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.Id == productId,
                    cancellationToken);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.ProductName = product.Name;

            return View(new ProductImageCreateViewModel
            {
                ProductId = product.Id
            });
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ProductImageCreateViewModel viewModel,
            CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(
                    p => p.Id == viewModel.ProductId,
                    cancellationToken);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.ProductName = product.Name;

            if (viewModel.ImageFile != null)
            {
                var validationError = await ValidateImageAsync(
                    viewModel.ImageFile,
                    cancellationToken);

                if (validationError != null)
                {
                    ModelState.AddModelError(
                        nameof(viewModel.ImageFile),
                        validationError);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            string? physicalPath = null;

            try
            {
                var imageFile = viewModel.ImageFile!;

                var extension = Path.GetExtension(imageFile.FileName)
                    .ToLowerInvariant();

                var fileName = $"{Guid.NewGuid():N}{extension}";

                var webRootPath = GetWebRootPath();

                var uploadDirectory = Path.Combine(
                    webRootPath,
                    "images",
                    "products");

                Directory.CreateDirectory(uploadDirectory);

                physicalPath = Path.Combine(
                    uploadDirectory,
                    fileName);

                await using (var fileStream = new FileStream(
                    physicalPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true))
                {
                    await imageFile.CopyToAsync(
                        fileStream,
                        cancellationToken);
                }

                var isFirstImage = !product.Images.Any();
                var shouldBeMain = viewModel.IsMain || isFirstImage;

                if (shouldBeMain)
                {
                    foreach (var existingImage in product.Images)
                    {
                        existingImage.IsMain = false;
                    }
                }

                var productImage = new ProductImage
                {
                    ProductId = product.Id,
                    ImageUrl =
                        $"/{ProductImagesRelativeFolder}/{fileName}",
                    IsMain = shouldBeMain
                };

                _context.ProductImages.Add(productImage);

                await _context.SaveChangesAsync(cancellationToken);

                TempData["ImageSuccess"] = isFirstImage
                    ? "Image uploaded successfully and set as the main image."
                    : "Image uploaded successfully.";

                return RedirectToAction(
                    nameof(Index),
                    new { productId = product.Id });
            }
            catch (OperationCanceledException)
            {
                TryDeletePhysicalFile(physicalPath);
                throw;
            }
            catch (Exception ex)
            {
                TryDeletePhysicalFile(physicalPath);

                _logger.LogError(
                    ex,
                    "Failed to upload an image for product {ProductId}.",
                    product.Id);

                ModelState.AddModelError(
                    nameof(viewModel.ImageFile),
                    "The image could not be uploaded. Please try again.");

                return View(viewModel);
            }
        }

        // SET MAIN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetMain(
            int id,
            CancellationToken cancellationToken)
        {
            var image = await _context.ProductImages
                .FirstOrDefaultAsync(
                    i => i.Id == id,
                    cancellationToken);

            if (image == null)
            {
                return NotFound();
            }

            var productId = image.ProductId;

            if (image.IsMain)
            {
                return RedirectToAction(
                    nameof(Index),
                    new { productId });
            }

            var productImages = await _context.ProductImages
                .Where(i => i.ProductId == productId)
                .ToListAsync(cancellationToken);

            foreach (var productImage in productImages)
            {
                productImage.IsMain = productImage.Id == id;
            }

            await _context.SaveChangesAsync(cancellationToken);

            TempData["ImageSuccess"] =
                "Main image updated successfully.";

            return RedirectToAction(
                nameof(Index),
                new { productId });
        }

        // DELETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            var image = await _context.ProductImages
                .FirstOrDefaultAsync(
                    i => i.Id == id,
                    cancellationToken);

            if (image == null)
            {
                return NotFound();
            }

            var productId = image.ProductId;
            var wasMain = image.IsMain;
            var imageUrl = image.ImageUrl;

            _context.ProductImages.Remove(image);

            if (wasMain)
            {
                var replacementImage =
                    await _context.ProductImages
                        .Where(i =>
                            i.ProductId == productId &&
                            i.Id != id)
                        .OrderBy(i => i.Id)
                        .FirstOrDefaultAsync(cancellationToken);

                if (replacementImage != null)
                {
                    replacementImage.IsMain = true;
                }
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
                when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Failed to delete product image {ImageId} from the database.",
                    id);

                TempData["ImageError"] =
                    "The image could not be deleted. Please try again.";

                return RedirectToAction(
                    nameof(Index),
                    new { productId });
            }

            TryDeleteLocalImageFile(imageUrl);

            TempData["ImageSuccess"] =
                "Image deleted successfully.";

            return RedirectToAction(
                nameof(Index),
                new { productId });
        }

        private static async Task<string?> ValidateImageAsync(
            IFormFile imageFile,
            CancellationToken cancellationToken)
        {
            if (imageFile.Length <= 0)
            {
                return "Please select a non-empty image.";
            }

            if (imageFile.Length > MaxImageSize)
            {
                return "The image must not exceed 5 MB.";
            }

            var extension = Path.GetExtension(imageFile.FileName)
                .ToLowerInvariant();

            if (!IsAllowedExtension(extension))
            {
                return
                    "Only JPG, JPEG, PNG, and WebP images are allowed.";
            }

            if (!IsAllowedContentType(
                    extension,
                    imageFile.ContentType))
            {
                return
                    "The uploaded file type does not match its extension.";
            }

            try
            {
                if (!await HasValidImageSignatureAsync(
                        imageFile,
                        extension,
                        cancellationToken))
                {
                    return
                        "The selected file is not a valid image.";
                }
            }
            catch (IOException)
            {
                return "The selected image could not be read.";
            }
            catch (UnauthorizedAccessException)
            {
                return "The selected image could not be accessed.";
            }
            catch (InvalidDataException)
            {
                return "The selected image could not be read.";
            }

            return null;
        }

        private static bool IsAllowedExtension(
            string extension)
        {
            return extension is
                ".jpg" or
                ".jpeg" or
                ".png" or
                ".webp";
        }

        private static bool IsAllowedContentType(
            string extension,
            string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return false;
            }

            return extension switch
            {
                ".jpg" or ".jpeg" =>
                    contentType.Equals(
                        "image/jpeg",
                        StringComparison.OrdinalIgnoreCase) ||
                    contentType.Equals(
                        "image/pjpeg",
                        StringComparison.OrdinalIgnoreCase),

                ".png" =>
                    contentType.Equals(
                        "image/png",
                        StringComparison.OrdinalIgnoreCase),

                ".webp" =>
                    contentType.Equals(
                        "image/webp",
                        StringComparison.OrdinalIgnoreCase),

                _ => false
            };
        }

        private static async Task<bool>
            HasValidImageSignatureAsync(
                IFormFile imageFile,
                string extension,
                CancellationToken cancellationToken)
        {
            var header = new byte[12];

            await using var stream =
                imageFile.OpenReadStream();

            var totalBytesRead = 0;

            while (totalBytesRead < header.Length)
            {
                var bytesRead = await stream.ReadAsync(
                    header.AsMemory(
                        totalBytesRead,
                        header.Length - totalBytesRead),
                    cancellationToken);

                if (bytesRead == 0)
                {
                    break;
                }

                totalBytesRead += bytesRead;
            }

            return extension switch
            {
                ".jpg" or ".jpeg" =>
                    totalBytesRead >= 3 &&
                    header[0] == 0xFF &&
                    header[1] == 0xD8 &&
                    header[2] == 0xFF,

                ".png" =>
                    totalBytesRead >= 8 &&
                    header[0] == 0x89 &&
                    header[1] == 0x50 &&
                    header[2] == 0x4E &&
                    header[3] == 0x47 &&
                    header[4] == 0x0D &&
                    header[5] == 0x0A &&
                    header[6] == 0x1A &&
                    header[7] == 0x0A,

                ".webp" =>
                    totalBytesRead >= 12 &&
                    header[0] == 0x52 &&
                    header[1] == 0x49 &&
                    header[2] == 0x46 &&
                    header[3] == 0x46 &&
                    header[8] == 0x57 &&
                    header[9] == 0x45 &&
                    header[10] == 0x42 &&
                    header[11] == 0x50,

                _ => false
            };
        }

        private string GetWebRootPath()
        {
            return _environment.WebRootPath
                   ?? Path.Combine(
                       _environment.ContentRootPath,
                       "wwwroot");
        }

        private void TryDeleteLocalImageFile(
            string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            const string localPrefix =
                "/images/products/";

            if (!imageUrl.StartsWith(
                    localPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var fileName = Path.GetFileName(imageUrl);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            var physicalPath = Path.Combine(
                GetWebRootPath(),
                "images",
                "products",
                fileName);

            TryDeletePhysicalFile(physicalPath);
        }

        private void TryDeletePhysicalFile(
            string? physicalPath)
        {
            if (string.IsNullOrWhiteSpace(physicalPath))
            {
                return;
            }

            try
            {
                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }
            }
            catch (Exception ex)
                when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(
                    ex,
                    "Could not delete local product image file {PhysicalPath}.",
                    physicalPath);
            }
        }
    }
}