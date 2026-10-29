using GarageOS.Execucao.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Execucao.Infrastructure.Mappings;

public class ItemReservaConfiguration : IEntityTypeConfiguration<ItemReserva>
{
    public void Configure(EntityTypeBuilder<ItemReserva> builder)
    {
        builder.ToTable("ItensReserva");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        builder.Property(i => i.Quantidade)
            .IsRequired();

        // A peça é do próprio serviço, então aqui a chave estrangeira existe.
        builder.HasOne<Estoque>()
            .WithMany()
            .HasForeignKey(i => i.EstoqueId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
