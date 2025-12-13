using ProductCatalogService.Models;

namespace ProductCatalogService.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ProductDbContext context)
        {
            if (context.Products.Any())
            {
                return; // DB has been seeded
            }

            var products = new Product[]
            {
                new Product { Name = "iPhone 15", Description = "Latest Apple iPhone", Price = 999.99m, StockQuantity = 100 },
                new Product { Name = "Samsung Galaxy S24", Description = "Flagship Samsung", Price = 899.99m, StockQuantity = 100 },
                new Product { Name = "Sony WH-1000XM5", Description = "Noise canceling headphones", Price = 349.99m, StockQuantity = 50 },
                new Product { Name = "MacBook Pro M3", Description = "Powerhouse laptop", Price = 1999.99m, StockQuantity = 20 }
            };

            foreach (var p in products)
            {
                context.Products.Add(p);
            }

            context.SaveChanges();
        }
    }
}
