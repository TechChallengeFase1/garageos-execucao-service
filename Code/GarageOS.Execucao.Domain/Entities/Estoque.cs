using GarageOS.Execucao.Domain.Enums;
using GarageOS.Execucao.Domain.Exceptions;

namespace GarageOS.Execucao.Domain.Entities;

/// <summary>Item de estoque (peça ou insumo) usado na execução das ordens de serviço</summary>
/// <remarks>
/// Na Fase 4 a peça deixa de sair do estoque no momento da aprovação. Ela passa
/// por dois tempos, e é isso que permite compensar a saga:
///
///   1. Reservar  - a quantidade fica separada para uma OS, mas continua no estoque
///   2. Consumir  - a baixa definitiva, quando a execução termina
///
/// Se o pagamento falhar entre os dois, a reserva é liberada e nada foi perdido.
/// Se a execução falhar depois da baixa, as peças voltam com Devolver.
/// </remarks>
public class Estoque
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; } = null!;
    public int Quantidade { get; private set; }
    public int QuantidadeReservada { get; private set; }
    public decimal Valor { get; private set; }
    public DateTime DataEntrada { get; private set; }
    public DateTime? DataSaida { get; private set; }
    public string Fornecedor { get; private set; } = null!;
    public StatusEstoque Status { get; private set; }

    /// <summary>Quanto ainda pode ser reservado</summary>
    public int QuantidadeDisponivel => Quantidade - QuantidadeReservada;

    protected Estoque() { }

    public Estoque(string nome, int quantidade, decimal valor, DateTime dataEntrada, string fornecedor, DateTime? dataSaida = null)
    {
        Validar(nome, quantidade, valor, fornecedor);

        Id = Guid.NewGuid();
        Nome = nome;
        Quantidade = quantidade;
        Valor = valor;
        DataEntrada = dataEntrada;
        DataSaida = dataSaida;
        Fornecedor = fornecedor;
        AtualizarStatus();
    }

    /// <summary>Separa a quantidade para uma OS, sem tirá-la do estoque</summary>
    public void Reservar(int quantidade)
    {
        ValidarQuantidadePositiva(quantidade);

        if (quantidade > QuantidadeDisponivel)
            throw new EstoqueInsuficienteException(Id, Nome, QuantidadeDisponivel, quantidade);

        QuantidadeReservada += quantidade;
        AtualizarStatus();
    }

    /// <summary>Devolve ao disponível uma quantidade que estava reservada</summary>
    public void LiberarReserva(int quantidade)
    {
        ValidarQuantidadePositiva(quantidade);
        ValidarReservado(quantidade);

        QuantidadeReservada -= quantidade;
        AtualizarStatus();
    }

    /// <summary>Baixa definitiva de uma quantidade que estava reservada</summary>
    public void ConsumirReserva(int quantidade)
    {
        ValidarQuantidadePositiva(quantidade);
        ValidarReservado(quantidade);

        QuantidadeReservada -= quantidade;
        Quantidade -= quantidade;
        AtualizarStatus();
    }

    /// <summary>Repõe no estoque peças que já tinham sido baixadas (compensação)</summary>
    public void Devolver(int quantidade)
    {
        ValidarQuantidadePositiva(quantidade);

        Quantidade += quantidade;
        AtualizarStatus();
    }

    public void Atualizar(string nome, int quantidade, decimal valor, DateTime dataEntrada, string fornecedor, DateTime? dataSaida = null)
    {
        Validar(nome, quantidade, valor, fornecedor);

        if (quantidade < QuantidadeReservada)
            throw new ArgumentException(
                $"Quantidade não pode ficar abaixo do que está reservado ({QuantidadeReservada}).", nameof(quantidade));

        Nome = nome;
        Quantidade = quantidade;
        Valor = valor;
        DataEntrada = dataEntrada;
        DataSaida = dataSaida;
        Fornecedor = fornecedor;
        AtualizarStatus();
    }

    private void AtualizarStatus() =>
        Status = QuantidadeDisponivel > 0 ? StatusEstoque.Disponivel : StatusEstoque.Indisponivel;

    private void ValidarReservado(int quantidade)
    {
        if (quantidade > QuantidadeReservada)
            throw new InvalidOperationException(
                $"Não há {quantidade} unidade(s) reservada(s) de '{Nome}'. Reservado: {QuantidadeReservada}.");
    }

    private static void ValidarQuantidadePositiva(int quantidade)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantidade));
    }

    private static void Validar(string nome, int quantidade, decimal valor, string fornecedor)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome do item não pode ser vazio.", nameof(nome));

        if (quantidade < 0)
            throw new ArgumentException("Quantidade não pode ser negativa.", nameof(quantidade));

        if (valor <= 0)
            throw new ArgumentException("Valor deve ser maior que zero.", nameof(valor));

        if (string.IsNullOrWhiteSpace(fornecedor))
            throw new ArgumentException("Fornecedor não pode ser vazio.", nameof(fornecedor));
    }
}
