namespace GarageOS.Execucao.Application.DTOs.Estoques;

/// <summary>Item de estoque</summary>
/// <remarks>
/// É também o contrato que o OS Service consome em GET /api/estoques/{id} para
/// validar e precificar uma peça. Renomear campos aqui quebra o OS.
/// </remarks>
public class EstoqueResponse
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public int QuantidadeReservada { get; set; }
    public int QuantidadeDisponivel { get; set; }
    public decimal Valor { get; set; }
    public DateTime DataEntrada { get; set; }
    public DateTime? DataSaida { get; set; }
    public string Fornecedor { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
