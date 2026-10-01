using System.Text.Json.Serialization;

/// <summary>
/// Parceiro de negocio (BusinessPartners no Service Layer / OCRD no SAP) elegivel para o
/// aumento de CreditLine: ativo (Valid/Frozen) e sem faturas de venda vencidas em aberto.
/// </summary>
public class BusinessPartnerCredito
{
    public string CardCode { get; set; } = string.Empty;
    public string? CardName { get; set; }

    /// <summary>
    /// Limite de credito do parceiro. No Service Layer (b1s/OData) o campo se chama
    /// "CreditLimit" - so o nome DI API/COM da propriedade e "CreditLimit" tambem, mas o
    /// campo fisico na tabela OCRD e "CreditLine" (nome usado no DI API via Recordset/Query
    /// Manager, o que costuma gerar confusao). Via Service Layer, que e o que essa
    /// ferramenta usa, o nome correto e "CreditLimit".
    ///
    /// Nullable porque o Service Layer retorna null pra parceiros sem limite definido
    /// (o metadata marca CreditLimit como Nullable) - tratado como 0 no calculo/aplicacao.
    /// </summary>
    public double? CreditLimit { get; set; }

    /// <summary>Marcado pelo usuario na grid pra entrar (ou nao) no aumento. Nao vem do SAP.</summary>
    [JsonIgnore]
    public bool Selecionado { get; set; } = true;

    /// <summary>CreditLimit apos aplicar o percentual informado na tela. Recalculado a cada mudanca do percentual.</summary>
    [JsonIgnore]
    public double NovoCreditLimit { get; set; }

    /// <summary>Preenchido depois de rodar o aumento (OK / SIMULADO / ERRO / vazio se ainda nao processado).</summary>
    [JsonIgnore]
    public string Status { get; set; } = string.Empty;

    /// <summary>Detalhe do resultado do aumento pra esse parceiro.</summary>
    [JsonIgnore]
    public string Mensagem { get; set; } = string.Empty;
}
