using Microsoft.EntityFrameworkCore;
using ProductCatalogService.Domain.Entities;

namespace ProductCatalogService.Infrastructure.Persistence
{
    public class ProductDbContext : DbContext
    {
        public ProductDbContext(DbContextOptions<ProductDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Seed initial data for quick testing
            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Laptop Pro", Description = "High-performance laptop.", Price = 1200.00m, StockQuantity = 50, Category = "Electronics" },
                new Product { Id = 2, Name = "Wireless Mouse", Description = "Ergonomic wireless mouse.", Price = 25.50m, StockQuantity = 200, Category = "Accessories" },
                new Product { Id = 3, Name = "Coffee Maker", Description = "12-cup automatic coffee maker.", Price = 75.99m, StockQuantity = 100, Category = "Home Goods" }
            );
        }
    }
}