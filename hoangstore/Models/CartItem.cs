
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hoangstore.Models;

public class CartItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int CartId { get; set; }
    [ForeignKey("CartId")]
    public Cart? Cart { get; set; }
    public int ProductVariantId { get; set; }
    [ForeignKey("ProductVariantId")]
    public ProductVariant? ProductVariant { get; set; }

    [Required(ErrorMessage = "Số lượng không được để trống")]
    [Range(1, 100, ErrorMessage = "Số lượng đặt mua phải nằm trong khoảng từ 1 đến 100")]
    public int Quantity { get; set; }
}