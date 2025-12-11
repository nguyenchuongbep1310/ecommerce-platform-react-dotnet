namespace ShoppingCartService.Models
{
    public class CartItem
    {
        public int Id { get; set; } // Primary Key for CartItem
        public string CartId { get; set; } = string.Empty; // Foreign key to Cart (UserId)
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal PriceAtAddition { get; set; } // Store price to prevent order manipulation if price changes later

        // Navigation property (handled by EF Core)
        // public Cart Cart { get; set; } 
    }
}