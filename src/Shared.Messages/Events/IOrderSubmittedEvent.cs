namespace Shared.Messages.Events
{
    public interface IOrderSubmittedEvent
    {
        Guid OrderId { get; }
        string UserId { get; }
        decimal TotalAmount { get; }
        List<OrderItemDto> Items { get; }
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
