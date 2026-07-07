using hoangstore.Models.Enums;

namespace hoangstore.Helpers
{
    public static class EnumExtensions
    {
        public static string ToDisplayName(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "Chờ xử lý",
                OrderStatus.Shipping => "Đang giao",
                OrderStatus.Delivered => "Đã giao",
                OrderStatus.Cancelled => "Đã hủy",
                _ => "Không xác định"
            };
        }
    }
}