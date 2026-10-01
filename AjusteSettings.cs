/// <summary>
/// Configuracao do ajuste de CreditLine, lida da secao "Ajuste" do appsettings.json.
/// </summary>
public class AjusteSettings
{
    /// <summary>
    /// Se true, a tela abre com "Modo simulacao" marcado por padrao: o aumento e
    /// calculado e exibido, mas nao chega a gravar no SAP. O usuario ainda pode
    /// desmarcar na tela antes de aplicar de verdade.
    /// </summary>
    public bool ModoSimulacaoPadrao { get; set; } = true;

    /// <summary>Pasta (relativa ao executavel) onde os logs de auditoria em CSV sao gravados.</summary>
    public string PastaLogs { get; set; } = "Logs";
}
