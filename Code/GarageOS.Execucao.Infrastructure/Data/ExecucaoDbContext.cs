using System.Reflection;
using GarageOS.Execucao.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Execucao.Infrastructure.Data;

/// <summary>Banco próprio do Execução Service</summary>
/// <remarks>
/// Nenhuma tabela daqui aponta para tabela de outro serviço. Ordem de serviço,
/// serviço do catálogo e cliente aparecem só como ID (e nome, quando precisa
/// ser exibido), copiados no momento em que o pedido chega.
/// </remarks>
public class ExecucaoDbContext : DbContext
{
    public ExecucaoDbContext(DbContextOptions<ExecucaoDbContext> options) : base(options) { }

    public DbSet<Estoque> Estoques { get; set; }
    public DbSet<ReservaPecas> ReservasPecas { get; set; }
    public DbSet<ItemReserva> ItensReserva { get; set; }
    public DbSet<OrdemDeExecucao> OrdensDeExecucao { get; set; }
    public DbSet<ItemExecucao> ItensExecucao { get; set; }
    public DbSet<HistoricoExecucao> HistoricoExecucao { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Outbox e inbox do MassTransit no mesmo banco: a mudança e a mensagem
        // a publicar vão no mesmo commit.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        base.OnModelCreating(modelBuilder);
    }
}
