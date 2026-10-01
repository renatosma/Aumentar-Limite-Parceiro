/// <summary>
/// Fatura de venda (Invoices no Service Layer / OINV no SAP) em aberto, usada so pra
/// verificar inadimplencia (titulo vencido e ainda nao pago). Nao e persistida/exibida.
/// </summary>
public class InvoiceAbertaDto
{
    public string CardCode { get; set; } = string.Empty;
    public DateTime? DocDueDate { get; set; }
}
