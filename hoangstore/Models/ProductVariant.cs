using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hoangstore.Models
{
    public class ProductVariant:IAuditable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [Required(ErrorMessage = "Size là bắt buộc"), StringLength(20)]
        [Display(Name = "Kích cỡ")]
        public string Size { get; set; } = null!;
        [Required(ErrorMessage = "Màu sắc là bắt buộc"), StringLength(50)]
        [Display(Name = "Màu sắc")]
        public string Color { get; set; } = null!;
        [Required(ErrorMessage = "Số lượng tồn kho là bắt buộc")]
        [Display(Name = "Số lượng tồn kho")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng không được âm")]
        public int Quantity { get; set; }
        [Required(ErrorMessage = "Giá tiền của biến thể là bắt buộc")]
        [Display(Name = "Giá tiền")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }
        [Display(Name = "Link ảnh riêng cho màu này")]
        public string? Variant_Image_Url { get; set; }
        //
        //
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public bool IsDeleted { get; set; } = false;

        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<OrderDetail> OrderDetails {  get; set; } = new List<OrderDetail>();
    }
}
