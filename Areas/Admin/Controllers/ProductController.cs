using hoangstore.Data;
using hoangstore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace hoangstore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles ="Admin")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _um;
        public ProductController(ApplicationDbContext db, UserManager<ApplicationUser> um)
        {
            _db = db;
            _um = um;
        }
       
        //Trang danh sach san pham
        public async Task<IActionResult> Index()
        {
            var ProductList = await _db.Products.Include(u => u.Category).Where(p=>p.IsDeleted==false && p.Category!=null && p.Category.IsDeleted ==false).ToListAsync();
            return View(ProductList);
        }
        //Them moi
        public async Task<IActionResult> Create()
        {
            await GetCategoryName();
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            
            if (!ModelState.IsValid)
            {
                await GetCategoryName();
                return View(product);
            }
            
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return RedirectToAction ("Index");
        }
        //sua
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();
            await GetCategoryName();
            return View(product);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product)
        {
            
            if (!ModelState.IsValid)
            {
                await GetCategoryName();
                return View(product);
            }
            var ProductInDB = await _db.Products.FindAsync(product.ProductId);
            if(ProductInDB == null) return NotFound();
            ProductInDB.Product_Name = product.Product_Name;
            
            ProductInDB.Product_Description = product.Product_Description;
            
            ProductInDB.CategoryId = product.CategoryId;

            ProductInDB.ModifiedDate = product.ModifiedDate;
            ProductInDB.ModifiedBy = product.ModifiedBy;
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        //xoa
   
        [HttpDelete]
      
        public async Task<IActionResult> Delete(int id) {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return Json(new {success=false});
            
            _db.Products.Update(product);
            await _db.SaveChangesAsync();
            return Json(new { success = true});
        }
       
        private async Task GetCategoryName()
        {
            ViewBag.CategoryList = await _db.Categories.Where(c => c.IsDeleted == false).Select(c=> new
            {
                CatId = c.CategoryId,
                CatName = c.Category_Name
            }).ToListAsync();
        }
    }
}
