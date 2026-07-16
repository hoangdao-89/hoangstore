using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hoangstore.Models;

public class Category : IAuditable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Tên danh mục không được để trống")]
    [StringLength(
        100,
        ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự")]
    [Display(Name = "Tên danh mục")]
    public string Category_Name { get; set; } = string.Empty;

    [MaxLength(
        200,
        ErrorMessage = "Mô tả không được vượt quá 200 ký tự")]
    [Display(Name = "Mô tả danh mục")]
    public string? Description { get; set; }

    [Range(
        1,
        100,
        ErrorMessage = "Thứ tự hiển thị phải từ 1 đến 100")]
    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; } = 1;

    [Display(Name = "Trạng thái hiển thị")]
    public bool IsActive { get; set; } = true;

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? CreatedDate { get; set; }

    public string ModifiedBy { get; set; } = string.Empty;

    public DateTime? ModifiedDate { get; set; }

    public string DeletedBy { get; set; } = string.Empty;

    public DateTime? DeletedDate { get; set; }

    public bool IsDeleted { get; set; } = false;

    public ICollection<Product> Products { get; set; }
        = new List<Product>();
}