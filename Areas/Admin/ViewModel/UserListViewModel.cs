using hoangstore.Models;

namespace hoangstore.Areas.Admin.ViewModels
{
    public class UserListViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public string Role { get; set; } = "Customer";
        public bool IsLocked { get; set; }
    }
}