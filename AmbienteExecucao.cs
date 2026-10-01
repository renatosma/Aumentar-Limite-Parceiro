using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

/// <summary>
/// Identifica onde a execucao rodou: servidor da Service Layer, maquina, usuario do
/// Windows e IP. Vai em colunas do log de auditoria para que um CSV coletado de outra
/// estacao continue dizendo de onde veio — e e usado tambem na faixa de ambiente da tela.
///
/// Os valores sao lidos uma vez, no construtor: nao mudam durante a execucao.
/// Nenhuma leitura pode lancar excecao; o que falhar vira "?".
/// </summary>
public sealed class AmbienteExecucao
{
    public AmbienteExecucao(string? baseUrl)
    {
        Servidor = FormatarServidor(baseUrl);
        Maquina = Seguro(() => Environment.MachineName);
        UsuarioWindows = Seguro(FormatarUsuarioWindows);
        Ip = Seguro(EnderecoLocal);
    }

    /// <summary>Servidor da Service Layer no formato "host:porta".</summary>
    public string Servidor { get; }

    public string Maquina { get; }

    /// <summary>Usuario do Windows no formato "DOMINIO\usuario".</summary>
    public string UsuarioWindows { get; }

    /// <summary>IPv4 da estacao. Com mais de uma placa, o primeiro endereco valido.</summary>
    public string Ip { get; }

    /// <summary>
    /// Extrai "host:porta" da BaseUrl da Service Layer, descartando o caminho.
    /// Uma URL fora do padrao e devolvida como esta, em vez de virar texto vazio.
    /// </summary>
    public static string FormatarServidor(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return "(nao configurado)";
        }

        // O Host vazio precisa ser testado: "srv-sap:50000" (sem esquema) e aceito pelo
        // TryCreate, que le "srv-sap" como esquema e deixa o Host em branco.
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host))
        {
            return uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
        }

        return baseUrl;
    }

    private static string FormatarUsuarioWindows()
    {
        var dominio = Environment.UserDomainName;
        var usuario = Environment.UserName;

        return string.IsNullOrEmpty(dominio) ? usuario : $"{dominio}\\{usuario}";
    }

    /// <summary>Primeiro IPv4 util da estacao, sem loopback e sem APIPA (169.254.x.x).</summary>
    private static string EnderecoLocal()
    {
        foreach (var adaptador in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (adaptador.OperationalStatus != OperationalStatus.Up ||
                adaptador.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            foreach (var info in adaptador.GetIPProperties().UnicastAddresses)
            {
                if (EhEnderecoUtil(info.Address))
                {
                    return info.Address.ToString();
                }
            }
        }

        // Reserva, caso a enumeracao de placas nao devolva nada util.
        foreach (var endereco in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (EhEnderecoUtil(endereco))
            {
                return endereco.ToString();
            }
        }

        return "?";
    }

    private static bool EhEnderecoUtil(IPAddress endereco)
    {
        if (endereco.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(endereco))
        {
            return false;
        }

        var bytes = endereco.GetAddressBytes();

        // APIPA: placa sem DHCP, nao identifica a estacao na rede.
        return !(bytes[0] == 169 && bytes[1] == 254);
    }

    private static string Seguro(Func<string> leitura)
    {
        try
        {
            var valor = leitura();
            return string.IsNullOrWhiteSpace(valor) ? "?" : valor.Trim();
        }
        catch
        {
            return "?";
        }
    }
}
