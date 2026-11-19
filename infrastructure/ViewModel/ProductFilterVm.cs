using Domin.Entity;

namespace Infrastructure.ViewModel
{
    public class ProductFilterVm
    {
        public string? SearchTerm { get; set; }
        public int? ProductTypeId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool InStockOnly { get; set; }
        public string SortBy { get; set; } = "newest"; // newest, price_asc, price_desc, name_asc
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;

        // Results
        public List<Product> Products { get; set; } = new();
        public List<ProductType> Categories { get; set; } = new();
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        public decimal PriceFloor { get; set; }
        public decimal PriceCeiling { get; set; }
    }
}
