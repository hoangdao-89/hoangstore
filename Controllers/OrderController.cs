using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using hoangstore.Data;
using hoangstore.Models;
using hoangstore.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hoangstore.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private const string CodPaymentMethod = "COD";
        private const string VnPayPaymentMethod = "VNPAY";

        private readonly ApplicationDbContext _db;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            ApplicationDbContext db,
            ILogger<OrderController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var orders = await _db.Orders
                .AsNoTracking()
                .Where(order => order.UserId == userId)
                .Include(order => order.OrderDetails)
                    .ThenInclude(detail => detail.ProductVariant)
                        .ThenInclude(variant => variant!.Product)
                .OrderByDescending(order => order.OrderDate)
                .ThenByDescending(order => order.Id)
                .ToListAsync();

            return View(orders);
        }

        [HttpGet("/Order/Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var order = await _db.Orders
                .AsNoTracking()
                .Where(item =>
                    item.Id == id &&
                    item.UserId == userId)
                .Include(item => item.OrderDetails)
                    .ThenInclude(detail => detail.ProductVariant)
                        .ThenInclude(variant => variant!.Product)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> Checkout(string itemIds)
        {
            if (!TryParseCartItemIds(itemIds, out var listIds))
            {
                TempData["CartErrorMessage"] =
                    "Danh sách sản phẩm thanh toán không hợp lệ.";

                return RedirectToAction("Index", "Cart");
            }

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var selectedItems = await _db.CartItems
                .AsNoTracking()
                .Include(item => item.Cart)
                .Include(item => item.ProductVariant)
                    .ThenInclude(variant => variant.Product)
                .Where(item =>
                    listIds.Contains(item.Id) &&
                    item.Cart != null &&
                    item.Cart.UserId == userId)
                .ToListAsync();

            if (selectedItems.Count != listIds.Count)
            {
                TempData["CartErrorMessage"] =
                    "Có sản phẩm không tồn tại hoặc không thuộc giỏ hàng của bạn.";

                return RedirectToAction("Index", "Cart");
            }

            foreach (var item in selectedItems)
            {
                if (item.ProductVariant == null ||
                    item.ProductVariant.Product == null ||
                    item.ProductVariant.IsDeleted ||
                    item.ProductVariant.Product.IsDeleted)
                {
                    TempData["CartErrorMessage"] =
                        "Có sản phẩm không còn được bán.";

                    return RedirectToAction("Index", "Cart");
                }

                if (item.Quantity <= 0)
                {
                    TempData["CartErrorMessage"] =
                        "Số lượng sản phẩm trong giỏ hàng không hợp lệ.";

                    return RedirectToAction("Index", "Cart");
                }

                if (item.ProductVariant.Quantity < item.Quantity)
                {
                    TempData["CartErrorMessage"] =
                        $"{item.ProductVariant.Product.Product_Name} " +
                        $"không đủ tồn kho. Hiện chỉ còn " +
                        $"{item.ProductVariant.Quantity} sản phẩm.";

                    return RedirectToAction("Index", "Cart");
                }
            }

            var viewModel = new CheckoutViewModel
            {
                SelectedItems = selectedItems,

                TotalOrderPrice = selectedItems.Sum(item =>
                    item.ProductVariant!.FinalPrice * item.Quantity),

                SelectedCartItemIds = string.Join(",", listIds)
            };

            var currentUser = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user =>
                    user.Id == userId);

            if (currentUser != null)
            {
                ViewBag.ReceiverName =
                    $"{currentUser.LastName} {currentUser.FirstName}".Trim();

                ViewBag.ReceiverPhone =
                    currentUser.PhoneNumber;

                ViewBag.ShippingAddress =
                    currentUser.Address;
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout(
            Order order,
            string cartItemIdsString)
        {
            if (!TryParseCartItemIds(
                    cartItemIdsString,
                    out var listIds))
            {
                TempData["CartErrorMessage"] =
                    "Không tìm thấy sản phẩm hợp lệ để thanh toán.";

                return RedirectToAction("Index", "Cart");
            }

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var receiverName =
                order.ReceiverName?.Trim();

            var receiverPhone =
                order.ReceiverPhone?.Trim();

            var shippingAddress =
                order.ShippingAddress?.Trim();

            var paymentMethod =
                order.PaymentMethod?
                    .Trim()
                    .ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(receiverName) ||
                receiverName.Length > 100)
            {
                TempData["CheckoutErrorMessage"] =
                    "Họ và tên người nhận không hợp lệ.";

                return RedirectToAction(
                    nameof(Checkout),
                    new { itemIds = cartItemIdsString });
            }

            if (string.IsNullOrWhiteSpace(receiverPhone) ||
                receiverPhone.Length > 20 ||
                !new PhoneAttribute().IsValid(receiverPhone))
            {
                TempData["CheckoutErrorMessage"] =
                    "Số điện thoại người nhận không hợp lệ.";

                return RedirectToAction(
                    nameof(Checkout),
                    new { itemIds = cartItemIdsString });
            }

            if (string.IsNullOrWhiteSpace(shippingAddress) ||
                shippingAddress.Length > 500)
            {
                TempData["CheckoutErrorMessage"] =
                    "Địa chỉ nhận hàng không hợp lệ.";

                return RedirectToAction(
                    nameof(Checkout),
                    new { itemIds = cartItemIdsString });
            }

            if (paymentMethod != CodPaymentMethod &&
                paymentMethod != VnPayPaymentMethod)
            {
                TempData["CheckoutErrorMessage"] =
                    "Phương thức thanh toán không hợp lệ.";

                return RedirectToAction(
                    nameof(Checkout),
                    new { itemIds = cartItemIdsString });
            }

            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var selectedItems = await _db.CartItems
                    .Include(item => item.Cart)
                    .Include(item => item.ProductVariant)
                        .ThenInclude(variant => variant.Product)
                    .Where(item =>
                        listIds.Contains(item.Id) &&
                        item.Cart != null &&
                        item.Cart.UserId == userId)
                    .ToListAsync();

                if (selectedItems.Count != listIds.Count)
                {
                    await transaction.RollbackAsync();

                    TempData["CartErrorMessage"] =
                        "Giỏ hàng đã thay đổi. Vui lòng kiểm tra lại.";

                    return RedirectToAction("Index", "Cart");
                }

                decimal totalOrderPrice = 0;

                foreach (var item in selectedItems)
                {
                    var variant = item.ProductVariant;

                    if (variant == null ||
                        variant.Product == null ||
                        variant.IsDeleted ||
                        variant.Product.IsDeleted)
                    {
                        await transaction.RollbackAsync();

                        TempData["CheckoutErrorMessage"] =
                            "Có sản phẩm không còn tồn tại hoặc đã ngừng bán.";

                        return RedirectToAction(
                            nameof(Checkout),
                            new { itemIds = cartItemIdsString });
                    }

                    if (item.Quantity <= 0)
                    {
                        await transaction.RollbackAsync();

                        TempData["CheckoutErrorMessage"] =
                            "Số lượng sản phẩm không hợp lệ.";

                        return RedirectToAction(
                            nameof(Checkout),
                            new { itemIds = cartItemIdsString });
                    }

                    if (variant.Quantity < item.Quantity)
                    {
                        await transaction.RollbackAsync();

                        TempData["CheckoutErrorMessage"] =
                            $"{variant.Product.Product_Name} không đủ tồn kho. " +
                            $"Hiện chỉ còn {variant.Quantity} sản phẩm.";

                        return RedirectToAction(
                            nameof(Checkout),
                            new { itemIds = cartItemIdsString });
                    }

                    totalOrderPrice +=
                        variant.FinalPrice * item.Quantity;
                }

                if (totalOrderPrice <= 0)
                {
                    await transaction.RollbackAsync();

                    TempData["CartErrorMessage"] =
                        "Tổng tiền đơn hàng không hợp lệ.";

                    return RedirectToAction("Index", "Cart");
                }

                var newOrder = new Order
                {
                    UserId = userId,
                    ReceiverName = receiverName,
                    ReceiverPhone = receiverPhone,
                    ShippingAddress = shippingAddress,
                    PaymentMethod = paymentMethod,
                    TotalPrice = totalOrderPrice,
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.Pending
                };

                _db.Orders.Add(newOrder);

                foreach (var item in selectedItems)
                {
                    var variant = item.ProductVariant!;

                    var orderDetail = new OrderDetail
                    {
                        Order = newOrder,
                        ProductVariantId = item.ProductVariantId,
                        Quantity = item.Quantity,
                        Price = variant.FinalPrice
                    };

                    _db.OrderDetails.Add(orderDetail);

                    variant.Quantity -= item.Quantity;
                }

                _db.CartItems.RemoveRange(selectedItems);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                if (paymentMethod == VnPayPaymentMethod)
                {
                    return RedirectToAction(
                        "CreatePaymentUrl",
                        "Payment",
                        new { orderId = newOrder.Id });
                }

                return RedirectToAction(
                    nameof(OrderSuccess),
                    new { id = newOrder.Id });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();

                _logger.LogWarning(
                    ex,
                    "Xung đột tồn kho khi user {UserId} đặt hàng.",
                    userId);

                TempData["CartErrorMessage"] =
                    "Tồn kho vừa thay đổi. Vui lòng kiểm tra lại giỏ hàng.";

                return RedirectToAction("Index", "Cart");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(
                    ex,
                    "Lỗi tạo đơn hàng cho user {UserId}.",
                    userId);

                TempData["CheckoutErrorMessage"] =
                    "Không thể tạo đơn hàng lúc này. Vui lòng thử lại.";

                return RedirectToAction(
                    nameof(Checkout),
                    new { itemIds = cartItemIdsString });
            }
        }

        [HttpGet]
        public async Task<IActionResult> OrderSuccess(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var orderExists = await _db.Orders
                .AsNoTracking()
                .AnyAsync(order =>
                    order.Id == id &&
                    order.UserId == userId);

            if (!orderExists)
            {
                return NotFound();
            }

            ViewBag.OrderId = id;

            return View();
        }

        private static bool TryParseCartItemIds(
            string? itemIds,
            out List<int> result)
        {
            result = new List<int>();

            if (string.IsNullOrWhiteSpace(itemIds))
            {
                return false;
            }

            var uniqueIds = new HashSet<int>();

            var parts = itemIds.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

            foreach (var part in parts)
            {
                if (!int.TryParse(part, out var id) ||
                    id <= 0)
                {
                    result.Clear();
                    return false;
                }

                uniqueIds.Add(id);
            }

            result = uniqueIds.ToList();

            return result.Count > 0;
        }
    }
}