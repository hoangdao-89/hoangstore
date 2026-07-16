using System.Security.Claims;
using hoangstore.Data;
using hoangstore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hoangstore.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CartController(ApplicationDbContext db)
        {
            _db = db;
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

            var cartItems = await _db.CartItems
                .AsNoTracking()
                .Include(item => item.Cart)
                .Include(item => item.ProductVariant)
                    .ThenInclude(variant => variant.Product)
                .Where(item =>
                    item.Cart != null &&
                    item.Cart.UserId == userId)
                .OrderByDescending(item => item.Id)
                .ToListAsync();

            return View(cartItems);
        }

        // Cho phép người chưa đăng nhập gọi AJAX để nhận kết quả
        // redirectToLogin thay vì bị trả về nguyên trang HTML đăng nhập.
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(
            int variantId,
            int quantity = 1)
        {
            if (User.Identity == null ||
                !User.Identity.IsAuthenticated)
            {
                return Json(new
                {
                    success = false,
                    redirectToLogin = true,
                    message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ."
                });
            }

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Json(new
                {
                    success = false,
                    redirectToLogin = true,
                    message = "Không tìm thấy thông tin người dùng."
                });
            }

            if (variantId <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Biến thể sản phẩm không hợp lệ."
                });
            }

            if (quantity < 1 || quantity > 100)
            {
                return Json(new
                {
                    success = false,
                    message = "Số lượng sản phẩm phải nằm trong khoảng từ 1 đến 100."
                });
            }

            var variant = await _db.ProductVariants
                .Include(item => item.Product)
                .FirstOrDefaultAsync(item =>
                    item.Id == variantId);

            if (variant == null ||
                variant.Product == null ||
                variant.IsDeleted ||
                variant.Product.IsDeleted)
            {
                return Json(new
                {
                    success = false,
                    message = "Sản phẩm hoặc biến thể này không còn tồn tại."
                });
            }

            if (variant.Quantity <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Phiên bản sản phẩm này đã hết hàng."
                });
            }

            if (quantity > variant.Quantity)
            {
                return Json(new
                {
                    success = false,
                    message =
                        $"Số lượng trong kho không đủ. " +
                        $"Hiện chỉ còn {variant.Quantity} sản phẩm."
                });
            }

            var cart = await _db.Carts
                .FirstOrDefaultAsync(item =>
                    item.UserId == userId);

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

            var cartItem = await _db.CartItems
                .FirstOrDefaultAsync(item =>
                    item.CartId == cart.Id &&
                    item.ProductVariantId == variantId);

            if (cartItem != null)
            {
                var newQuantity =
                    cartItem.Quantity + quantity;

                if (newQuantity > 100)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Số lượng tối đa trong giỏ là 100."
                    });
                }

                if (newQuantity > variant.Quantity)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            $"Bạn không thể thêm vượt quá tồn kho. " +
                            $"Hiện chỉ còn {variant.Quantity} sản phẩm."
                    });
                }

                cartItem.Quantity = newQuantity;
            }
            else
            {
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

            var totalCartQuantity = await _db.CartItems
                .Where(item => item.CartId == cart.Id)
                .SumAsync(item => item.Quantity);

            return Json(new
            {
                success = true,
                newCount = totalCartQuantity,
                message = "Đã thêm vào giỏ hàng thành công!",
                cartItemId = cartItem.Id
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(
            int cartItemId,
            int step)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Json(new
                {
                    success = false,
                    redirectToLogin = true,
                    message = "Phiên đăng nhập đã hết hạn."
                });
            }

            if (step != 1 && step != -1)
            {
                return Json(new
                {
                    success = false,
                    message = "Thao tác thay đổi số lượng không hợp lệ."
                });
            }

            var cartItem = await _db.CartItems
                .Include(item => item.Cart)
                .Include(item => item.ProductVariant)
                    .ThenInclude(variant => variant.Product)
                .FirstOrDefaultAsync(item =>
                    item.Id == cartItemId &&
                    item.Cart != null &&
                    item.Cart.UserId == userId);

            if (cartItem == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy sản phẩm trong giỏ hàng của bạn."
                });
            }

            var variant = cartItem.ProductVariant;

            if (variant == null ||
                variant.Product == null ||
                variant.IsDeleted ||
                variant.Product.IsDeleted)
            {
                return Json(new
                {
                    success = false,
                    message = "Sản phẩm không còn tồn tại hoặc đã ngừng bán."
                });
            }

            if (variant.Quantity <= 0)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Sản phẩm đã hết hàng. Vui lòng xóa sản phẩm khỏi giỏ."
                });
            }

            var newQuantity =
                cartItem.Quantity + step;

            /*
             * Sửa dữ liệu cũ:
             * Ví dụ trước đây giỏ đã lưu 28 nhưng kho hiện chỉ còn 15.
             * Khi người dùng bấm giảm, đưa thẳng số lượng về 15 để họ
             * không phải bấm giảm nhiều lần.
             */
            if (step == -1 &&
                cartItem.Quantity > variant.Quantity)
            {
                newQuantity = variant.Quantity;
            }

            if (newQuantity < 1)
            {
                return Json(new
                {
                    success = false,
                    message = "Số lượng tối thiểu là 1."
                });
            }

            if (newQuantity > 100)
            {
                return Json(new
                {
                    success = false,
                    message = "Số lượng tối đa là 100."
                });
            }

            if (step == 1 &&
                newQuantity > variant.Quantity)
            {
                return Json(new
                {
                    success = false,
                    message =
                        $"Không đủ tồn kho. Sản phẩm hiện chỉ còn " +
                        $"{variant.Quantity} sản phẩm."
                });
            }

            cartItem.Quantity = newQuantity;
            cartItem.Cart!.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var newSubTotal =
                variant.FinalPrice * newQuantity;

            var totalCartQuantity = await _db.CartItems
                .Where(item =>
                    item.Cart != null &&
                    item.Cart.UserId == userId)
                .SumAsync(item => item.Quantity);

            return Json(new
            {
                success = true,
                newQty = newQuantity,
                newSubTotal,
                newCount = totalCartQuantity
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItem(
            int cartItemId)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Json(new
                {
                    success = false,
                    redirectToLogin = true,
                    message = "Phiên đăng nhập đã hết hạn."
                });
            }

            var cartItem = await _db.CartItems
                .Include(item => item.Cart)
                .FirstOrDefaultAsync(item =>
                    item.Id == cartItemId &&
                    item.Cart != null &&
                    item.Cart.UserId == userId);

            if (cartItem == null)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Sản phẩm không tồn tại hoặc không thuộc giỏ hàng của bạn."
                });
            }

            _db.CartItems.Remove(cartItem);

            if (cartItem.Cart != null)
            {
                cartItem.Cart.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            var totalCartQuantity = await _db.CartItems
                .Where(item =>
                    item.Cart != null &&
                    item.Cart.UserId == userId)
                .SumAsync(item => item.Quantity);

            return Json(new
            {
                success = true,
                newCount = totalCartQuantity
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelectedItems(
            [FromBody] List<int>? cartItemIds)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Json(new
                {
                    success = false,
                    redirectToLogin = true,
                    message = "Phiên đăng nhập đã hết hạn."
                });
            }

            if (cartItemIds == null ||
                cartItemIds.Count == 0)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Vui lòng chọn ít nhất một sản phẩm để xóa."
                });
            }

            var validIds = cartItemIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (validIds.Count == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Danh sách sản phẩm cần xóa không hợp lệ."
                });
            }

            /*
             * Chỉ lấy CartItem thuộc đúng người dùng.
             * Nếu request chứa ID của người khác thì ID đó bị bỏ qua.
             */
            var itemsToDelete = await _db.CartItems
                .Include(item => item.Cart)
                .Where(item =>
                    validIds.Contains(item.Id) &&
                    item.Cart != null &&
                    item.Cart.UserId == userId)
                .ToListAsync();

            if (itemsToDelete.Count == 0)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Không tìm thấy sản phẩm hợp lệ trong giỏ hàng của bạn."
                });
            }

            _db.CartItems.RemoveRange(itemsToDelete);

            var cart = itemsToDelete
                .Select(item => item.Cart)
                .FirstOrDefault(item => item != null);

            if (cart != null)
            {
                cart.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            var totalCartQuantity = await _db.CartItems
                .Where(item =>
                    item.Cart != null &&
                    item.Cart.UserId == userId)
                .SumAsync(item => item.Quantity);

            return Json(new
            {
                success = true,
                deletedIds = itemsToDelete.Select(item => item.Id),
                newCount = totalCartQuantity
            });
        }
    }
}