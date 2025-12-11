namespace ShoppingCartService.DTOs
{
    public class CartResponseDto
    {
        public string UserId { get; set; } = string.Empty;
        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
        public decimal TotalPrice => Items.Sum(i => i.PriceAtAddition * i.Quantity);
    }

    public class CartItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal PriceAtAddition { get; set; }
        public string ProductName { get; set; } = "Unknown"; // We will populate this later
    }
}