/// <summary>
/// Registro de acerto ou erro ao aplicar o aumento de CreditLine num Business Partner.
/// </summary>
public class AumentoCreditLineResultado
{
    public string CardCode { get; set; } = string.Empty;
    public bool Sucesso { get; set; }
    public double CreditLimitAnterior { get; set; }
    public double CreditLimitNovo { get; set; }
    public string Detalhe { get; set; } = string.Empty;
    public DateTime DataHora { get; set; } = DateTime.Now;
}
