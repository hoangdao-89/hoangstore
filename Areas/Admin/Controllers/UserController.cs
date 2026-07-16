using hoangstore.Areas.Admin.ViewModels;
using hoangstore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hoangstore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserController> _logger;

        public UserController(UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ILogger<UserController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm, int page = 1)
        {
            if (!IsCurrentUserRootAdmin()) return Forbid();

            const int pageSize = 10;
            searchTerm = searchTerm?.Trim();
            if (page < 1) page = 1;

            var query = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(x =>
                    x.FirstName.Contains(searchTerm) ||
                    x.LastName.Contains(searchTerm) ||
                    x.Email.Contains(searchTerm) ||
                    x.PhoneNumber.Contains(searchTerm));
            }

            var totalItems = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            if (page > totalPages) page = totalPages;

            var users = await query.OrderBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userList = new List<UserListViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                userList.Add(new UserListViewModel
                {
                    User = user,
                    Role = roles.FirstOrDefault() ?? "Chưa phân quyền",
                    IsLocked = IsLocked(user)
                });
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.RootAdminEmail = GetRootAdminEmail();

            ViewBag.Roles = await _roleManager.Roles
                .Where(x => x.Name == "Admin" || x.Name == "Khách hàng")
                .Select(x => x.Name!)
                .OrderBy(x => x)
                .ToListAsync();

            return View(userList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string userId, string role,
            string? searchTerm, int page = 1)
        {
            if (!IsCurrentUserRootAdmin()) return Forbid();

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return RedirectWithError("Không tìm thấy tài khoản.", searchTerm, page);

            if (IsRootAdmin(user))
                return RedirectWithError("Không thể thay đổi vai trò của Admin gốc.", searchTerm, page);

            if (role != "Admin" && role != "Khách hàng")
                return RedirectWithError("Vai trò không hợp lệ.", searchTerm, page);

            if (!await _roleManager.RoleExistsAsync(role))
                return RedirectWithError("Vai trò chưa tồn tại trong hệ thống.", searchTerm, page);

            var currentRoles = await _userManager.GetRolesAsync(user);

            if (currentRoles.Count == 1 && currentRoles.Contains(role))
                return RedirectWithSuccess("Tài khoản đã có vai trò này.", searchTerm, page);

            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!removeResult.Succeeded)
            {
                var removeError = string.Join(" ", removeResult.Errors.Select(x => x.Description));
                return RedirectWithError(removeError, searchTerm, page);
            }

            var addResult = await _userManager.AddToRoleAsync(user, role);

            if (!addResult.Succeeded)
            {
                if (currentRoles.Any())
                    await _userManager.AddToRolesAsync(user, currentRoles);

                var addError = string.Join(" ", addResult.Errors.Select(x => x.Description));
                return RedirectWithError(addError, searchTerm, page);
            }

            await _userManager.UpdateSecurityStampAsync(user);

            return RedirectWithSuccess("Cập nhật vai trò thành công.", searchTerm, page);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string userId,
            string? searchTerm, int page = 1)
        {
            if (!IsCurrentUserRootAdmin()) return Forbid();

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return RedirectWithError("Không tìm thấy tài khoản.", searchTerm, page);

            if (IsRootAdmin(user))
                return RedirectWithError("Không thể khóa tài khoản Admin gốc.", searchTerm, page);

            var isLocked = IsLocked(user);

            try
            {
                var enableResult = await _userManager.SetLockoutEnabledAsync(user, true);

                if (!enableResult.Succeeded)
                {
                    var enableError = string.Join(" ", enableResult.Errors.Select(x => x.Description));
                    return RedirectWithError(enableError, searchTerm, page);
                }

                var lockResult = await _userManager.SetLockoutEndDateAsync(
                    user, isLocked ? null : DateTimeOffset.MaxValue);

                if (!lockResult.Succeeded)
                {
                    var lockError = string.Join(" ", lockResult.Errors.Select(x => x.Description));
                    return RedirectWithError(lockError, searchTerm, page);
                }

                await _userManager.UpdateSecurityStampAsync(user);

                var successMessage = isLocked
                    ? "Mở khóa tài khoản thành công."
                    : "Khóa tài khoản thành công.";

                return RedirectWithSuccess(successMessage, searchTerm, page);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thay đổi trạng thái tài khoản {UserId}", userId);
                return RedirectWithError("Không thể thay đổi trạng thái tài khoản lúc này.", searchTerm, page);
            }
        }

        private bool IsCurrentUserRootAdmin()
        {
            var rootAdminEmail = GetRootAdminEmail();
            var currentEmail = User.Identity?.Name;

            return !string.IsNullOrWhiteSpace(rootAdminEmail) &&
                   string.Equals(currentEmail, rootAdminEmail, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsRootAdmin(ApplicationUser user)
        {
            var rootAdminEmail = GetRootAdminEmail();

            return !string.IsNullOrWhiteSpace(rootAdminEmail) &&
                   string.Equals(user.Email, rootAdminEmail, StringComparison.OrdinalIgnoreCase);
        }

        private string? GetRootAdminEmail()
        {
            return _configuration["SystemAdmin:Email"];
        }

        private static bool IsLocked(ApplicationUser user)
        {
            return user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
        }

        private IActionResult RedirectWithSuccess(string message, string? searchTerm, int page)
        {
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index), new { searchTerm, page });
        }

        private IActionResult RedirectWithError(string message, string? searchTerm, int page)
        {
            TempData["ErrorMessage"] = message;
            return RedirectToAction(nameof(Index), new { searchTerm, page });
        }
    }
}