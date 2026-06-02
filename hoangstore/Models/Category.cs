using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hoangstore.Models
{
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Category_Id {  get; set; }
        [StringLength(100)]
        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [Display(Name = "Tên danh mục")]
        public string Category_Name { get; set; }
        [Display(Name = "Mô tả danh mục")]
        [MaxLength(200)]
        public string? Description { get; set; }
        [Display(Name = "Thứ tự hiển thị")]
        [Range(1, 100, ErrorMessage = "Thứ tự từ 1-100")]
        public int DisplayOrder { get; set; }
        [Display(Name = "Ngày tạo")]

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        [Display(Name = "Người tạo")]
        public string? CreatedBy { get; set; }
        [Display(Name ="Trạng thái hiển thị")]
        public bool IsActive { get; set; } = true;
        [Display(Name ="Ngày cập nhật")]
        public DateTime? ModifiedDate {  get; set; }
        [Display(Name = "Người cập nhật")]
        public string? ModifiedBy { get; set; }
        [Display(Name ="Xóa")]
        public bool IsDelete { get; set; } = false;
        [Display(Name = "Người xóa")]
        public string? DeleteBy { get; set; }
        [Display(Name = "Ngày xóa")]
        public DateTime DeleteDate {  get; set; }
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
