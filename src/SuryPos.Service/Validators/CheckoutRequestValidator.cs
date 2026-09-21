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
            .GreaterThan(0).WithMessage("Product ID wajib diisi.");

        RuleFor(i => i.Quantity)
            .GreaterThan(0).WithMessage("Jumlah barang minimal 1 unit.");

        // Cek eksistensi + stok dalam satu query async per item,
        // supaya pesan error bisa memuat nama & sisa stok aktual.
        RuleFor(i => i).CustomAsync(async (item, ctx, ct) =>
        {
            if (item.ProductId <= 0 || item.Quantity <= 0)
                return; // Sudah ditangani rule di atas.

            var product = await productRepo.GetByIdAsync(item.ProductId, ct);
            if (product is null)
            {
                ctx.AddFailure("ProductId", $"Produk dengan ID '{item.ProductId}' tidak ditemukan!");
            }
            else if (product.Stock < item.Quantity)
            {
                ctx.AddFailure("Quantity",
                    $"Stok produk '{product.Name}' tidak mencukupi! Sisa stok: {product.Stock}");
            }
        });
    }
}
