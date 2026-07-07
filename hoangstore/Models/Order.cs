using hoangstore.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hoangstore.Models;

public class Order
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [Required]
    public string UserId {  get; set; }
    [ForeignKey("UserId")]
    public ApplicationUser? User {  get; set; }
    [StringLength(100)]
    [Required(ErrorMessage = "Họ và tên người nhận không được để trống")]
    [Display(Name ="Họ và tên")]
    public string ReceiverName { get; set; } = null!;
    [Required(ErrorMessage = "Số điện thoại nhận hàng không được để trống")]
    [Phone]
    [StringLength(20)]
    [Display(Name = "Số điện thoại")]
    public string ReceiverPhone { get; set; } = null!;
    [Required(ErrorMessage = "Địa chỉ nhận hàng không được để trống")]
    [StringLength(500)]
    [Display(Name = "Địa chỉ nhận hàng")]
    public string ShippingAddress { get; set; } = null!;
    [Required(ErrorMessage = "Phương thức thanh toán không được để trống")]
    [StringLength(50)]
    [Display(Name = "Phương thức thanh toán")]
    public string PaymentMethod { get; set; } = "COD";
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Tổng giá tiền")]
    public decimal TotalPrice { get; set; }
    [Required]
    [Display(Name = "Trạng thái")]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
