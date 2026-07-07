using hoangstore.Data;
using hoangstore.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;

namespace hoangstore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        public OrderController(ApplicationDbContext db)
        {
            _db = db;
        }
        [HttpGet]
        public async Task<IActionResult> Index(OrderStatus? orderStatus)
        {
            var order = _db.Orders.AsQueryable();
            if (orderStatus.HasValue)
            {
                order = order.Where(o => o.Status == orderStatus.Value);
            }
            ViewBag.CurrentFilter = orderStatus;
            return View(order);
        }
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var order = await _db.Orders.Include(o => o.OrderDetails).ThenInclude(o => o.ProductVariant).ThenInclude(o => o.Product).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, OrderStatus orderStatus)
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng cần cập nhật.";
                return RedirectToAction("Index");
            }
            // neu don hang da giao || da huy thi khong cho doi trang thai nua
            if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Đơn hàng đã hoàn thành hoặc đã hủy, không thể đổi trạng thái!";
                return RedirectToAction("Details", new { id = orderId });
            }
            order.Status = orderStatus;
            _db.Orders.Update(order);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Cập nhật trạng thái đơn hàng #{orderId} thành công!";
            return RedirectToAction("Details", new { id = orderId });
        }
    }
}
