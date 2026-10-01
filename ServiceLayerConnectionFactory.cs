using B1SLayer;
using Microsoft.Extensions.Options;

/// <summary>
/// Monta a conexao com a Service Layer (B1SLayer) a partir do CompanyLayer (injetado via
/// Options/DI, alimentado pela secao "ServiceLayer" do appsettings.json) e valida o login
/// antes de devolve-la pronta pra uso.
/// </summary>
public class ServiceLayerConnectionFactory
{
    private readonly CompanyLayer _settings;

    public ServiceLayerConnectionFactory(IOptions<CompanyLayer> options)
    {
        _settings = options.Value;
    }

    public CompanyLayer Settings => _settings;

    public async Task<SLConnection> CriarAsync()
    {
        if (string.IsNullOrEmpty(_settings.BaseUrl))
            throw new InvalidOperationException("ServiceLayer:BaseUrl ausente no appsettings.json.");
        if (string.IsNullOrEmpty(_settings.CompanyDB))
            throw new InvalidOperationException("ServiceLayer:CompanyDB ausente no appsettings.json.");
        if (string.IsNullOrEmpty(_settings.UserName))
            throw new InvalidOperationException("ServiceLayer:UserName ausente no appsettings.json.");
        if (string.IsNullOrEmpty(_settings.Password))
            throw new InvalidOperationException(
                "ServiceLayer:Password ausente. Defina no appsettings.json ou na variavel de " +
                "ambiente ServiceLayer__Password (dois underlines).");

        var sl = new SLConnection(_settings.BaseUrl, _settings.CompanyDB, _settings.UserName, _settings.Password);

        // Requisicao minima para validar login/autenticacao antes de liberar a conexao
        await sl.Request("BusinessPartners")
            .Select("CardCode")
            .Top(1)
            .GetAsync<List<BusinessPartnerCredito>>();

        return sl;
    }
}
