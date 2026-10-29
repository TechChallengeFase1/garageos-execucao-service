using GarageOS.Execucao.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Execucao.Infrastructure.Mappings;

public class ItemExecucaoConfiguration : IEntityTypeConfiguration<ItemExecucao>
{
    public void Configure(EntityTypeBuilder<ItemExecucao> builder)
    {
        builder.ToTable("ItensExecucao");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        // Serviço do catálogo do OS Service: ID e nome copiados, sem FK.
        builder.Property(i => i.ServicoId)
            .IsRequired();

        builder.Property(i => i.Nome)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.CriadoEm)
            .IsRequired();

        builder.Property(i => i.IniciadaEm);

        builder.Property(i => i.FinalizadaEm);

        // O aging filtra por itens finalizados e agrupa por serviço.
        builder.HasIndex(i => new { i.ServicoId, i.FinalizadaEm });
    }
}
