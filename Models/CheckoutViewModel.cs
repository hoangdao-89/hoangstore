using System.Collections.Generic;

namespace hoangstore.Models
{
    public class CheckoutViewModel
    {
        // Danh sach cac mat hang khach da tich chon mua de hien thi tom tat hoa don
        public List<CartItem> SelectedItems { get; set; } = new List<CartItem>();

        // Tong tien thuc te cua cac mat hang duoc chon mua
        public decimal TotalOrderPrice { get; set; }

        // Chuoi ID cac muc gio hang dang dang "1,2,3" gui qua URL
        public string SelectedCartItemIds { get; set; } = string.Empty;
    }
}