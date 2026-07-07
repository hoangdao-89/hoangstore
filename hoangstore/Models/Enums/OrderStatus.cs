namespace hoangstore.Models.Enums
{
    public enum OrderStatus
    {
        Pending = 1,    // Chờ xử lý
        Shipping = 2,   // Đang giao
        Delivered = 3,  // Đã giao
        Cancelled = 4   // Đã hủy
    }
}
