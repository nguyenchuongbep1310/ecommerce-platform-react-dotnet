namespace OrderService.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending"; // e.g., Pending, Processing, Shipped, Delivered
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}