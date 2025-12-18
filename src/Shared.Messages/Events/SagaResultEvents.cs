namespace Shared.Messages.Events
{
    public interface IStockReservedEvent
    {
        Guid OrderId { get; }
    }

    public interface IStockReservationFailedEvent
    {
        Guid OrderId { get; }
        string Reason { get; }
    }

    public interface IPaymentCompletedEvent
    {
        Guid OrderId { get; }
        string TransactionId { get; }
    }

    public interface IPaymentFailedEvent
    {
        Guid OrderId { get; }
        string Reason { get; }
    }
}
