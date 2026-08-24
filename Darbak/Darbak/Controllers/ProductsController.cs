using Darbak.Data;
using Darbak.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Darbak.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProductsController> _logger;

        private const long MaxImageSize = 5 * 1024 * 1024;
        private const string ProductImagesRelativeFolder = "images/products";

        public ProductsController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<ProductsController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // ==========================================
        // INDEX + ADMIN FILTERING
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            int? categoryId,
            bool? isActive,
            string? stockStatus)
        {
            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .AsQueryable();

            // Product name - partial search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(p =>
                    p.Name.Contains(search));
            }

            // Category
            if (categoryId.HasValue)
            {
                query = query.Where(p =>
                    p.CategoryId == categoryId.Value);
            }

            // Active / Inactive
            if (isActive.HasValue)
            {
                query = query.Where(p =>
                    p.IsActive == isActive.Value);
            }

            // Stock status
            if (!string.IsNullOrWhiteSpace(stockStatus))
            {
                stockStatus =
                    stockStatus.Trim().ToLowerInvariant();

                switch (stockStatus)
                {
                    case "in_stock":
                        query = query.Where(p =>
                            p.StockQuantity > 0);
                        break;

                    case "out_of_stock":
                        query = query.Where(p =>
                            p.StockQuantity == 0);
                        break;

                    default:
                        stockStatus = null;
                        break;
                }
            }

            var categories =
                await _context.Categories
                    .AsNoTracking()
                    .OrderBy(c => c.Name)
                    .ToListAsync();

            var products =
                await query
                    .OrderByDescending(p =>
                        p.CreatedAt)
                    .ToListAsync();

            ViewBag.Search =
                search;

            ViewBag.CategoryId =
                categoryId;

            ViewBag.IsActive =
                isActive?.ToString()
                    .ToLowerInvariant();

            ViewBag.StockStatus =
                stockStatus;

            ViewBag.Categories =
                categories;

            return View(products);
        }

        // CREATE GET
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCategories();

            return View();
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(
                "Name,Description,Price,StockQuantity,IsActive,CategoryId"
            )]
            Product product,
            List<IFormFile>? imageFiles,
            CancellationToken cancellationToken)
        {
            ModelState.Remove(nameof(Product.Category));

            var categoryExists = await _context.Categories
                .AnyAsync(
                    c => c.Id == product.CategoryId,
                    cancellationToken);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    nameof(Product.CategoryId),
                    "The selected category does not exist."
                );
            }

            var uploadedImages = imageFiles?
                .Where(file => file != null)
                .ToList()
                ?? new List<IFormFile>();

            foreach (var imageFile in uploadedImages)
            {
                var validationError = await ValidateImageAsync(
                    imageFile,
                    cancellationToken);

                if (validationError != null)
                {
                    ModelState.AddModelError(
                        "imageFiles",
                        $"{Path.GetFileName(imageFile.FileName)}: {validationError}");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadCategories(product.CategoryId);

                return View(product);
            }

            product.Name = product.Name.Trim();

            product.Description =
                string.IsNullOrWhiteSpace(product.Description)
                    ? null
                    : product.Description.Trim();

            product.CreatedAt = DateTime.UtcNow;

            var savedPhysicalPaths = new List<string>();

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                _context.Products.Add(product);

                await _context.SaveChangesAsync(cancellationToken);

                if (uploadedImages.Count > 0)
                {
                    var webRootPath = GetWebRootPath();

                    var uploadDirectory = Path.Combine(
                        webRootPath,
                        "images",
                        "products");

                    Directory.CreateDirectory(uploadDirectory);

                    for (var index = 0;
                         index < uploadedImages.Count;
                         index++)
                    {
                        var imageFile = uploadedImages[index];

                        var extension =
                            Path.GetExtension(imageFile.FileName)
                                .ToLowerInvariant();

                        var fileName =
                            $"{Guid.NewGuid():N}{extension}";

                        var physicalPath = Path.Combine(
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

                        savedPhysicalPaths.Add(physicalPath);

                        _context.ProductImages.Add(
                            new ProductImage
                            {
                                ProductId = product.Id,
                                ImageUrl =
                                    $"/{ProductImagesRelativeFolder}/{fileName}",
                                IsMain = index == 0
                            });
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                TempData["ProductSuccess"] =
                    uploadedImages.Count > 0
                        ? "Product created successfully with its images."
                        : "Product created successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (OperationCanceledException)
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);

                DeleteSavedFiles(savedPhysicalPaths);

                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);

                DeleteSavedFiles(savedPhysicalPaths);

                _logger.LogError(
                    ex,
                    "Failed to create product with uploaded images.");

                ModelState.AddModelError(
                    string.Empty,
                    "The product could not be created. Please try again.");

                await LoadCategories(product.CategoryId);

                return View(product);
            }
        }


        // EDIT GET
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product =
                await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            await LoadCategories(product.CategoryId);

            return View(product);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind(
                "Id,Name,Description,Price,StockQuantity,IsActive,CategoryId"
            )]
            Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(Product.Category));

            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == product.CategoryId);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    nameof(Product.CategoryId),
                    "The selected category does not exist."
                );
            }

            if (!ModelState.IsValid)
            {
                await LoadCategories(product.CategoryId);

                return View(product);
            }

            var existingProduct =
                await _context.Products.FindAsync(id);

            if (existingProduct == null)
            {
                return NotFound();
            }

            existingProduct.Name =
                product.Name.Trim();

            existingProduct.Description =
                string.IsNullOrWhiteSpace(product.Description)
                    ? null
                    : product.Description.Trim();

            existingProduct.Price =
                product.Price;

            existingProduct.StockQuantity =
                product.StockQuantity;

            existingProduct.IsActive =
                product.IsActive;

            existingProduct.CategoryId =
                product.CategoryId;

            await _context.SaveChangesAsync();

            TempData["ProductSuccess"] =
                "Product updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // DELETE GET
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.OrderItems)
                .FirstOrDefaultAsync(
                    p => p.Id == id
                );

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // DELETE POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var product = await _context.Products
                .Include(p => p.OrderItems)
                .FirstOrDefaultAsync(
                    p => p.Id == id
                );

            if (product == null)
            {
                return NotFound();
            }

            if (product.OrderItems.Any())
            {
                TempData["ProductError"] =
                    "This product cannot be deleted because it exists in previous orders. You can deactivate it instead.";

                return RedirectToAction(nameof(Index));
            }

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            TempData["ProductSuccess"] =
                "Product deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Reviews
                    .Where(r =>
                        r.Status == ApprovalStatus.Approved))
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Normal users and guests must not
            // access inactive products directly.
            if (!product.IsActive &&
                !User.IsInRole("Admin"))
            {
                return NotFound();
            }

            return View(product);
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
                    return "The selected file is not a valid image.";
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

        private static void DeleteSavedFiles(
            IEnumerable<string> physicalPaths)
        {
            foreach (var physicalPath in physicalPaths)
            {
                try
                {
                    if (System.IO.File.Exists(physicalPath))
                    {
                        System.IO.File.Delete(physicalPath);
                    }
                }
                catch
                {
                    // Best-effort cleanup. The original failure is logged
                    // by the caller and should not be masked here.
                }
            }
        }

        private async Task LoadCategories(
            int? selectedCategoryId = null)
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.CategoryId = new SelectList(
                categories,
                "Id",
                "Name",
                selectedCategoryId
            );
        }
    }
}