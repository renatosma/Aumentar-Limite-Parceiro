/// <summary>
/// Dados de conexao com a Service Layer, lidos da secao "ServiceLayer" do appsettings.json.
/// A senha pode ser sobreposta pela variavel de ambiente ServiceLayer__Password.
/// </summary>
public class CompanyLayer
{
    public string BaseUrl { get; set; } = string.Empty;
    public string CompanyDB { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Password { get; set; }
}
