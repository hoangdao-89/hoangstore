using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using hoangstore.Data;
using hoangstore.Models;
using System.Threading.Tasks;
using System.Linq;

namespace hoangstore.ViewComponents
{
    public class CartCountViewComponent:ViewComponent
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _um;
        public CartCountViewComponent(ApplicationDbContext db, UserManager<ApplicationUser> um)
        {
            _db = db;
            _um = um;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            int count = 0;
            
            var user = await _um.GetUserAsync(HttpContext.User);
            if(user!= null)
            {
                count = await _db.CartItems.Include(c => c.Cart).Where(c => c.Cart.UserId == user.Id).SumAsync(c => c.Quantity);
                
            }
            return View(count);
        }
    }
}
