using hoangstore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using hoangstore.Models;
using NuGet.Packaging.Core;
using System.Linq.Expressions;
namespace hoangstore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles ="Admin")]
    public class CategoryController : Controller
    {
      
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _um;
        public CategoryController(ApplicationDbContext db, UserManager<ApplicationUser> um)
        {
            _db = db;
            _um = um;
        }
       
        //danh sach categori
        public async Task<IActionResult> Index()
        {
            var CategoriesList = await _db.Categories.Where(c => c.IsDeleted==false).ToListAsync();
            return View(CategoriesList);
        }
        //them categori
        public async Task<IActionResult> Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(Category category) {
           
            if (!ModelState.IsValid)
            {
                return View(category);
            }
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if(id == 0||id == null)
            {
                return NotFound();
            }
            var categories = await _db.Categories.FindAsync(id);
            if(categories == null) return NotFound();
            return View(categories);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category category)
        {
            
            if (!ModelState.IsValid)
            {
                return View(category);
            }
            var categoryInDb = await _db.Categories.FindAsync(category.CategoryId);
            if (categoryInDb == null) return NotFound();

            categoryInDb.Category_Name = category.Category_Name;
            categoryInDb.Description = category.Description;
            categoryInDb.DisplayOrder = category.DisplayOrder;
            categoryInDb.IsActive = category.IsActive;

            
            categoryInDb.ModifiedDate = category.ModifiedDate;
            categoryInDb.ModifiedBy = category.ModifiedBy;
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            
            var category = await _db.Categories.FindAsync(id);
            if(category == null) return Json(new {success=false});
            _db.Categories.Update(category);
            await _db.SaveChangesAsync();
         
            return Json(new {success=true});
        }
        

    }
}
