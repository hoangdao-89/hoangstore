using System.ComponentModel.DataAnnotations;

namespace hoangstore.Models.Enums
{
    public enum ProductGender
    {
        [Display(Name = "Nam")] Male = 1,
        [Display(Name = "Nữ")] Female = 2,
        [Display(Name = "Unisex")] Unisex = 3
    }
}
