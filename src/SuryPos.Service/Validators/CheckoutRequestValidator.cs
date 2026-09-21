using FluentValidation;
using SuryPos.Domain.Interfaces;
using SuryPos.Service.DTOs;

namespace SuryPos.Service.Validators;

public class CheckoutRequestValidator : AbstractValidator<CheckoutRequestDto>
{
    public CheckoutRequestValidator(IValidator<CheckoutItemDto> itemValidator)
    {
        RuleFor(x => x.Items)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Daftar item belanja wajib diisi.")
            .NotEmpty().WithMessage("Keranjang belanja tidak boleh kosong.");

        RuleForEach(x => x.Items).SetValidator(itemValidator);
    }
}

public class CheckoutItemDtoValidator : AbstractValidator<CheckoutItemDto>
{
    public CheckoutItemDtoValidator(IProductRepository productRepo)
    {
        RuleFor(i => i.ProductId)
            .NotEmpty().WithMessage("Product ID wajib diisi.")
            .Must(id => productRepo.GetById(id) is not null)
            .WithMessage(i => $"Produk dengan ID '{i.ProductId}' tidak ditemukan!");

        RuleFor(i => i.Quantity)
            .GreaterThan(0).WithMessage("Jumlah barang minimal 1 unit.")
            .Must((item, qty) =>
            {
                var product = productRepo.GetById(item.ProductId);
                return product is null || product.Stock >= qty;
            })
            .WithMessage(i =>
            {
                var product = productRepo.GetById(i.ProductId);
                return $"Stok produk '{product?.Name}' tidak mencukupi! Sisa stok: {product?.Stock}";
            });
    }
}
