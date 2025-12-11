namespace OrderService.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; } // FK
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        // public string ProductName { get; set; } // Can be stored for historical record

        public Order Order { get; set; } = null!;
    }
}