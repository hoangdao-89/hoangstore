using hoangstore.Models.Enums;

namespace hoangstore.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<Product> FeaturedProducts { get; set; } = new();
        public List<Product> NewProducts { get; set; } = new();
        public List<Product> Products { get; set; } = new();

        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public ProductGender? Gender { get; set; }
        public string? Collection { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalProducts { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalProducts / (double)PageSize);

        public bool IsFiltered =>
            !string.IsNullOrWhiteSpace(SearchTerm) ||
            CategoryId.HasValue ||
            !string.IsNullOrWhiteSpace(CategoryName) ||
            Gender.HasValue ||
            !string.IsNullOrWhiteSpace(Collection);
    }
}
