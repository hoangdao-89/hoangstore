
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hoangstore.Models;
public class Product
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

    [Required(ErrorMessage = "Giá tiền là bắt buộc")]
    [Display(Name = "Giá tiền")]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Số lượng còn là bắt buộc")]
    [Display(Name = "Số lượng còn")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "Link ảnh là bắt buộc")]
    [Display(Name = "Link ảnh")]
    public string? Image_Url { get; set; }

    [Display(Name = "Sản phẩm nổi bật")]
    public bool IsFeatured { get; set; } = false;

    [Display(Name = "Ngày tạo")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Display(Name = "Người tạo")]
    public string? CreatedBy { get; set; }

    [Display(Name = "Ngày cập nhật")]
    public DateTime? ModifiedDate { get; set; }

    [Display(Name = "Người cập nhật")]
    public string? ModifiedBy { get; set; }

    [Display(Name = "Xóa")]
    public bool IsDelete { get; set; } = false;

    [Display(Name = "Người xóa")]
    public string? DeleteBy { get; set; }

    [Display(Name = "Ngày xóa")]
    public DateTime? DeleteDate { get; set; } // Sửa thành nullable

    public int CategoryId { get; set; }
    [ForeignKey("CategoryId")]
    public Category? Category { get; set; }
    public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}