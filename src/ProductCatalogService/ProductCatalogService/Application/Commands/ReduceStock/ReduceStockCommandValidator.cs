using FluentValidation;

namespace ProductCatalogService.Application.Commands.ReduceStock;

public class ReduceStockCommandValidator : AbstractValidator<ReduceStockCommand>
{
    public ReduceStockCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0)
            .WithMessage("Product ID must be greater than 0");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(10000)
            .WithMessage("Quantity cannot exceed 10,000 units per transaction");
    }
}
