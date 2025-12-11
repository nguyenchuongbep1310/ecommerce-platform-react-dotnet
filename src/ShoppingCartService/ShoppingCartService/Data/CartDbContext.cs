using Microsoft.EntityFrameworkCore;
using ShoppingCartService.Models;

namespace ShoppingCartService.Data
{
    public class CartDbContext : DbContext
    {
        public CartDbContext(DbContextOptions<CartDbContext> options)
            : base(options)
        {
        }

        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Define the primary key for Cart
            modelBuilder.Entity<Cart>()
                .HasKey(c => c.UserId);

            // Configure the relationship between Cart and CartItem
            modelBuilder.Entity<CartItem>()
                .HasOne<Cart>()
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CartId);
        }
    }
}