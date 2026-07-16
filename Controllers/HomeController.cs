using System.Diagnostics;
using hoangstore.Data;
using hoangstore.Models;
using hoangstore.Models.Enums;
using hoangstore.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hoangstore.Controllers
{
    public class HomeController : Controller
    {
        private const int PageSize = 12;
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Index(
            string? searchTerm,
            int? categoryId,
            string? categoryName,
            ProductGender? gender,
            string? collection,
            int page = 1)
        {
            searchTerm = searchTerm?.Trim();
            categoryName = categoryName?.Trim();
            collection = collection?.Trim().ToLowerInvariant();
            page = page < 1 ? 1 : page;

            var baseQuery = _db.Products
                .AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.ProductVariants.Where(v => !v.IsDeleted))
                .Where(x =>
                    !x.IsDeleted &&
                    x.Category != null &&
                    !x.Category.IsDeleted &&
                    x.Category.IsActive);

            var isFiltered =
                !string.IsNullOrWhiteSpace(searchTerm) ||
                categoryId.HasValue ||
                !string.IsNullOrWhiteSpace(categoryName) ||
                gender.HasValue ||
                !string.IsNullOrWhiteSpace(collection);

            var viewModel = new HomeViewModel
            {
                SearchTerm = searchTerm,
                CategoryId = categoryId,
                CategoryName = categoryName,
                Gender = gender,
                Collection = collection,
                CurrentPage = page,
                PageSize = PageSize
            };

            if (!isFiltered)
            {
                viewModel.FeaturedProducts = await baseQuery
                    .Where(x => x.IsFeatured)
                    .OrderByDescending(x => x.CreatedDate)
                    .Take(4)
                    .ToListAsync();

                var featuredIds = viewModel.FeaturedProducts
                    .Select(x => x.ProductId)
                    .ToList();

                var newDate = DateTime.Now.AddDays(-30);

                viewModel.NewProducts = await baseQuery
                    .Where(x =>
                        x.CreatedDate >= newDate &&
                        !featuredIds.Contains(x.ProductId))
                    .OrderByDescending(x => x.CreatedDate)
                    .Take(4)
                    .ToListAsync();

                var allProductsQuery = baseQuery
                    .OrderByDescending(x => x.CreatedDate);

                viewModel.TotalProducts = await allProductsQuery.CountAsync();
                viewModel.Products = await allProductsQuery
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();
            }
            else
            {
                var productQuery = baseQuery;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    productQuery = productQuery.Where(x =>
                        EF.Functions.Like(
                            x.Product_Name,
                            $"%{searchTerm}%"));
                }

                if (categoryId.HasValue && categoryId > 0)
                {
                    productQuery = productQuery.Where(x =>
                        x.CategoryId == categoryId);
                }

                if (!string.IsNullOrWhiteSpace(categoryName))
                {
                    productQuery = productQuery.Where(x =>
                        x.Category!.Category_Name == categoryName);
                }

                if (gender.HasValue)
                {
                    productQuery = productQuery.Where(x =>
                        x.Gender == gender ||
                        x.Gender == ProductGender.Unisex);
                }

                productQuery = collection switch
                {
                    "new" => productQuery.Where(x =>
                        x.CreatedDate >= DateTime.Now.AddDays(-30)),

                    "featured" => productQuery.Where(x =>
                        x.IsFeatured),

                    "sale" => productQuery.Where(x =>
                        x.ProductVariants.Any(v =>
                            !v.IsDeleted &&
                            v.DiscountPercent > 0)),

                    _ => productQuery
                };

                productQuery = productQuery
                    .OrderByDescending(x => x.IsFeatured)
                    .ThenByDescending(x => x.CreatedDate);

                viewModel.TotalProducts = await productQuery.CountAsync();
                viewModel.Products = await productQuery
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();
            }

            if (viewModel.TotalPages > 0 && page > viewModel.TotalPages)
            {
                return RedirectToAction(nameof(Index), new
                {
                    searchTerm,
                    categoryId,
                    categoryName,
                    gender,
                    collection,
                    page = viewModel.TotalPages
                });
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetQuickView(int? id)
        {
            if (!id.HasValue || id <= 0) return NotFound();

            var product = await _db.Products
                .AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.ProductVariants.Where(v => !v.IsDeleted))
                .FirstOrDefaultAsync(x =>
                    x.ProductId == id &&
                    !x.IsDeleted &&
                    x.Category != null &&
                    !x.Category.IsDeleted);

            return product == null
                ? NotFound()
                : PartialView("_QuickViewPartial", product);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ??
                            HttpContext.TraceIdentifier
            });
        }
    }
}
