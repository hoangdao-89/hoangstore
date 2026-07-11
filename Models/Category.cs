
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hoangstore.Models;
public class Category : IAuditable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CategoryId { get; set; } // Đổi cho đồng bộ với Product.CategoryId

    [StringLength(100)]
    [Required(ErrorMessage = "Tên danh mục không được để trống")]
    [Display(Name = "Tên danh mục")]
    public string Category_Name { get; set; } = null!;

    [Display(Name = "Mô tả danh mục")]
    [MaxLength(200)]
    public string? Description { get; set; }

    [Display(Name = "Thứ tự hiển thị")]
    [Range(1, 100, ErrorMessage = "Thứ tự từ 1-100")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Trạng thái hiển thị")]
    public bool IsActive { get; set; } = true;
    //
    //
    public string CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string DeletedBy { get; set; }
    public DateTime? DeletedDate { get; set; }
    public bool IsDeleted { get; set; } = false;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}