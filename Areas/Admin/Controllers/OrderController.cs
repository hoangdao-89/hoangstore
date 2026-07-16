using hoangstore.Data;
using hoangstore.Models;
using hoangstore.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace hoangstore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<OrderController> _logger;

        public OrderController(ApplicationDbContext db, ILogger<OrderController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm, OrderStatus? orderStatus, string? paymentMethod, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            const int pageSize = 20;
            searchTerm = searchTerm?.Trim();
            paymentMethod = paymentMethod?.Trim();
            if (page < 1) page = 1;

            var query = _db.Orders.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalizedId = searchTerm.Replace("#HS-", "", StringComparison.OrdinalIgnoreCase).Trim();
                if (int.TryParse(normalizedId, out var orderId)) query = query.Where(x => x.Id == orderId);
                else query = query.Where(x => x.ReceiverName.Contains(searchTerm) || x.ReceiverPhone.Contains(searchTerm));
            }

            if (orderStatus.HasValue) query = query.Where(x => x.Status == orderStatus.Value);
            if (!string.IsNullOrWhiteSpace(paymentMethod)) query = query.Where(x => x.PaymentMethod == paymentMethod);
            if (fromDate.HasValue) query = query.Where(x => x.OrderDate >= fromDate.Value.Date);
            if (toDate.HasValue) query = query.Where(x => x.OrderDate < toDate.Value.Date.AddDays(1));

            var totalItems = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            if (page > totalPages) page = totalPages;

            var orders = await query.OrderByDescending(x => x.OrderDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentFilter = orderStatus;
            ViewBag.PaymentMethod = paymentMethod;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (!id.HasValue || id <= 0) return NotFound();

            var order = await _db.Orders.AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.OrderDetails)
                .ThenInclude(x => x.ProductVariant)
                .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.Id == id.Value);

            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, OrderStatus orderStatus)
        {
            if (!Enum.IsDefined(typeof(OrderStatus), orderStatus))
            {
                TempData["ErrorMessage"] = "Trạng thái không hợp lệ.";
                return RedirectToAction(nameof(Details), new { area = "Admin", id = orderId });
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                var order = await _db.Orders.Include(x => x.OrderDetails).FirstOrDefaultAsync(x => x.Id == orderId);
                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction(nameof(Index), new { area = "Admin" });
                }

                if (!CanChangeStatus(order, orderStatus))
                {
                    TempData["ErrorMessage"] = "Không thể chuyển đơn hàng sang trạng thái đã chọn.";
                    return RedirectToAction(nameof(Details), new { area = "Admin", id = orderId });
                }

                if (orderStatus == OrderStatus.Cancelled) await RestoreStock(order.OrderDetails);
                order.Status = orderStatus;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = $"Cập nhật đơn hàng #HS-{orderId} thành công.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi cập nhật đơn hàng {OrderId}", orderId);
                TempData["ErrorMessage"] = "Không thể cập nhật đơn hàng lúc này.";
            }

            return RedirectToAction(nameof(Details), new { area = "Admin", id = orderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpdateStatus(List<int> orderIds, OrderStatus orderStatus, string? searchTerm, OrderStatus? currentFilter, string? paymentMethod, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            if (orderIds == null || orderIds.Count == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một đơn hàng.";
                return RedirectToIndex(searchTerm, currentFilter, paymentMethod, fromDate, toDate, page);
            }

            if (!Enum.IsDefined(typeof(OrderStatus), orderStatus))
            {
                TempData["ErrorMessage"] = "Trạng thái không hợp lệ.";
                return RedirectToIndex(searchTerm, currentFilter, paymentMethod, fromDate, toDate, page);
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                var orders = await _db.Orders.Include(x => x.OrderDetails).Where(x => orderIds.Contains(x.Id)).ToListAsync();
                var updatedCount = 0;
                var skippedCount = orderIds.Count - orders.Count;

                foreach (var order in orders)
                {
                    if (!CanChangeStatus(order, orderStatus))
                    {
                        skippedCount++;
                        continue;
                    }

                    if (orderStatus == OrderStatus.Cancelled) await RestoreStock(order.OrderDetails);
                    order.Status = orderStatus;
                    updatedCount++;
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                if (updatedCount > 0) TempData["SuccessMessage"] = $"Đã cập nhật {updatedCount} đơn hàng.";
                if (skippedCount > 0) TempData["ErrorMessage"] = $"Có {skippedCount} đơn không thể cập nhật do không đúng quy trình.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi cập nhật hàng loạt đơn hàng");
                TempData["ErrorMessage"] = "Không thể cập nhật hàng loạt lúc này.";
            }

            return RedirectToIndex(searchTerm, currentFilter, paymentMethod, fromDate, toDate, page);
        }

        private static bool CanChangeStatus(Order order, OrderStatus nextStatus)
        {
            if (order.Status == nextStatus || order.Status is OrderStatus.Delivered or OrderStatus.Cancelled) return false;
            var isVnPay = string.Equals(order.PaymentMethod, "VNPAY", StringComparison.OrdinalIgnoreCase);
            if (order.Status == OrderStatus.Pending && isVnPay) return nextStatus == OrderStatus.Cancelled;
            if (order.Status == OrderStatus.Pending) return nextStatus is OrderStatus.Shipping or OrderStatus.Cancelled;
            if (order.Status == OrderStatus.Shipping) return nextStatus is OrderStatus.Delivered or OrderStatus.Cancelled;
            return false;
        }

        private async Task RestoreStock(IEnumerable<OrderDetail> orderDetails)
        {
            var variantIds = orderDetails.Select(x => x.ProductVariantId).Distinct().ToList();
            var variants = await _db.ProductVariants.Where(x => variantIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
            foreach (var detail in orderDetails)
            {
                if (variants.TryGetValue(detail.ProductVariantId, out var variant)) variant.Quantity += detail.Quantity;
            }
        }

        private IActionResult RedirectToIndex(string? searchTerm, OrderStatus? orderStatus, string? paymentMethod, DateTime? fromDate, DateTime? toDate, int page)
        {
            return RedirectToAction(nameof(Index), new { area = "Admin", searchTerm, orderStatus, paymentMethod, fromDate = fromDate?.ToString("yyyy-MM-dd"), toDate = toDate?.ToString("yyyy-MM-dd"), page });
        }
    }
}