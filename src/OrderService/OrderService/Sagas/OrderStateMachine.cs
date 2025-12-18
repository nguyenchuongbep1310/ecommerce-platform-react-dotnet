using MassTransit;
using Shared.Messages.Events;
using Shared.Messages.Commands;

namespace OrderService.Sagas
{
    public class OrderStateMachine : MassTransitStateMachine<OrderState>
    {
        public OrderStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => OrderSubmitted, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => StockReserved, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => StockReservationFailed, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => PaymentCompleted, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => PaymentFailed, x => x.CorrelateById(m => m.Message.OrderId));

            Initially(
                When(OrderSubmitted)
                    .Then(context =>
                    {
                        context.Saga.UserId = context.Message.UserId;
                        context.Saga.TotalAmount = context.Message.TotalAmount;
                        context.Saga.CreatedAt = DateTime.UtcNow;
                    })
                    .TransitionTo(Submitted)
                    .Publish(context => new ReserveStockCommand 
                    {
                        OrderId = context.Saga.CorrelationId,
                        Items = context.Message.Items
                    })
            );

            During(Submitted,
                When(StockReserved)
                    .TransitionTo(StockReservedState)
                    .Publish(context => new ProcessPaymentCommand 
                    {
                        OrderId = context.Saga.CorrelationId,
                        UserId = context.Saga.UserId,
                        Amount = context.Saga.TotalAmount,
                        PaymentMethodId = "pm_card_visa"
                    }),
                When(StockReservationFailed)
                    .Then(context => context.Saga.FailureReason = context.Message.Reason)
                    .TransitionTo(Failed)
                    .Publish(context => new CancelOrderCommand
                    {
                        OrderId = context.Saga.CorrelationId,
                        Reason = context.Message.Reason
                    })
            );

            During(StockReservedState,
                When(PaymentCompleted)
                    .TransitionTo(Completed)
                    .Publish(context => new CompleteOrderCommand { OrderId = context.Saga.CorrelationId }),
                When(PaymentFailed)
                    .Then(context => context.Saga.FailureReason = context.Message.Reason)
                    .TransitionTo(Failed)
                    .Publish(context => new CancelOrderCommand
                    {
                        OrderId = context.Saga.CorrelationId,
                        Reason = context.Message.Reason
                    })
            );
        }

        public State Submitted { get; private set; }
        public State StockReservedState { get; private set; }
        public State Completed { get; private set; }
        public State Failed { get; private set; }

        public Event<IOrderSubmittedEvent> OrderSubmitted { get; private set; }
        public Event<IStockReservedEvent> StockReserved { get; private set; }
        public Event<IStockReservationFailedEvent> StockReservationFailed { get; private set; }
        public Event<IPaymentCompletedEvent> PaymentCompleted { get; private set; }
        public Event<IPaymentFailedEvent> PaymentFailed { get; private set; }
    }
}
