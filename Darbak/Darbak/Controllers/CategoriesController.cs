using Darbak.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Darbak.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<CategoriesController> _logger;

        private const long MaxImageSize = 5 * 1024 * 1024;
        private const string CategoryImagesRelativeFolder = "images/categories";

        public CategoriesController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<CategoriesController> logger)
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
            string? search)
        {
            var query =
                _context.Categories
                    .AsNoTracking()
                    .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(c =>
                    c.Name.Contains(search));
            }

            var categories =
                await query
                    .OrderBy(c => c.Name)
                    .ToListAsync();

            ViewBag.Search = search;

            return View(categories);
        }

        // ==========================================
        // CREATE GET
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // ==========================================
        // CREATE POST
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Description")] Category category,
            IFormFile? imageFile,
            CancellationToken cancellationToken)
        {
            NormalizeCategory(category);

            await ValidateCategoryNameAsync(
                category,
                excludedId: null,
                cancellationToken);

            if (imageFile != null)
            {
                var imageError = await ValidateImageAsync(
                    imageFile,
                    cancellationToken);

                if (imageError != null)
                {
                    ModelState.AddModelError(
                        "imageFile",
                        imageError);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            string? uploadedPhysicalPath = null;

            try
            {
                if (imageFile != null)
                {
                    var uploadResult = await SaveImageAsync(
                        imageFile,
                        cancellationToken);

                    category.ImageUrl = uploadResult.ImageUrl;
                    uploadedPhysicalPath = uploadResult.PhysicalPath;
                }

                _context.Categories.Add(category);

                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                TryDeletePhysicalFile(uploadedPhysicalPath);
                throw;
            }
            catch (DbUpdateException ex)
            {
                TryDeletePhysicalFile(uploadedPhysicalPath);

                _logger.LogWarning(
                    ex,
                    "Failed to create category {CategoryName}.",
                    category.Name);

                ModelState.AddModelError(
                    nameof(Category.Name),
                    "The category could not be saved. A category with this name may already exist.");

                category.ImageUrl = null;
                return View(category);
            }
            catch (Exception ex)
            {
                TryDeletePhysicalFile(uploadedPhysicalPath);

                _logger.LogError(
                    ex,
                    "Failed to create category {CategoryName}.",
                    category.Name);

                ModelState.AddModelError(
                    string.Empty,
                    "The category could not be saved. Please try again.");

                category.ImageUrl = null;
                return View(category);
            }

            TempData["CategorySuccess"] =
                "Category created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // EDIT GET
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category =
                await _context.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c =>
                        c.Id == id.Value);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // ==========================================
        // EDIT POST
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Name,Description")] Category category,
            IFormFile? imageFile,
            bool removeImage,
            CancellationToken cancellationToken)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            var existingCategory =
                await _context.Categories
                    .FirstOrDefaultAsync(
                        c => c.Id == id,
                        cancellationToken);

            if (existingCategory == null)
            {
                return NotFound();
            }

            var oldImageUrl = existingCategory.ImageUrl;
            category.ImageUrl = oldImageUrl;

            NormalizeCategory(category);

            await ValidateCategoryNameAsync(
                category,
                excludedId: category.Id,
                cancellationToken);

            if (imageFile != null)
            {
                var imageError = await ValidateImageAsync(
                    imageFile,
                    cancellationToken);

                if (imageError != null)
                {
                    ModelState.AddModelError(
                        "imageFile",
                        imageError);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            string? newPhysicalPath = null;
            string? newImageUrl = null;

            try
            {
                if (imageFile != null)
                {
                    var uploadResult = await SaveImageAsync(
                        imageFile,
                        cancellationToken);

                    newImageUrl = uploadResult.ImageUrl;
                    newPhysicalPath = uploadResult.PhysicalPath;
                }

                existingCategory.Name = category.Name;
                existingCategory.Description = category.Description;

                if (newImageUrl != null)
                {
                    existingCategory.ImageUrl = newImageUrl;
                }
                else if (removeImage)
                {
                    existingCategory.ImageUrl = null;
                }

                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                TryDeletePhysicalFile(newPhysicalPath);
                throw;
            }
            catch (DbUpdateException ex)
            {
                TryDeletePhysicalFile(newPhysicalPath);

                existingCategory.ImageUrl = oldImageUrl;
                category.ImageUrl = oldImageUrl;

                _logger.LogWarning(
                    ex,
                    "Failed to update category {CategoryId}.",
                    id);

                ModelState.AddModelError(
                    nameof(Category.Name),
                    "The category could not be updated. A category with this name may already exist.");

                return View(category);
            }
            catch (Exception ex)
            {
                TryDeletePhysicalFile(newPhysicalPath);

                existingCategory.ImageUrl = oldImageUrl;
                category.ImageUrl = oldImageUrl;

                _logger.LogError(
                    ex,
                    "Failed to update category {CategoryId}.",
                    id);

                ModelState.AddModelError(
                    string.Empty,
                    "The category could not be updated. Please try again.");

                return View(category);
            }

            if (newImageUrl != null || removeImage)
            {
                TryDeleteLocalImageFile(oldImageUrl);
            }

            TempData["CategorySuccess"] =
                "Category updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // DELETE GET
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Delete(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category =
                await _context.Categories
                    .AsNoTracking()
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c =>
                        c.Id == id.Value);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // ==========================================
        // DELETE POST
        // ==========================================
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id,
            CancellationToken cancellationToken)
        {
            var category =
                await _context.Categories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(
                        c => c.Id == id,
                        cancellationToken);

            if (category == null)
            {
                return NotFound();
            }

            if (category.Products.Any())
            {
                TempData["CategoryError"] =
                    "This category cannot be deleted because it contains products.";

                return RedirectToAction(nameof(Index));
            }

            var imageUrl = category.ImageUrl;

            try
            {
                _context.Categories.Remove(category);

                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to delete category {CategoryId}.",
                    id);

                TempData["CategoryError"] =
                    "This category could not be deleted because it is being used by other data.";

                return RedirectToAction(nameof(Index));
            }

            TryDeleteLocalImageFile(imageUrl);

            TempData["CategorySuccess"] =
                "Category deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private static void NormalizeCategory(
            Category category)
        {
            category.Name =
                category.Name?.Trim()
                ?? string.Empty;

            category.Description =
                string.IsNullOrWhiteSpace(category.Description)
                    ? null
                    : category.Description.Trim();
        }

        private async Task ValidateCategoryNameAsync(
            Category category,
            int? excludedId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                ModelState.AddModelError(
                    nameof(Category.Name),
                    "Category name is required.");

                return;
            }

            var query = _context.Categories
                .AsNoTracking()
                .Where(c => c.Name == category.Name);

            if (excludedId.HasValue)
            {
                query = query.Where(c => c.Id != excludedId.Value);
            }

            if (await query.AnyAsync(cancellationToken))
            {
                ModelState.AddModelError(
                    nameof(Category.Name),
                    "A category with this name already exists.");
            }
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
                return "Only JPG, JPEG, PNG, and WebP images are allowed.";
            }

            if (!IsAllowedContentType(extension, imageFile.ContentType))
            {
                return "The uploaded file type does not match its extension.";
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

        private static async Task<bool> HasValidImageSignatureAsync(
            IFormFile imageFile,
            string extension,
            CancellationToken cancellationToken)
        {
            var header = new byte[12];

            await using var stream = imageFile.OpenReadStream();

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

        private async Task<(string ImageUrl, string PhysicalPath)> SaveImageAsync(
            IFormFile imageFile,
            CancellationToken cancellationToken)
        {
            var extension = Path.GetExtension(imageFile.FileName)
                .ToLowerInvariant();

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var uploadDirectory = Path.Combine(
                GetWebRootPath(),
                "images",
                "categories");

            Directory.CreateDirectory(uploadDirectory);

            var physicalPath = Path.Combine(
                uploadDirectory,
                fileName);

            await using var fileStream = new FileStream(
                physicalPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await imageFile.CopyToAsync(
                fileStream,
                cancellationToken);

            return (
                $"/{CategoryImagesRelativeFolder}/{fileName}",
                physicalPath);
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

            const string localPrefix = "/images/categories/";

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
                "categories",
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
                    "Could not delete local category image file {PhysicalPath}.",
                    physicalPath);
            }
        }
    }
}
