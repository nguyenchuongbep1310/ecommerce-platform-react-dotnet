using Shared.Messages.Events;

namespace Shared.Messages.Commands
{
    public interface IReserveStockCommand
    {
        Guid OrderId { get; }
        List<OrderItemDto> Items { get; }
    }

    public interface IReleaseStockCommand
    {
        Guid OrderId { get; }
        List<OrderItemDto> Items { get; }
    }

    public interface IProcessPaymentCommand
    {
        Guid OrderId { get; }
        string UserId { get; }
        decimal Amount { get; }
        string PaymentMethodId { get; }
    }

    public interface ICancelOrderCommand
    {
        Guid OrderId { get; }
        string Reason { get; }
    }

    public interface ICompleteOrderCommand
    {
        Guid OrderId { get; }
    }

    public class ReserveStockCommand : IReserveStockCommand
    {
        public Guid OrderId { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class ReleaseStockCommand : IReleaseStockCommand
    {
        public Guid OrderId { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class ProcessPaymentCommand : IProcessPaymentCommand
    {
        public Guid OrderId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethodId { get; set; } = string.Empty;
    }

    public class CancelOrderCommand : ICancelOrderCommand
    {
        public Guid OrderId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class CompleteOrderCommand : ICompleteOrderCommand
    {
        public Guid OrderId { get; set; }
    }
}
