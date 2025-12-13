namespace ShoppingCartService.DTOs
{
    public class AddItemRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}