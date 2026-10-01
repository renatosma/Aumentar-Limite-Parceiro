using B1SLayer;

/// <summary>
/// Consulta os Business Partners elegiveis para o aumento de CreditLine: clientes ativos
/// (Valid/Frozen) e adimplentes (sem fatura de venda vencida em aberto), via B1SLayer.
/// </summary>
public class BusinessPartnerCreditoQueryService
{
    // CreditLimit gt 0 já filtra no servidor quem não tem limite definido (null) ou tem 0 -
    // não faz sentido trazer pra tela quem não vai ganhar aumento nenhum (0 * qualquer% = 0).
    private const string FiltroClientesAtivos = "Valid eq 'tYES' and Frozen eq 'tNO' and CardType eq 'cCustomer' and CreditLimit gt 0";

    // So o status e filtrado no servidor (formato de literal de data no Service Layer varia
    // por versao/localizacao); o corte por "vencida" (DocDueDate no passado) e feito em C#.
    private const string FiltroFaturasAbertas = "DocumentStatus eq 'bost_Open'";

    private readonly ServiceLayerConnectionFactory _connectionFactory;
    private SLConnection? _sl;

    public BusinessPartnerCreditoQueryService(ServiceLayerConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public CompanyLayer Settings => _connectionFactory.Settings;

    public async Task<SLConnection> ObterConexaoAsync()
    {
        _sl ??= await _connectionFactory.CriarAsync();
        return _sl;
    }

    /// <summary>
    /// Retorna os clientes ativos, com CreditLimit maior que zero (quem nao tem limite
    /// definido nao entra - 0 ou null nao ganham aumento nenhum), que NAO tem nenhuma fatura
    /// de venda em aberto com DocDueDate anterior a hoje (premissa de "adimplente" confirmada
    /// com o usuario: sem titulos vencidos em aberto). Nao considera parcelas/boletos
    /// individuais nem outros tipos de documento (ex.: cheques, notas de debito) - so Faturas
    /// de venda.
    /// </summary>
    public async Task<List<BusinessPartnerCredito>> ConsultarElegiveisAsync()
    {
        var sl = await ObterConexaoAsync();

        var ativos = await sl.Request("BusinessPartners")
            .Select("CardCode,CardName,CreditLimit")
            .Filter(FiltroClientesAtivos)
            .OrderBy("CardCode")
            .GetAllAsync<BusinessPartnerCredito>();

        var faturasAbertas = await sl.Request("Invoices")
            .Select("CardCode,DocDueDate")
            .Filter(FiltroFaturasAbertas)
            .GetAllAsync<InvoiceAbertaDto>();

        var hoje = DateTime.Today;
        var inadimplentes = faturasAbertas
            .Where(f => f.DocDueDate is not null && f.DocDueDate.Value.Date < hoje)
            .Select(f => f.CardCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return ativos
            .Where(p => !inadimplentes.Contains(p.CardCode))
            .Where(p => (p.CreditLimit ?? 0) > 0) // reforço defensivo, caso o filtro do servidor não pegue algum null
            .ToList();
    }

    public async Task LogoutAsync()
    {
        if (_sl is not null)
        {
            await _sl.LogoutAsync();
        }
    }
}
