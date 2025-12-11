namespace ShoppingCartService.Models
{
    public class Cart
    {
        // Primary key - using the User ID as the Cart Identifier for simplicity
        public string UserId { get; set; } = string.Empty; 

        // Navigation property to store cart items
        public List<CartItem> Items { get; set; } = new List<CartItem>();
    }
}