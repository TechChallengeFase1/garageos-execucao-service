using GarageOS.Execucao.Infrastructure.Data;
using GarageOS.Execucao.IntegrationTests.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace GarageOS.Execucao.IntegrationTests.Fixtures;

public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _pgContainer = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("garageos_execucao_tests")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Sem SQS nos testes: o MassTransit roda em memória, com o outbox de
        // verdade no PostgreSQL do container.
        builder.UseSetting("Mensageria:Transporte", "InMemory");

        builder.ConfigureServices(services =>
        {
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ExecucaoDbContext>) ||
                    d.ServiceType == typeof(ExecucaoDbContext) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition().FullName != null &&
                     d.ServiceType.GetGenericTypeDefinition().FullName!
                         .Contains("IDbContextOptionsConfiguration")))
                .ToList();

            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            services.AddDbContext<ExecucaoDbContext>(options =>
                options.UseNpgsql(_pgContainer.GetConnectionString()));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                "Test", options => { });
        });
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _pgContainer.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExecucaoDbContext>();
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _pgContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
