using hoangstore.Data;
using hoangstore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace hoangstore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        public HomeController(ApplicationDbContext db)
        {
            _db = db; 
        }

        public async Task<IActionResult> Index()
        {
            var product = await _db.Products.Include(p=>p.Category).Include(p=>p.ProductVariants).Where(p => p.IsDelete == false && p.Category!=null&& p.Category.IsDelete==false).ToListAsync();

            return View(product);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
