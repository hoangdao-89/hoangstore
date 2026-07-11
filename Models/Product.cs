
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hoangstore.Models;
public class Product: IAuditable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ProductId { get; set; } 

    [Required(ErrorMessage = "Tên sản phẩm là bắt buộc"), StringLength(100)]
    [Display(Name = "Tên sản phẩm")]
    public string Product_Name { get; set; } = null!;

    [StringLength(200)]
    [Required(ErrorMessage = "Mô tả là bắt buộc")]
    [Display(Name = "Mô tả")]
    public string? Product_Description { get; set; }
    [Required(ErrorMessage = "Link ảnh là bắt buộc")]
    [Display(Name = "Link ảnh")]
    public string? Image_Url { get; set; }

    [Display(Name = "Sản phẩm nổi bật")]
    public bool IsFeatured { get; set; } = false;
    //
    //
    public string CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string DeletedBy { get; set; }
    public DateTime? DeletedDate { get; set; }
    public bool IsDeleted { get; set; } = false;

    public int CategoryId { get; set; }
    [ForeignKey("CategoryId")]
    public Category? Category { get; set; }
    public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}