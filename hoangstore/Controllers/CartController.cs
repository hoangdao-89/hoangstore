using hoangstore.Data;
using hoangstore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;
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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            //lay id nguoi dung
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }
            //lay ra cac cartitems cua userID nay
            var cartItems = await _db.CartItems.Include(c => c.ProductVariant).ThenInclude(c => c.Product).Include(c => c.Cart).Where(c => c.Cart.UserId == userId).ToListAsync();
            return View(cartItems);
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
            if (string.IsNullOrEmpty(userId))
            {
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
            if (cartItem != null)
            {
                // neu mat hang nay da co trong gio->tang so luong
                if (cartItem.Quantity + quantity > variant.Quantity)
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
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int step)
        {
            var cartItem = await _db.CartItems.Include(c => c.ProductVariant).FirstOrDefaultAsync(c => c.Id == cartItemId);
            if (cartItem == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy sản phẩm trong giỏ"
                });
            }
            //tinh so luong moi
            int newQuantity = cartItem.Quantity + step;
            if (newQuantity < 1)
            {
                return Json(new { success = false, message = "Số lượng tối thiểu là 1" });
            }
            if (newQuantity > 100)
            {
                return Json(new { success = false, message = "Số lượng tối đa là 100" });
            }
            cartItem.Quantity = newQuantity;
            await _db.SaveChangesAsync();

            //tinh tong tien moi
            var newSubTotal = (cartItem.ProductVariant?.Price ?? 0) * newQuantity;

            return Json(new
            {
                success = true,
                newQty = newQuantity,
                newSubTotal = newSubTotal,
            });
        }
        //API xoa san pham don le
        [HttpPost]
        public async Task<IActionResult> DeleteItem(int cartItemId)
        {
            var cartItem = await _db.CartItems.FindAsync(cartItemId);
            if (cartItem == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc đã bị xóa trước đó" });
            }
            _db.CartItems.Remove(cartItem);
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }
        //Xoa hang loat
        [HttpPost]
        public async Task<IActionResult> DeleteSelectedItems([FromBody] List<int> cartItemIds)
        {
            if(cartItemIds == null || !cartItemIds.Any())
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất một sản phẩm để xóa" });

            }
            var itemsToDelete = await _db.CartItems.Where(ci => cartItemIds.Contains(ci.Id)).ToListAsync();
            if(itemsToDelete.Count > 0 || itemsToDelete.Any())
            {
                _db.CartItems.RemoveRange(itemsToDelete);
                await _db.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

    }
}
