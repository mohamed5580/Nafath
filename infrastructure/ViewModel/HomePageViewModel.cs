using Domin.Entity;

namespace Infrastructure.ViewModel
{
    public class HomePageViewModel
    {
        public List<ProductType> Categories { get; set; } = new();
        public List<Product> FeaturedProducts { get; set; } = new();
    }
}
