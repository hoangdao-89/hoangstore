using hoangstore.Data;
using hoangstore.Models;
using hoangstore.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace hoangstore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProductController> _logger;

        public ProductController(ApplicationDbContext db, IWebHostEnvironment environment, ILogger<ProductController> logger)
        {
            _db = db;
            _environment = environment;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm, int? categoryId, int page = 1)
        {
            const int pageSize = 10;
            searchTerm = searchTerm?.Trim();
            if (page < 1) page = 1;

            var query = _db.Products.AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.ProductVariants)
                .Where(x => !x.IsDeleted && x.Category != null && !x.Category.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(x => EF.Functions.Like(x.Product_Name, $"%{searchTerm}%"));

            if (categoryId.HasValue && categoryId > 0)
                query = query.Where(x => x.CategoryId == categoryId);

            var totalItems = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            if (page > totalPages) page = totalPages;

            var products = await query.OrderByDescending(x => x.CreatedDate)
                .ThenBy(x => x.Product_Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CategoryId = categoryId;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            await LoadCategories(categoryId);
            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCategories();
            return View(new Product());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            NormalizeProduct(product);
            ModelState.Remove(nameof(Product.Image_Url));
            await ValidateProduct(product);

            if (imageFile == null || imageFile.Length == 0)
                ModelState.AddModelError("imageFile", "Vui lòng chọn ảnh sản phẩm.");
            else
                ValidateImage(imageFile, "imageFile");

            if (!ModelState.IsValid)
            {
                await LoadCategories(product.CategoryId);
                return View(product);
            }

            string? savedImage = null;

            try
            {
                savedImage = await SaveImage(imageFile!, "products");
                product.Image_Url = savedImage;
                product.IsDeleted = false;

                _db.Products.Add(product);
                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm sản phẩm thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                DeleteImage(savedImage);
                _logger.LogError(ex, "Lỗi khi thêm sản phẩm {ProductName}", product.Product_Name);
                ModelState.AddModelError(string.Empty, "Không thể thêm sản phẩm lúc này.");
                await LoadCategories(product.CategoryId);
                return View(product);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!id.HasValue || id <= 0) return NotFound();

            var product = await _db.Products.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProductId == id && !x.IsDeleted);

            if (product == null) return NotFound();

            await LoadCategories(product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? imageFile)
        {
            if (id <= 0 || id != product.ProductId) return NotFound();

            var productInDb = await _db.Products.FirstOrDefaultAsync(x => x.ProductId == id && !x.IsDeleted);
            if (productInDb == null) return NotFound();

            NormalizeProduct(product);
            ModelState.Remove(nameof(Product.Image_Url));
            await ValidateProduct(product, id);

            if (imageFile != null && imageFile.Length > 0)
                ValidateImage(imageFile, "imageFile");

            if (!ModelState.IsValid)
            {
                product.Image_Url = productInDb.Image_Url;
                await LoadCategories(product.CategoryId);
                return View(product);
            }

            string? newImage = null;
            var oldImage = productInDb.Image_Url;

            try
            {
                if (imageFile != null && imageFile.Length > 0)
                    newImage = await SaveImage(imageFile, "products");

                productInDb.Product_Name = product.Product_Name;
                productInDb.Product_Description = product.Product_Description;
                productInDb.CategoryId = product.CategoryId;
                productInDb.IsFeatured = product.IsFeatured;
                productInDb.Gender = product.Gender;

                if (newImage != null) productInDb.Image_Url = newImage;

                await _db.SaveChangesAsync();

                if (newImage != null) DeleteImage(oldImage);

                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                DeleteImage(newImage);
                _logger.LogError(ex, "Lỗi khi cập nhật sản phẩm {ProductId}", id);
                product.Image_Url = oldImage;
                ModelState.AddModelError(string.Empty, "Không thể cập nhật sản phẩm lúc này.");
                await LoadCategories(product.CategoryId);
                return View(product);
            }
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products.FirstOrDefaultAsync(x => x.ProductId == id && !x.IsDeleted);

            if (product == null)
                return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc đã bị xóa." });

            try
            {
                _db.Products.Remove(product);
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa sản phẩm thành công." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa sản phẩm {ProductId}", id);
                return Json(new { success = false, message = "Không thể xóa sản phẩm lúc này." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Variants(int id)
        {
            var product = await _db.Products.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProductId == id && !x.IsDeleted);

            if (product == null) return NotFound();

            var variants = await _db.ProductVariants.AsNoTracking()
                .Where(x => x.ProductId == id && !x.IsDeleted)
                .OrderBy(x => x.Color)
                .ThenBy(x => x.Size)
                .ToListAsync();

            ViewBag.Product = product;
            return View(variants);
        }

        [HttpGet]
        public async Task<IActionResult> CreateVariant(int productId)
        {
            var product = await _db.Products.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProductId == productId && !x.IsDeleted);

            if (product == null) return NotFound();

            ViewBag.Product = product;
            return View(new ProductVariant { ProductId = productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVariant(ProductVariant variant, IFormFile? variantImageFile)
        {
            var product = await _db.Products.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProductId == variant.ProductId && !x.IsDeleted);

            if (product == null) return NotFound();

            NormalizeVariant(variant);
            ClearVariantModelState();
            await ValidateVariant(variant);

            if (variantImageFile != null && variantImageFile.Length > 0)
                ValidateImage(variantImageFile, "variantImageFile");

            if (!ModelState.IsValid)
            {
                ViewBag.Product = product;
                return View(variant);
            }

            string? savedImage = null;

            try
            {
                if (variantImageFile != null && variantImageFile.Length > 0)
                    savedImage = await SaveImage(variantImageFile, "variants");

                variant.Variant_Image_Url = savedImage;
                variant.IsDeleted = false;

                _db.ProductVariants.Add(variant);
                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm biến thể thành công.";
                return RedirectToAction(nameof(Variants), new { id = variant.ProductId });
            }
            catch (Exception ex)
            {
                DeleteImage(savedImage);
                _logger.LogError(ex, "Lỗi khi thêm biến thể cho sản phẩm {ProductId}", variant.ProductId);
                ModelState.AddModelError(string.Empty, "Không thể thêm biến thể lúc này.");
                ViewBag.Product = product;
                return View(variant);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditVariant(int id)
        {
            var variant = await _db.ProductVariants.AsNoTracking()
                .Include(x => x.Product)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted &&
                                          x.Product != null && !x.Product.IsDeleted);

            if (variant == null) return NotFound();

            ViewBag.Product = variant.Product;
            return View(variant);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVariant(int id, ProductVariant variant, IFormFile? variantImageFile)
        {
            if (id != variant.Id) return NotFound();

            var variantInDb = await _db.ProductVariants.Include(x => x.Product)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted &&
                                          x.Product != null && !x.Product.IsDeleted);

            if (variantInDb == null) return NotFound();

            variant.ProductId = variantInDb.ProductId;
            NormalizeVariant(variant);
            ClearVariantModelState();
            await ValidateVariant(variant, id);

            if (variantImageFile != null && variantImageFile.Length > 0)
                ValidateImage(variantImageFile, "variantImageFile");

            if (!ModelState.IsValid)
            {
                variant.Variant_Image_Url = variantInDb.Variant_Image_Url;
                ViewBag.Product = variantInDb.Product;
                return View(variant);
            }

            string? newImage = null;
            var oldImage = variantInDb.Variant_Image_Url;

            try
            {
                if (variantImageFile != null && variantImageFile.Length > 0)
                    newImage = await SaveImage(variantImageFile, "variants");

                variantInDb.Size = variant.Size;
                variantInDb.Color = variant.Color;
                variantInDb.Price = variant.Price;
                variantInDb.DiscountPercent = variant.DiscountPercent;
                variantInDb.Quantity = variant.Quantity;

                if (newImage != null) variantInDb.Variant_Image_Url = newImage;

                await _db.SaveChangesAsync();

                if (newImage != null) DeleteImage(oldImage);

                TempData["SuccessMessage"] = "Cập nhật biến thể thành công.";
                return RedirectToAction(nameof(Variants), new { id = variantInDb.ProductId });
            }
            catch (Exception ex)
            {
                DeleteImage(newImage);
                _logger.LogError(ex, "Lỗi khi cập nhật biến thể {VariantId}", id);
                variant.Variant_Image_Url = oldImage;
                ModelState.AddModelError(string.Empty, "Không thể cập nhật biến thể lúc này.");
                ViewBag.Product = variantInDb.Product;
                return View(variant);
            }
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVariant(int id)
        {
            var variant = await _db.ProductVariants.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (variant == null)
                return Json(new { success = false, message = "Biến thể không tồn tại hoặc đã bị xóa." });

            try
            {
                _db.ProductVariants.Remove(variant);
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa biến thể thành công." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa biến thể {VariantId}", id);
                return Json(new { success = false, message = "Không thể xóa biến thể lúc này." });
            }
        }

        private async Task ValidateProduct(Product product, int? currentId = null)
        {
            var categoryExists = await _db.Categories.AnyAsync(x =>
                x.CategoryId == product.CategoryId && !x.IsDeleted && x.IsActive);

            if (!categoryExists)
                ModelState.AddModelError(nameof(Product.CategoryId), "Danh mục không tồn tại hoặc đã ngừng hoạt động.");

            if (string.IsNullOrWhiteSpace(product.Product_Name)) return;

            var nameExists = await _db.Products.AnyAsync(x =>
                !x.IsDeleted && x.ProductId != currentId && x.Product_Name == product.Product_Name);

            if (nameExists)
                ModelState.AddModelError(nameof(Product.Product_Name), "Tên sản phẩm đã tồn tại.");
        }

        private async Task ValidateVariant(ProductVariant variant, int? currentId = null)
        {
            var duplicate = await _db.ProductVariants.AnyAsync(x =>
                !x.IsDeleted && x.ProductId == variant.ProductId && x.Id != currentId &&
                x.Size == variant.Size && x.Color == variant.Color);

            if (duplicate)
                ModelState.AddModelError(string.Empty, "Sản phẩm đã có biến thể cùng size và màu sắc.");
        }

        private void ClearVariantModelState()
        {
            ModelState.Remove(nameof(ProductVariant.Product));
            ModelState.Remove(nameof(ProductVariant.CreatedBy));
            ModelState.Remove(nameof(ProductVariant.ModifiedBy));
            ModelState.Remove(nameof(ProductVariant.DeletedBy));
            ModelState.Remove(nameof(ProductVariant.CartItems));
            ModelState.Remove(nameof(ProductVariant.OrderDetails));
            ModelState.Remove(nameof(ProductVariant.Variant_Image_Url));
        }

        private void ValidateImage(IFormFile imageFile, string fieldName)
        {
            var extensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

            if (!extensions.Contains(extension))
                ModelState.AddModelError(fieldName, "Chỉ chấp nhận ảnh JPG, JPEG, PNG hoặc WEBP.");

            if (!imageFile.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError(fieldName, "Tệp được chọn không phải hình ảnh.");

            if (imageFile.Length > 5 * 1024 * 1024)
                ModelState.AddModelError(fieldName, "Dung lượng ảnh không được vượt quá 5 MB.");
        }

        private async Task<string> SaveImage(IFormFile imageFile, string folderName)
        {
            var folder = Path.Combine(_environment.WebRootPath, "images", folderName);
            Directory.CreateDirectory(folder);

            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(folder, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await imageFile.CopyToAsync(stream);

            return $"/images/{folderName}/{fileName}";
        }

        private void DeleteImage(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) ||
                !imageUrl.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)) return;

            var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var filePath = Path.Combine(_environment.WebRootPath, relativePath);

            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
        }

        private async Task LoadCategories(int? selectedId = null)
        {
            var categories = await _db.Categories.AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Category_Name)
                .ToListAsync();

            ViewBag.CategoryList = new SelectList(categories, "CategoryId", "Category_Name", selectedId);
        }

        private static void NormalizeProduct(Product product)
        {
            product.Product_Name = product.Product_Name?.Trim() ?? string.Empty;
            product.Product_Description = product.Product_Description?.Trim() ?? string.Empty;
        }

        private static void NormalizeVariant(ProductVariant variant)
        {
            variant.Size = variant.Size?.Trim().ToUpperInvariant() ?? string.Empty;
            variant.Color = variant.Color?.Trim() ?? string.Empty;
        }
    }
}