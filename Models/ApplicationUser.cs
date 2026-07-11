using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Versioning;

namespace hoangstore.Models
{
    public class ApplicationUser:IdentityUser
    {

        [Required(ErrorMessage = "Họ không được để trống"), StringLength(50)]
        [Display(Name = "Họ")]
        public string LastName { get; set; }
        [Required(ErrorMessage = "Tên không được để trống"), StringLength(50)]
        [Display(Name ="Tên")]
        public string FirstName {  get; set; }
        [Required(ErrorMessage = "Địa chỉ không được để trống"), StringLength(256)]
        [Display(Name ="Địa chỉ")]
        public string Address {  get; set; }
        [Phone]
        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Display(Name = "Số điện thoại")]
        public override string PhoneNumber { get; set; }
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress]
        [Display(Name = "Email")]
        public override string Email { get ; set ; }
    }
}
