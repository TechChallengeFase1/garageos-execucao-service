using GarageOS.Execucao.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Execucao.Infrastructure.Mappings;

public class HistoricoExecucaoConfiguration : IEntityTypeConfiguration<HistoricoExecucao>
{
    public void Configure(EntityTypeBuilder<HistoricoExecucao> builder)
    {
        builder.ToTable("HistoricoExecucao");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .ValueGeneratedNever();

        builder.Property(h => h.StatusAnterior)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(h => h.StatusNovo)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(h => h.Motivo)
            .HasMaxLength(500);

        builder.Property(h => h.OcorridoEm)
            .IsRequired();
    }
}
