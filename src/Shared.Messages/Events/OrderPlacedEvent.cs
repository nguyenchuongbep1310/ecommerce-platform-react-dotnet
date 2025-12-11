namespace Shared.Messages.Events
{
    public record OrderPlacedEvent
    {
        public int OrderId { get; init; }
        public string UserId { get; init; } = string.Empty;
        public DateTime OrderDate { get; init; }
        public decimal TotalAmount { get; init; }
        public IEnumerable<OrderItemEvent> Items { get; init; } = Enumerable.Empty<OrderItemEvent>();
    }

    public record OrderItemEvent
    {
        public int ProductId { get; init; }
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
    }
}