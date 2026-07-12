using System.Diagnostics;
using hoangstore.Data;
using hoangstore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hoangstore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? searchTerm,
            int? categoryId)
        {
            searchTerm = searchTerm?.Trim();

            var productQuery = _db.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .Include(product => product.ProductVariants
                    .Where(variant => !variant.IsDeleted))
                .Where(product =>
                    !product.IsDeleted &&
                    product.Category != null &&
                    !product.Category.IsDeleted);

            // Tìm kiếm theo tên sản phẩm.
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                productQuery = productQuery.Where(product =>
                    EF.Functions.Like(
                        product.Product_Name,
                        $"%{searchTerm}%"));
            }

            // Lọc sản phẩm theo danh mục.
            if (categoryId.HasValue &&
                categoryId.Value > 0)
            {
                productQuery = productQuery.Where(product =>
                    product.CategoryId == categoryId.Value);
            }

            var products = await productQuery
                .OrderByDescending(product => product.IsFeatured)
                .ThenByDescending(product => product.CreatedDate)
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedCategoryId = categoryId;

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> GetQuickView(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                return NotFound();
            }

            var product = await _db.Products
                .AsNoTracking()
                .Include(item => item.Category)
                .Include(item => item.ProductVariants
                    .Where(variant => !variant.IsDeleted))
                .FirstOrDefaultAsync(item =>
                    item.ProductId == id.Value &&
                    !item.IsDeleted &&
                    item.Category != null &&
                    !item.Category.IsDeleted);

            if (product == null)
            {
                return NotFound();
            }

            return PartialView(
                "_QuickViewPartial",
                product);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id ??
                    HttpContext.TraceIdentifier
            });
        }
    }
}