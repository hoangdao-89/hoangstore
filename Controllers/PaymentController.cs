using System.Globalization;
using System.Security.Claims;
using hoangstore.Data;
using hoangstore.Models.Enums;
using hoangstore.Models.Services;
using hoangstore.Models.VNPay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hoangstore.Controllers
{
    public class PaymentController : Controller
    {
        private const string VnPayPaymentMethod = "VNPAY";

        private readonly ApplicationDbContext _db;
        private readonly IVnPayService _vnPayService;

        public PaymentController(
            ApplicationDbContext db,
            IVnPayService vnPayService)
        {
            _db = db;
            _vnPayService = vnPayService;
        }

        // Chỉ người đã đăng nhập và là chủ sở hữu đơn hàng
        // mới được tạo đường dẫn thanh toán.
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CreatePaymentUrl(int orderId)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var order = await _db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(order =>
                    order.Id == orderId &&
                    order.UserId == userId);

            if (order == null)
            {
                return NotFound(
                    "Không tìm thấy đơn hàng hoặc bạn không có quyền thanh toán đơn này.");
            }

            if (!string.Equals(
                    order.PaymentMethod,
                    VnPayPaymentMethod,
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "Đơn hàng này không sử dụng phương thức thanh toán VNPAY.");
            }

            if (order.Status != OrderStatus.Pending)
            {
                return BadRequest(
                    "Đơn hàng này không còn ở trạng thái chờ thanh toán.");
            }

            if (order.TotalPrice <= 0)
            {
                return BadRequest(
                    "Tổng tiền của đơn hàng không hợp lệ.");
            }

            var request = new VnPaymentRequest
            {
                OrderId = order.Id,
                Amount = order.TotalPrice,
                OrderDescription =
                    $"ThanhToanDonHang{order.Id}",
                BankCode = string.Empty
            };

            var paymentUrl =
                _vnPayService.CreatePaymentUrl(
                    HttpContext,
                    request);

            return Redirect(paymentUrl);
        }

        // VNPay chuyển người dùng về URL này nên không yêu cầu đăng nhập.
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> PaymentCallback()
        {
            ViewBag.Status = "error";
            ViewBag.Message =
                "Không thể xử lý kết quả thanh toán.";

            // Kiểm tra chữ ký thông qua VnPayService.
            var response =
                _vnPayService.PaymentExecute(Request.Query);

            if (!response.Success)
            {
                ViewBag.Message =
                    "Lỗi bảo mật: Chữ ký xác thực từ VNPAY không hợp lệ.";

                return View();
            }

            if (!TryGetOrderId(
                    response.OrderId,
                    out var orderId))
            {
                ViewBag.Message =
                    "Mã tham chiếu đơn hàng từ VNPAY không hợp lệ.";

                return View();
            }

            var order = await _db.Orders
                .Include(order => order.OrderDetails)
                .FirstOrDefaultAsync(order =>
                    order.Id == orderId);

            if (order == null)
            {
                ViewBag.Message =
                    "Không tìm thấy đơn hàng tương ứng.";

                return View();
            }

            if (!string.Equals(
                    order.PaymentMethod,
                    VnPayPaymentMethod,
                    StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Message =
                    "Phương thức thanh toán của đơn hàng không hợp lệ.";

                return View();
            }

            // vnp_Amount được VNPay trả về theo đơn vị tiền x100.
            var amountText =
                Request.Query["vnp_Amount"].ToString();

            var amountIsValid = long.TryParse(
                amountText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var paidAmount);

            var expectedAmount = Convert.ToInt64(
                decimal.Round(
                    order.TotalPrice * 100m,
                    0,
                    MidpointRounding.AwayFromZero));

            if (!amountIsValid ||
                paidAmount != expectedAmount)
            {
                // Không tự động hoàn kho tại đây vì có khả năng
                // giao dịch đã phát sinh nhưng dữ liệu không khớp.
                // Đơn vẫn Pending để cleanup service xử lý sau.
                ViewBag.Message =
                    "Số tiền VNPAY trả về không khớp với đơn hàng. " +
                    "Đơn hàng chưa được cập nhật.";

                return View();
            }

            var transactionStatus =
                Request.Query["vnp_TransactionStatus"]
                    .ToString();

            // Một số response có thể không chứa TransactionStatus.
            // Nếu có thì bắt buộc phải bằng 00.
            var transactionSucceeded =
                string.IsNullOrWhiteSpace(transactionStatus) ||
                transactionStatus == "00";

            var paymentSucceeded =
                response.VnPayResponseCode == "00" &&
                transactionSucceeded;

            if (paymentSucceeded)
            {
                return await HandleSuccessfulPayment(order);
            }

            return await HandleFailedPayment(
                order,
                response.VnPayResponseCode);
        }

        private async Task<IActionResult> HandleSuccessfulPayment(
            Models.Order order)
        {
            // Callback thành công đã được xử lý trước đó.
            if (order.Status == OrderStatus.Processing ||
                order.Status == OrderStatus.Shipping ||
                order.Status == OrderStatus.Delivered)
            {
                ViewBag.Status = "success";
                ViewBag.Message =
                    "Giao dịch này đã được xác nhận trước đó.";

                return View("PaymentCallback");
            }

            // Đơn đã hết hạn và được cleanup service hoàn kho.
            // Không tự động chuyển lại sang Processing.
            if (order.Status == OrderStatus.Cancelled)
            {
                ViewBag.Status = "error";
                ViewBag.Message =
                    "Đơn hàng đã bị hủy hoặc hết hạn thanh toán.";

                return View("PaymentCallback");
            }

            if (order.Status != OrderStatus.Pending)
            {
                ViewBag.Status = "error";
                ViewBag.Message =
                    "Trạng thái đơn hàng không hợp lệ.";

                return View("PaymentCallback");
            }

            order.Status = OrderStatus.Processing;

            await _db.SaveChangesAsync();

            ViewBag.Status = "success";
            ViewBag.Message =
                "Thanh toán thành công qua cổng VNPAY!";

            return View("PaymentCallback");
        }

        private async Task<IActionResult> HandleFailedPayment(
            Models.Order order,
            string responseCode)
        {
            // Callback thất bại đã được xử lý trước đó.
            // Không được hoàn kho thêm lần nữa.
            if (order.Status == OrderStatus.Cancelled)
            {
                ViewBag.Status = "error";
                ViewBag.Message =
                    "Giao dịch thất bại đã được xử lý trước đó.";

                return View("PaymentCallback");
            }

            // Không hủy hoặc hoàn kho đơn đã thanh toán/đang giao.
            if (order.Status != OrderStatus.Pending)
            {
                ViewBag.Status = "error";
                ViewBag.Message =
                    "Đơn hàng đã được xử lý nên không thể hủy.";

                return View("PaymentCallback");
            }

            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            try
            {
                var variantIds = order.OrderDetails
                    .Select(detail =>
                        detail.ProductVariantId)
                    .Distinct()
                    .ToList();

                var variants = await _db.ProductVariants
                    .Where(variant =>
                        variantIds.Contains(variant.Id))
                    .ToDictionaryAsync(variant =>
                        variant.Id);

                foreach (var detail in order.OrderDetails)
                {
                    if (variants.TryGetValue(
                            detail.ProductVariantId,
                            out var variant))
                    {
                        variant.Quantity += detail.Quantity;
                    }
                }

                // Đổi trạng thái trong cùng transaction với hoàn kho.
                order.Status = OrderStatus.Cancelled;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            ViewBag.Status = "error";
            ViewBag.Message =
                $"Thanh toán thất bại! Mã lỗi: {responseCode}";

            return View("PaymentCallback");
        }

        private static bool TryGetOrderId(
            string transactionReference,
            out int orderId)
        {
            orderId = 0;

            if (string.IsNullOrWhiteSpace(
                    transactionReference))
            {
                return false;
            }

            var orderIdPart = transactionReference
                .Split(
                    '_',
                    StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            return int.TryParse(
                orderIdPart,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out orderId);
        }
    }
}