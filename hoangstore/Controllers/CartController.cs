using hoangstore.Data;
using hoangstore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace hoangstore.Controllers
{
    
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }
        [HttpPost]
        public async Task<IActionResult> AddToCart(int variantId, int quantity = 1)
        {
            
            //kiem tra dang nhap
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Json(new
                {
                    success = false,
                    redirectToLogin = true
                });
            }
            // lay userId cua nguoi dung
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)){
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy Id người dùng"
                });
            }
            // kiem tra bien the san pham
            //productvariant khong ton tai
            var variant = await _db.ProductVariants.FirstOrDefaultAsync(u => u.Id == variantId);
            if (variant == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Sản phẩm hoặc kích cỡ này không tồn tại!"
                });
            }
            //pvariant khong du so luong
            if (variant.Quantity < quantity)
            {
                return Json(new
                {
                    success = false,
                    message = $"Số lượng trong kho không đủ! Chỉ còn {variant.Quantity} sản phẩm."
                });
            }
            //lay hoac tao gio hang cua user
            var cart = await _db.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.Carts.Add(cart);
                await _db.SaveChangesAsync();
            }
            // xu ly cartitem
            var cartItem = await _db.CartItems.FirstOrDefaultAsync(c => c.CartId == cart.Id && c.ProductVariantId == variantId);
            //kiem tra xem cartItem da ton tai trong gio chua
            if (cartItem != null) {
                // neu mat hang nay da co trong gio->tang so luong
                if(cartItem.Quantity + quantity > variant.Quantity)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Bạn không thể thêm số lượng vượt quá tồn kho!"
                    });
                }
                cartItem.Quantity += quantity;
            }
            else
            {
                // neu chua co -> tao moi cartitem
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductVariantId = variantId,
                    Quantity = quantity
                };
                _db.CartItems.Add(cartItem);
            }
            cart.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            int totalCartItems = await _db.CartItems.Where(ci => ci.CartId == cart.Id).SumAsync(ci => ci.Quantity);
            return Json(new
            {
                success = true,
                newCount = totalCartItems,
                message = "Đã thêm vào giỏ hàng thành công!"
            });
        }
        public IActionResult Index()
        {

            return View();
        }
    }
}
