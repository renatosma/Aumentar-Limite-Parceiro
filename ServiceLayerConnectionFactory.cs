using B1SLayer;
using Microsoft.Extensions.Options;

/// <summary>
/// Monta a conexao com a Service Layer (B1SLayer) a partir do CompanyLayer (injetado via
/// Options/DI, alimentado pela secao "ServiceLayer" do appsettings.json) e valida o login
/// antes de devolve-la pronta pra uso.
///
/// Ha dois caminhos: a conta de servico do appsettings, usada na consulta, e uma conta
/// informada na tela, usada na gravacao — assim o SAP registra quem alterou cada parceiro.
/// </summary>
public class ServiceLayerConnectionFactory
{
    private readonly CompanyLayer _settings;

    public ServiceLayerConnectionFactory(IOptions<CompanyLayer> options)
    {
        _settings = options.Value;
    }

    public CompanyLayer Settings => _settings;

    /// <summary>Conexao com a conta de servico do appsettings.json.</summary>
    public Task<SLConnection> CriarAsync()
    {
        if (string.IsNullOrEmpty(_settings.UserName))
            throw new InvalidOperationException("ServiceLayer:UserName ausente no appsettings.json.");
        if (string.IsNullOrEmpty(_settings.Password))
            throw new InvalidOperationException(
                "ServiceLayer:Password ausente. Defina no appsettings.json ou na variavel de " +
                "ambiente ServiceLayer__Password (dois underlines).");

        return CriarAsync(_settings.UserName, _settings.Password);
    }

    /// <summary>
    /// Conexao com as credenciais informadas. Quem chamar e responsavel por encerrar a
    /// sessao com LogoutAsync: a Service Layer tem limite de sessoes simultaneas.
    /// </summary>
    public async Task<SLConnection> CriarAsync(string usuario, string senha)
    {
        if (string.IsNullOrEmpty(_settings.BaseUrl))
            throw new InvalidOperationException("ServiceLayer:BaseUrl ausente no appsettings.json.");
        if (string.IsNullOrEmpty(_settings.CompanyDB))
            throw new InvalidOperationException("ServiceLayer:CompanyDB ausente no appsettings.json.");
        if (string.IsNullOrWhiteSpace(usuario))
            throw new InvalidOperationException("Informe o usuario do SAP.");
        if (string.IsNullOrEmpty(senha))
            throw new InvalidOperationException("Informe a senha do SAP.");

        var sl = new SLConnection(_settings.BaseUrl, _settings.CompanyDB, usuario, senha);

        // Valida a autenticacao antes de liberar a conexao. O erro, quando a credencial
        // esta errada, vem do proprio login — nao de uma consulta que falha depois.
        await sl.LoginAsync();

        return sl;
    }
}
