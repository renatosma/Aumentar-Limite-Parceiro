using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;

/// <summary>
/// Calcula e aplica o aumento de CreditLine (percentual informado na tela) nos parceiros
/// selecionados, com log de auditoria em CSV. Em modo simulacao, calcula e loga sem gravar no SAP.
/// Junto com CreditLimit, tambem iguala o MaxCommitment (limite de comprometimento) ao novo
/// valor, senao o SAP recusa a gravacao quando CreditLimit fica maior que MaxCommitment.
/// </summary>
public class AumentoCreditLineService
{
    private readonly BusinessPartnerCreditoQueryService _queryService;
    private readonly AjusteSettings _ajusteSettings;

    public AumentoCreditLineService(BusinessPartnerCreditoQueryService queryService, IOptions<AjusteSettings> ajusteOptions)
    {
        _queryService = queryService;
        _ajusteSettings = ajusteOptions.Value;
    }

    /// <summary>CreditLimit com o percentual aplicado. CreditLimit 0 (sem limite no SAP) permanece 0.</summary>
    public static double CalcularNovoValor(double creditLimitAtual, decimal percentual)
        => creditLimitAtual * (1 + (double)percentual / 100);

    /// <param name="sl">
    /// Conexao autenticada com a conta de quem esta aplicando o aumento — nao a conta de
    /// servico usada na consulta. Encerrar a sessao e responsabilidade de quem chamou.
    /// </param>
    /// <param name="usuarioSap">Conta do SAP autenticada em <paramref name="sl"/>, registrada no log.</param>
    public async Task<List<AumentoCreditLineResultado>> AplicarAsync(
        B1SLayer.SLConnection sl,
        string usuarioSap,
        IEnumerable<BusinessPartnerCredito> selecionados,
        decimal percentual,
        bool modoSimulacao,
        Action<BusinessPartnerCredito, AumentoCreditLineResultado>? aoProcessarUm = null)
    {
        var companyDb = _queryService.Settings.CompanyDB;
        var lista = selecionados.ToList();
        var resultados = new List<AumentoCreditLineResultado>();

        // Lido uma vez: servidor, maquina, usuario e IP sao fixos durante a execucao,
        // e vao repetidos em cada linha para que o CSV identifique a origem sozinho.
        var ambiente = new AmbienteExecucao(_queryService.Settings.BaseUrl, usuarioSap);

        var pastaLogs = Path.Combine(AppContext.BaseDirectory, _ajusteSettings.PastaLogs);
        Directory.CreateDirectory(pastaLogs);
        var logPath = Path.Combine(pastaLogs, $"Log_AumentoCreditLine_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        using var logWriter = new StreamWriter(logPath, append: false, Encoding.UTF8) { AutoFlush = true };
        await logWriter.WriteLineAsync("DataHora,Servidor,CompanyDB,UsuarioSAP,Maquina,UsuarioWindows,IP,CardCode,Percentual,CreditLimitAnterior,CreditLimitNovo,ModoSimulacao,Status,Detalhe");

        foreach (var p in lista)
        {
            var resultado = await AplicarUmAsync(sl, companyDb, ambiente, p, percentual, modoSimulacao, logWriter);
            resultados.Add(resultado);
            aoProcessarUm?.Invoke(p, resultado);
        }

        return resultados;
    }

    private static async Task<AumentoCreditLineResultado> AplicarUmAsync(
        B1SLayer.SLConnection sl,
        string companyDb,
        AmbienteExecucao ambiente,
        BusinessPartnerCredito p,
        decimal percentual,
        bool modoSimulacao,
        StreamWriter logWriter)
    {
        var creditLimitAnterior = p.CreditLimit ?? 0;
        var novoValor = CalcularNovoValor(creditLimitAnterior, percentual);

        try
        {
            if (!modoSimulacao)
            {
                // SAP recusa CreditLimit > MaxCommitment ("O limite de compromisso deve ser
                // maior que o limite de credito."), entao MaxCommitment sobe junto, igualado
                // ao novo CreditLimit. No DI API a propriedade se chama MaxCommitment (campo
                // fisico OCRD.DebtLine); no Service Layer, que e o que essa ferramenta usa, o
                // nome da propriedade tambem e "MaxCommitment" (igual ao DI API, diferente do
                // nome do campo fisico).
                await sl.Request("BusinessPartners", p.CardCode).PatchAsync(new { CreditLimit = novoValor, MaxCommitment = novoValor });
            }

            var status = modoSimulacao ? "SIMULADO" : "OK";
            var detalhe = modoSimulacao
                ? $"Simulado: {creditLimitAnterior:N2} -> {novoValor:N2} ({percentual}%) - MaxCommitment tambem seria igualado a {novoValor:N2}"
                : $"Aplicado: {creditLimitAnterior:N2} -> {novoValor:N2} ({percentual}%) - MaxCommitment tambem igualado a {novoValor:N2}";

            p.Status = status;
            p.Mensagem = detalhe;
            p.NovoCreditLimit = novoValor;

            await logWriter.WriteLineAsync(FormatarLinhaCsv(companyDb, ambiente, p.CardCode, percentual, creditLimitAnterior, novoValor, modoSimulacao, status, detalhe));

            return new AumentoCreditLineResultado
            {
                CardCode = p.CardCode,
                Sucesso = true,
                CreditLimitAnterior = creditLimitAnterior,
                CreditLimitNovo = novoValor,
                Detalhe = detalhe
            };
        }
        catch (Exception ex)
        {
            p.Status = "ERRO";
            p.Mensagem = ex.Message;

            await logWriter.WriteLineAsync(FormatarLinhaCsv(companyDb, ambiente, p.CardCode, percentual, creditLimitAnterior, novoValor, modoSimulacao, "ERRO", ex.Message));

            return new AumentoCreditLineResultado
            {
                CardCode = p.CardCode,
                Sucesso = false,
                CreditLimitAnterior = creditLimitAnterior,
                CreditLimitNovo = novoValor,
                Detalhe = ex.Message
            };
        }
    }

    private static string FormatarLinhaCsv(string companyDb, AmbienteExecucao ambiente, string cardCode, decimal percentual, double anterior, double novo, bool modoSimulacao, string status, string detalhe)
    {
        return string.Join(",",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            CsvEscape(ambiente.Servidor),
            CsvEscape(companyDb),
            CsvEscape(ambiente.UsuarioSap),
            CsvEscape(ambiente.Maquina),
            CsvEscape(ambiente.UsuarioWindows),
            CsvEscape(ambiente.Ip),
            CsvEscape(cardCode),
            percentual.ToString(CultureInfo.InvariantCulture),
            anterior.ToString(CultureInfo.InvariantCulture),
            novo.ToString(CultureInfo.InvariantCulture),
            modoSimulacao,
            status,
            CsvEscape(detalhe));
    }

    private static string CsvEscape(string value)
    {
        value = value.Replace("\r", " ").Replace("\n", " ");
        if (value.Contains(',') || value.Contains('"'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }
}
