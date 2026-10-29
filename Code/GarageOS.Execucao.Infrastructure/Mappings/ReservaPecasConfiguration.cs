using GarageOS.Execucao.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Execucao.Infrastructure.Mappings;

public class ReservaPecasConfiguration : IEntityTypeConfiguration<ReservaPecas>
{
    public void Configure(EntityTypeBuilder<ReservaPecas> builder)
    {
        builder.ToTable("ReservasPecas");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.OrdemDeServicoId)
            .IsRequired();

        // Uma reserva por OS. É o que sustenta a idempotência: duas requisições
        // simultâneas para a mesma OS não conseguem gravar duas reservas.
        builder.HasIndex(r => r.OrdemDeServicoId)
            .IsUnique();

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.CriadaEm)
            .IsRequired();

        builder.Property(r => r.AtualizadaEm)
            .IsRequired();

        builder.HasMany(r => r.Itens)
            .WithOne()
            .HasForeignKey(i => i.ReservaPecasId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(r => r.Itens)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
