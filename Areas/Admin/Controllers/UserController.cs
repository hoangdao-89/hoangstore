using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using hoangstore.Data;
using Microsoft.AspNetCore.Identity;

namespace hoangstore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles ="Admin")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _um;
        public UserController(ApplicationDbContext db, UserManager<IdentityUser> um)
        {
            _db = db;
            _um = um;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
