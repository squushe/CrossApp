using Api.Contracts;
using FluentValidation;

namespace Api.Validation;

public sealed class SaveOrderRequestValidator : AbstractValidator<SaveOrderRequest>
{
    public SaveOrderRequestValidator()
    {
        RuleFor(x => x.Customer).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Клієнт обов'язковий.")
            .MaximumLength(100).WithMessage("Клієнт: до 100 символів.")
            .OverridePropertyName("customer");
        RuleFor(x => x.Lines).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Рядки замовлення обов'язкові.")
            .Must(lines => lines!.Count is >= 1 and <= 1000)
            .WithMessage("Замовлення має містити від 1 до 1000 рядків.")
            .OverridePropertyName("lines");
        When(x => x.Lines is { Count: >= 1 and <= 1000 }, () =>
        {
            RuleForEach(x => x.Lines!).NotNull().WithMessage("Рядок не може бути null.")
                .SetValidator(new SaveOrderLineRequestValidator())
                .OverridePropertyName("lines");
            RuleFor(x => x.Lines).Custom((lines, context) =>
            {
                try
                {
                    decimal total = 0;
                    foreach (var line in lines!)
                        if (line is not null && line.Price > 0 && line.Quantity > 0)
                            total += line.Price * line.Quantity;
                }
                catch (OverflowException)
                {
                    context.AddFailure("lines", "Сума замовлення надто велика.");
                }
            });
        });
    }
}

internal sealed class SaveOrderLineRequestValidator : AbstractValidator<SaveOrderLineRequest?>
{
    public SaveOrderLineRequestValidator()
    {
        RuleFor(x => x!.ProductId).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("ID товару обов'язковий.")
            .MaximumLength(100).WithMessage("ID товару: до 100 символів.")
            .OverridePropertyName("productId");
        RuleFor(x => x!.Name).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Назва обов'язкова.")
            .MaximumLength(200).WithMessage("Назва: до 200 символів.")
            .OverridePropertyName("name");
        RuleFor(x => x!.Price).GreaterThan(0).WithMessage("Ціна має бути більшою за нуль.")
            .OverridePropertyName("price");
        RuleFor(x => x!.Quantity).GreaterThan(0).WithMessage("Кількість має бути більшою за нуль.")
            .OverridePropertyName("quantity");
    }
}
