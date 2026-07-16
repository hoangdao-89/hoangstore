using hoangstore.Areas.Admin.Documents;
using hoangstore.Areas.Admin.ViewModels;
using hoangstore.Data;
using hoangstore.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace hoangstore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var weekStart = today.AddDays(-6);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var nextMonth = monthStart.AddMonths(1);
            var lastMonthStart = monthStart.AddMonths(-1);
            var yearStart = new DateTime(today.Year, 1, 1);
            var deliveredOrders = _db.Orders.AsNoTracking().Where(x => x.Status == OrderStatus.Delivered);

            var thisMonthRevenue = await deliveredOrders.Where(x => x.OrderDate >= monthStart && x.OrderDate < nextMonth).SumAsync(x => (decimal?)x.TotalPrice) ?? 0;
            var lastMonthRevenue = await deliveredOrders.Where(x => x.OrderDate >= lastMonthStart && x.OrderDate < monthStart).SumAsync(x => (decimal?)x.TotalPrice) ?? 0;
            var customerRoleId = await _db.Roles.AsNoTracking().Where(x => x.Name == "Khách hàng").Select(x => x.Id).FirstOrDefaultAsync();

            var weekData = await deliveredOrders.Where(x => x.OrderDate >= weekStart).GroupBy(x => x.OrderDate.Date).Select(x => new { Date = x.Key, Revenue = x.Sum(y => y.TotalPrice) }).ToListAsync();
            var monthData = await deliveredOrders.Where(x => x.OrderDate >= monthStart && x.OrderDate < nextMonth).GroupBy(x => x.OrderDate.Date).Select(x => new { Date = x.Key, Revenue = x.Sum(y => y.TotalPrice) }).ToListAsync();
            var yearData = await deliveredOrders.Where(x => x.OrderDate >= yearStart).GroupBy(x => x.OrderDate.Month).Select(x => new { Month = x.Key, Revenue = x.Sum(y => y.TotalPrice) }).ToListAsync();

            var model = new DashboardViewModel
            {
                TotalOrders = await _db.Orders.CountAsync(),
                PendingOrders = await _db.Orders.CountAsync(x => x.Status == OrderStatus.Pending),
                ShippingOrders = await _db.Orders.CountAsync(x => x.Status == OrderStatus.Shipping),
                DeliveredOrders = await _db.Orders.CountAsync(x => x.Status == OrderStatus.Delivered),
                CancelledOrders = await _db.Orders.CountAsync(x => x.Status == OrderStatus.Cancelled),
                TotalRevenue = await deliveredOrders.SumAsync(x => (decimal?)x.TotalPrice) ?? 0,
                ThisMonthRevenue = thisMonthRevenue,
                LastMonthRevenue = lastMonthRevenue,
                RevenueGrowthPercent = lastMonthRevenue == 0 ? (thisMonthRevenue > 0 ? 100 : 0) : Math.Round((thisMonthRevenue - lastMonthRevenue) / lastMonthRevenue * 100, 1),
                TotalCustomers = string.IsNullOrWhiteSpace(customerRoleId) ? 0 : await _db.UserRoles.CountAsync(x => x.RoleId == customerRoleId),
                TotalProducts = await _db.Products.CountAsync(x => !x.IsDeleted),
                LowStockCount = await _db.ProductVariants.CountAsync(x => !x.IsDeleted && x.Quantity <= 5 && x.Product != null && !x.Product.IsDeleted),
                RecentOrders = await _db.Orders.AsNoTracking().OrderByDescending(x => x.OrderDate).Take(5).ToListAsync()
            };

            for (var date = weekStart; date <= today; date = date.AddDays(1))
            {
                model.WeekLabels.Add(date.ToString("dd/MM"));
                model.WeekRevenue.Add(weekData.Where(x => x.Date == date).Sum(x => x.Revenue));
            }

            for (var date = monthStart; date <= today; date = date.AddDays(1))
            {
                model.MonthLabels.Add(date.ToString("dd/MM"));
                model.MonthRevenue.Add(monthData.Where(x => x.Date == date).Sum(x => x.Revenue));
            }

            for (var month = 1; month <= 12; month++)
            {
                model.YearLabels.Add($"Tháng {month}");
                model.YearRevenue.Add(yearData.Where(x => x.Month == month).Sum(x => x.Revenue));
            }

            model.TopSellingProducts = await _db.OrderDetails.AsNoTracking()
                .Where(x => x.Order != null && x.Order.Status == OrderStatus.Delivered && x.ProductVariant != null && x.ProductVariant.Product != null)
                .GroupBy(x => new { x.ProductVariant!.Product!.ProductId, x.ProductVariant.Product.Product_Name })
                .Select(x => new TopSellingProductViewModel
                {
                    ProductId = x.Key.ProductId,
                    ProductName = x.Key.Product_Name,
                    QuantitySold = x.Sum(y => y.Quantity),
                    Revenue = x.Sum(y => y.Price * y.Quantity)
                })
                .OrderByDescending(x => x.QuantitySold).Take(5).ToListAsync();

            model.CategoryRevenue = await _db.OrderDetails.AsNoTracking()
                .Where(x => x.Order != null && x.Order.Status == OrderStatus.Delivered && x.ProductVariant != null && x.ProductVariant.Product != null && x.ProductVariant.Product.Category != null)
                .GroupBy(x => x.ProductVariant!.Product!.Category!.Category_Name)
                .Select(x => new CategoryRevenueViewModel
                {
                    CategoryName = x.Key,
                    QuantitySold = x.Sum(y => y.Quantity),
                    Revenue = x.Sum(y => y.Price * y.Quantity)
                })
                .OrderByDescending(x => x.Revenue).Take(5).ToListAsync();

            model.PaymentStatistics = await deliveredOrders
                .GroupBy(x => x.PaymentMethod)
                .Select(x => new PaymentStatisticViewModel
                {
                    PaymentMethod = x.Key,
                    OrderCount = x.Count(),
                    Revenue = x.Sum(y => y.TotalPrice)
                })
                .OrderByDescending(x => x.Revenue).ToListAsync();

            model.LowStockItems = await _db.ProductVariants.AsNoTracking()
                .Where(x => !x.IsDeleted && x.Quantity <= 5 && x.Product != null && !x.Product.IsDeleted)
                .OrderBy(x => x.Quantity).Take(5)
                .Select(x => new LowStockItemViewModel
                {
                    VariantId = x.Id,
                    ProductName = x.Product!.Product_Name,
                    Size = x.Size,
                    Color = x.Color,
                    Quantity = x.Quantity
                }).ToListAsync();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportRevenuePdf(DateTime? fromDate, DateTime? toDate)
        {
            var start = (fromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
            var end = (toDate ?? DateTime.Today).Date;
            if (start > end) (start, end) = (end, start);

            var orders = _db.Orders.AsNoTracking().Where(x => x.Status == OrderStatus.Delivered && x.OrderDate >= start && x.OrderDate < end.AddDays(1));
            var orderCount = await orders.CountAsync();
            var totalRevenue = await orders.SumAsync(x => (decimal?)x.TotalPrice) ?? 0;

            var report = new RevenueReportViewModel
            {
                FromDate = start,
                ToDate = end,
                DeliveredOrderCount = orderCount,
                TotalRevenue = totalRevenue,
                AverageOrderValue = orderCount == 0 ? 0 : totalRevenue / orderCount,
                RevenueByDate = await orders.GroupBy(x => x.OrderDate.Date)
                    .Select(x => new RevenueByDateViewModel { Date = x.Key, OrderCount = x.Count(), Revenue = x.Sum(y => y.TotalPrice) })
                    .OrderBy(x => x.Date).ToListAsync(),
                TopSellingProducts = await _db.OrderDetails.AsNoTracking()
                    .Where(x => x.Order != null && x.Order.Status == OrderStatus.Delivered && x.Order.OrderDate >= start && x.Order.OrderDate < end.AddDays(1) && x.ProductVariant != null && x.ProductVariant.Product != null)
                    .GroupBy(x => new { x.ProductVariant!.Product!.ProductId, x.ProductVariant.Product.Product_Name })
                    .Select(x => new TopSellingProductViewModel
                    {
                        ProductId = x.Key.ProductId,
                        ProductName = x.Key.Product_Name,
                        QuantitySold = x.Sum(y => y.Quantity),
                        Revenue = x.Sum(y => y.Price * y.Quantity)
                    })
                    .OrderByDescending(x => x.QuantitySold).Take(10).ToListAsync()
            };

            var pdf = new RevenueReportDocument(report).GeneratePdf();
            return File(pdf, "application/pdf", $"bao-cao-doanh-thu-{start:yyyyMMdd}-{end:yyyyMMdd}.pdf");
        }
    }
}
