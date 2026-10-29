using GarageOS.Execucao.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Execucao.Infrastructure.Mappings;

public class EstoqueConfiguration : IEntityTypeConfiguration<Estoque>
{
    public void Configure(EntityTypeBuilder<Estoque> builder)
    {
        builder.ToTable("Estoques");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.Nome)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.Quantidade)
            .IsRequired();

        builder.Property(e => e.QuantidadeReservada)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.Valor)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(e => e.DataEntrada)
            .IsRequired();

        builder.Property(e => e.DataSaida);

        builder.Property(e => e.Fornecedor)
            .IsRequired()
            .HasMaxLength(150);

        // Texto, e não inteiro: um valor novo no meio do enum não muda o
        // significado do que já está gravado.
        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Ignore(e => e.QuantidadeDisponivel);

        // Token de concorrência: a coluna de sistema xmin do PostgreSQL.
        // Duas reservas simultâneas da mesma peça leram o mesmo saldo; a que
        // gravar por último falha em vez de sobrescrever a outra.
        builder.Property<uint>("Versao")
            .IsRowVersion();
    }
}
