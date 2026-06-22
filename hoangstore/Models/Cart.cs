using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hoangstore.Models
{
    public class Cart
    {
        [Key]
        [Display(Name ="Mã giỏ hàng")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required(ErrorMessage = "Mã người dùng không được bỏ trống")]
        [Display(Name = "Mã người dùng")]
        public string UserId { get; set; } = null!;
        public DateTime CreatedAt { get; set; }= DateTime.Now;
        public DateTime UpdatedAt {  get; set; }= DateTime.Now;
    }
}
