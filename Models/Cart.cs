using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hoangstore.Models
{
    public class Cart
    {
        [Key]
        [Display(Name = "Mã giỏ hàng")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // BỔ SUNG DÒNG NÀY: Mối quan hệ điều hướng (Navigation Property)
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}