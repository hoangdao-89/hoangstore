using hoangstore.Data;
using hoangstore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hoangstore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ApplicationDbContext db, ILogger<CategoryController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm)
        {
            searchTerm = searchTerm?.Trim();

            var query = _db.Categories
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(x =>
                    EF.Functions.Like(x.Category_Name, $"%{searchTerm}%"));
            }

            var categories = await query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Category_Name)
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            return View(categories);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Category
            {
                DisplayOrder = 1,
                IsActive = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            category.Category_Name = category.Category_Name?.Trim() ?? string.Empty;
            category.Description = category.Description?.Trim();

            if (!string.IsNullOrWhiteSpace(category.Category_Name))
            {
                var nameExists = await _db.Categories.AnyAsync(x =>
                    !x.IsDeleted && x.Category_Name == category.Category_Name);

                if (nameExists)
                {
                    ModelState.AddModelError(
                        nameof(Category.Category_Name),
                        "Tên danh mục đã tồn tại.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            category.IsDeleted = false;

            try
            {
                _db.Categories.Add(category);
                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm danh mục thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm danh mục {CategoryName}.", category.Category_Name);

                ModelState.AddModelError(
                    string.Empty,
                    "Không thể thêm danh mục lúc này.");

                return View(category);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!id.HasValue || id <= 0)
            {
                return NotFound();
            }

            var category = await _db.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CategoryId == id && !x.IsDeleted);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id <= 0 || id != category.CategoryId)
            {
                return NotFound();
            }

            category.Category_Name = category.Category_Name?.Trim() ?? string.Empty;
            category.Description = category.Description?.Trim();

            if (!string.IsNullOrWhiteSpace(category.Category_Name))
            {
                var nameExists = await _db.Categories.AnyAsync(x =>
                    !x.IsDeleted &&
                    x.CategoryId != id &&
                    x.Category_Name == category.Category_Name);

                if (nameExists)
                {
                    ModelState.AddModelError(
                        nameof(Category.Category_Name),
                        "Tên danh mục đã tồn tại.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            var categoryInDb = await _db.Categories
                .FirstOrDefaultAsync(x => x.CategoryId == id && !x.IsDeleted);

            if (categoryInDb == null)
            {
                return NotFound();
            }

            categoryInDb.Category_Name = category.Category_Name;
            categoryInDb.Description = category.Description;
            categoryInDb.DisplayOrder = category.DisplayOrder;
            categoryInDb.IsActive = category.IsActive;

            try
            {
                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật danh mục thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật danh mục {CategoryId}.", id);

                ModelState.AddModelError(
                    string.Empty,
                    "Không thể cập nhật danh mục lúc này.");

                return View(category);
            }
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Mã danh mục không hợp lệ."
                });
            }

            var category = await _db.Categories
                .FirstOrDefaultAsync(x => x.CategoryId == id && !x.IsDeleted);

            if (category == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Danh mục không tồn tại hoặc đã bị xóa."
                });
            }

            var hasProducts = await _db.Products
                .AnyAsync(x => x.CategoryId == id && !x.IsDeleted);

            if (hasProducts)
            {
                return Json(new
                {
                    success = false,
                    message = "Không thể xóa vì danh mục vẫn còn sản phẩm."
                });
            }

            try
            {
                _db.Categories.Remove(category);
                await _db.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Xóa danh mục thành công."
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa danh mục {CategoryId}.", id);

                return Json(new
                {
                    success = false,
                    message = "Không thể xóa danh mục lúc này."
                });
            }
        }
    }
}