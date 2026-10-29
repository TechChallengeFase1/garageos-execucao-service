using GarageOS.Execucao.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Execucao.Infrastructure.Mappings;

public class OrdemDeExecucaoConfiguration : IEntityTypeConfiguration<OrdemDeExecucao>
{
    public void Configure(EntityTypeBuilder<OrdemDeExecucao> builder)
    {
        builder.ToTable("OrdensDeExecucao");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .ValueGeneratedNever();

        // Só o ID da OS: ela pertence ao OS Service, sem chave estrangeira.
        builder.Property(o => o.OrdemDeServicoId)
            .IsRequired();

        builder.HasIndex(o => o.OrdemDeServicoId)
            .IsUnique();

        builder.Property(o => o.NumeroOS)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(o => o.FilialId);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(o => o.Status);

        builder.Property(o => o.MotivoFalha)
            .HasMaxLength(500);

        builder.Property(o => o.CriadaEm)
            .IsRequired();

        builder.Property(o => o.AtualizadaEm)
            .IsRequired();

        builder.Property<uint>("Versao")
            .IsRowVersion();

        builder.HasMany(o => o.Itens)
            .WithOne()
            .HasForeignKey(i => i.OrdemDeExecucaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Historico)
            .WithOne()
            .HasForeignKey(h => h.OrdemDeExecucaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Itens)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(o => o.Historico)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
