using FluentValidation;
using SuryPos.Service.DTOs;

namespace SuryPos.Service.Validators;

public class CheckoutRequestValidator : AbstractValidator<CheckoutRequestDto>
{
    public CheckoutRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotNull().WithMessage("Daftar item belanja wajib diisi.")
            .NotEmpty().WithMessage("Keranjang belanja tidak boleh kosong.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("Product ID wajib diisi.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Jumlah barang minimal 1 unit.");
        });
    }
}