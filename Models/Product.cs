using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using hoangstore.Models.Enums;

namespace hoangstore.Models;

public class Product : IAuditable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
    [StringLength(100, ErrorMessage = "Tên sản phẩm không được vượt quá 100 ký tự")]
    [Display(Name = "Tên sản phẩm")]
    public string Product_Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mô tả sản phẩm không được để trống")]
    [StringLength(200, ErrorMessage = "Mô tả không được vượt quá 200 ký tự")]
    [Display(Name = "Mô tả sản phẩm")]
    public string Product_Description { get; set; } = string.Empty;

    [Display(Name = "Ảnh sản phẩm")]
    public string Image_Url { get; set; } = string.Empty;

    [Display(Name = "Sản phẩm nổi bật")]
    public bool IsFeatured { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn giới tính")]
    [Display(Name = "Giới tính")]
    public ProductGender Gender { get; set; } = ProductGender.Unisex;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục")]
    [Display(Name = "Danh mục")]
    public int CategoryId { get; set; }

    [ValidateNever]
    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    [ValidateNever]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? CreatedDate { get; set; }

    [ValidateNever]
    public string ModifiedBy { get; set; } = string.Empty;

    public DateTime? ModifiedDate { get; set; }

    [ValidateNever]
    public string DeletedBy { get; set; } = string.Empty;

    public DateTime? DeletedDate { get; set; }

    public bool IsDeleted { get; set; }

    [ValidateNever]
    public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}