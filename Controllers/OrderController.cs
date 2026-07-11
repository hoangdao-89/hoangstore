using hoangstore.Data;
using hoangstore.Models;
using hoangstore.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace hoangstore.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        public OrderController(ApplicationDbContext db)
        {
            _db = db;
        }
        //ham xu lt phan nhap thong tin nhan hang
        [HttpGet]
        public async Task<IActionResult> Checkout(string itemIds)
        {
            if (string.IsNullOrEmpty(itemIds))
            {
                return RedirectToAction("Index", "Cart");
            }
            // chuyen chuoi id "1,2" tu Url thanh ds so nguyen [1,2]
            var listIds = itemIds.Split(',').Select(int.Parse).ToList();
            // lay id nguoi dung
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // lay thong tin cua cac san pham ma khach da tich chon de hien thi
            var selectedItems = await _db.CartItems.Include(c => c.ProductVariant).ThenInclude(c => c.Product).Where(c => listIds.Contains(c.Id) && c.Cart.UserId == userId).ToListAsync();
            if (!selectedItems.Any())
            {
                return RedirectToAction("Index", "Cart");
            }
            // nap du lieu vao checkoutviewmodel
            var viewModel = new CheckoutViewModel
            {
                SelectedItems = selectedItems,
                TotalOrderPrice = selectedItems.Sum(t => (t.ProductVariant?.Price ?? 0) * t.Quantity),
                SelectedCartItemIds = itemIds
            };
            // tu donfg lay thong tin cua nguoi dung
            var currentUser = await _db.Users.FindAsync(userId);
            if (currentUser != null) {
                ViewBag.ReceiverName = $"{currentUser.LastName} {currentUser.FirstName}";
                ViewBag.ReceiverPhone = currentUser.PhoneNumber;
                ViewBag.ShippingAddress = currentUser.Address;
            }
            return View(viewModel);

        }
        // ham thuc hien luu xuong db va tru di so luong ton kho\
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout(Order order, string cartItemIdsString)
        {
            if (string.IsNullOrEmpty(cartItemIdsString))
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm hợp lệ để thanh toán";
                return RedirectToAction("Index", "Cart");
            }
            var listIds = cartItemIdsString.Split(',').Select(int.Parse).ToList();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var selectedItems = await _db.CartItems
                 .Include(c => c.ProductVariant)
                 .ThenInclude(v => v.Product)
                 .Where(c => listIds.Contains(c.Id) && c.Cart.UserId == userId)
                 .ToListAsync();
            if (!selectedItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng đang trong phiên làm việc hoặc quá thời gian chờ";
                return RedirectToAction("Index", "Cart");
            }
            // Kiem tra ton kho va tinh tong tien
            decimal totalOrderPrice = 0;
            foreach (var i in selectedItems)
            {
                if (i.ProductVariant == null)
                {
                    TempData["ErrorMessage"] = "Có sản phẩm không còn tồn tại trên hệ thống.";
                    return RedirectToAction("Checkout", new { itemIds = cartItemIdsString });

                }
                totalOrderPrice += (i.ProductVariant.Price * i.Quantity);
            }
            // khoi tao thong tin don hang
            var newOrder = new Order
            {
                UserId = userId,
                ReceiverName = order.ReceiverName,
                ReceiverPhone = order.ReceiverPhone,
                ShippingAddress = order.ShippingAddress,
                PaymentMethod = order.PaymentMethod,
                TotalPrice = totalOrderPrice,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending
            };
            
            _db.Orders .Add(newOrder);
            await _db.SaveChangesAsync();

            // duyet qua tung sp de tao chi tiet don hang va tru so tronf kho
            foreach(var i in selectedItems)
            {
                var oderDetail = new OrderDetail
                {
                    // Gan ma don hang tong vua sinh o tren vao
                    OrderId = newOrder.Id,
                    ProductVariantId = i.ProductVariantId,
                    Quantity = i.Quantity,
                    // Chot dung muc gia luc bam dat hang
                    Price = i.ProductVariant.Price
                };
                _db.OrderDetails.Add(oderDetail);

                // tru so luong ton kho
                i.ProductVariant.Quantity -= i.Quantity;
                _db.ProductVariants.Update(i.ProductVariant);
            }
            // xoa cac don hang da thanh toan ra khoi gio hang
            _db.CartItems.RemoveRange(selectedItems);
            await _db.SaveChangesAsync();
            //\
            // neu thanh quan truc tuyen
            if (newOrder.PaymentMethod != null && newOrder.PaymentMethod.ToUpper() == "VNPAY")
            {
                // Chuyen sang PaymentController (Client) de tu dong sinh link va chuyen khach sang tran VNP de thanh toam 
                return RedirectToAction("CreatePaymentUrl", "Payment", new { orderId = newOrder.Id });
            }

            // Neu phuong thuc thanh toan la COD thi chuyen sang tranhg thong bao mua thanh cong
            return RedirectToAction("OrderSuccess", new { id = newOrder.Id });
            
        }
        // ham hien thi mua thanh cong
        [HttpGet]
        public IActionResult OrderSuccess(int id)
        {
            ViewBag.OrderId = id;
            return View();
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
