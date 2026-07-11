using System;
using System.Linq;
using System.Threading.Tasks;
using hoangstore.Data;
using hoangstore.Models;
using hoangstore.Models.Enums;
using hoangstore.Models.VNPay; 
using hoangstore.Models.Services;  
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hoangstore.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IVnPayService _vnPayService;

        // Ép kiểu (Inject) đúng Service tự viết vào Controller
        public PaymentController(ApplicationDbContext db, IVnPayService vnPayService)
        {
            _db = db;
            _vnPayService = vnPayService;
        }

        [HttpGet]
        public async Task<IActionResult> CreatePaymentUrl(int orderId)
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null) return NotFound("Không tìm thấy đơn hàng!");

            // Đóng gói request bằng chính VnPaymentRequest của bạn (không dùng PaymentRequest của Nuget nữa)
            var request = new VnPaymentRequest
            {
                OrderId = order.Id,
                Amount = (double)order.TotalPrice,
                OrderDescription = "ThanhToanDonHang" + order.Id,
                BankCode = ""
            };

            // Gọi đúng 1 hàm duy nhất từ Service để sinh link
            string paymentUrl = _vnPayService.CreatePaymentUrl(HttpContext, request);

            return Redirect(paymentUrl);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCallback()
        {
            // Bắn toàn bộ Query nhận được sang Service xử lý và check chữ ký
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (response.Success)
            {
                string orderIdStr = response.OrderId.Split('_').FirstOrDefault();
                int.TryParse(orderIdStr, out int orderId);

                var order = await _db.Orders.FindAsync(orderId);
                if (order != null)
                {
                    if (response.VnPayResponseCode == "00")
                    {
                        order.Status = OrderStatus.Processing;
                        _db.Orders.Update(order);
                        await _db.SaveChangesAsync();

                        ViewBag.Message = "Thanh toán thành công qua cổng VNPAY!";
                        ViewBag.Status = "success";
                    }
                    else
                    {
                        order.Status = OrderStatus.Cancelled;
                        _db.Orders.Update(order);

                        // Xử lý hoàn kho sản phẩm nếu lỗi hoặc hủy
                        var orderDetails = await _db.OrderDetails
                            .Where(d => d.OrderId == order.Id)
                            .ToListAsync();

                        foreach (var detail in orderDetails)
                        {
                            var variant = await _db.ProductVariants.FindAsync(detail.ProductVariantId);
                            if (variant != null)
                            {
                                variant.Quantity += detail.Quantity;
                                _db.ProductVariants.Update(variant);
                            }
                        }

                        await _db.SaveChangesAsync();

                        ViewBag.Message = $"Thanh toán thất bại! Mã lỗi: {response.VnPayResponseCode}";
                        ViewBag.Status = "error";
                    }
                }
            }
            else
            {
                ViewBag.Message = "Lỗi bảo mật: Sai chữ ký xác thực từ VNPAY!";
                ViewBag.Status = "error";
            }

            return View();
        }
       
        
    }
}