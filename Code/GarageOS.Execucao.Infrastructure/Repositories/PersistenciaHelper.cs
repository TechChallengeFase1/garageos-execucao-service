using GarageOS.Execucao.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GarageOS.Execucao.Infrastructure.Repositories;

/// <summary>Traduz falhas de gravação do EF em exceções do domínio</summary>
internal static class PersistenciaHelper
{
    // Código do PostgreSQL para violação de índice único.
    private const string ViolacaoDeUnicidade = "23505";

    public static async Task SalvarAsync(this DbContext context, string operacao)
    {
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflitoDeConcorrenciaException(
                $"{operacao}: o registro foi alterado por outra requisição ao mesmo tempo. Tente novamente.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: ViolacaoDeUnicidade })
        {
            throw new ConflitoDeConcorrenciaException(
                $"{operacao}: já existe um registro para esta ordem de serviço.", ex);
        }
    }
}
