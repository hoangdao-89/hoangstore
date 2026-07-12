using hoangstore.Models.Enums;

namespace hoangstore.Helpers
{
    public static class EnumExtensions
    {
        public static string ToDisplayName(
            this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending =>
                    "Chờ xử lý",

                OrderStatus.Processing =>
                    "Đã thanh toán",

                OrderStatus.Shipping =>
                    "Đang giao",

                OrderStatus.Delivered =>
                    "Đã giao",

                OrderStatus.Cancelled =>
                    "Đã hủy",

                _ =>
                    "Không xác định"
            };
        }

        public static string ToBadgeClass(
            this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending =>
                    "bg-warning text-dark",

                OrderStatus.Processing =>
                    "bg-info text-dark",

                OrderStatus.Shipping =>
                    "bg-primary text-white",

                OrderStatus.Delivered =>
                    "bg-success text-white",

                OrderStatus.Cancelled =>
                    "bg-danger text-white",

                _ =>
                    "bg-secondary text-white"
            };
        }
    }
}