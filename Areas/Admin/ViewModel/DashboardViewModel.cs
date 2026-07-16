using hoangstore.Models;

namespace hoangstore.Areas.Admin.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ShippingOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalProducts { get; set; }
        public int LowStockCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal ThisMonthRevenue { get; set; }
        public decimal LastMonthRevenue { get; set; }
        public decimal RevenueGrowthPercent { get; set; }
        public List<string> WeekLabels { get; set; } = new();
        public List<decimal> WeekRevenue { get; set; } = new();
        public List<string> MonthLabels { get; set; } = new();
        public List<decimal> MonthRevenue { get; set; } = new();
        public List<string> YearLabels { get; set; } = new();
        public List<decimal> YearRevenue { get; set; } = new();
        public List<TopSellingProductViewModel> TopSellingProducts { get; set; } = new();
        public List<CategoryRevenueViewModel> CategoryRevenue { get; set; } = new();
        public List<PaymentStatisticViewModel> PaymentStatistics { get; set; } = new();
        public List<LowStockItemViewModel> LowStockItems { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();
    }

    public class TopSellingProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class CategoryRevenueViewModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class PaymentStatisticViewModel
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class LowStockItemViewModel
    {
        public int VariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class RevenueReportViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int DeliveredOrderCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<RevenueByDateViewModel> RevenueByDate { get; set; } = new();
        public List<TopSellingProductViewModel> TopSellingProducts { get; set; } = new();
    }

    public class RevenueByDateViewModel
    {
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }
}
