# Aumentar Limite Parceiro

Ferramenta Windows Forms que aplica um aumento percentual no **CreditLine** dos
parceiros de negócio do SAP Business One, via **Service Layer** ([B1SLayer](https://github.com/Rodringo/B1SLayer)).

Seleciona os clientes elegíveis, mostra o valor atual e o calculado, e grava o
aumento apenas nos que você marcar — com log de auditoria em CSV.

## Quem entra na lista

Clientes **ativos** e **adimplentes**:

- `Valid eq 'tYES'`, `Frozen eq 'tNO'`, `CardType eq 'cCustomer'` e `CreditLimit gt 0`
- sem nenhuma fatura de venda vencida em aberto

O corte por "vencida" (`DocDueDate` no passado) é feito em C#, porque o formato de
literal de data no Service Layer varia por versão e localização.

## Como usar

1. **Conectar e Consultar** — autentica e traz os clientes elegíveis.
2. Informe o **percentual** e clique em **Calcular** para ver o valor novo na grade.
   Nada é gravado nesta etapa.
3. Marque os clientes (ou **Marcar Todos**).
4. Desmarque **Modo simulação** — enquanto ele estiver marcado, o botão
   **Aplicar Aumento** fica desabilitado. É uma trava deliberada.
5. **Aplicar Aumento**, confirme, e informe **seu usuário e senha do SAP** na tela
   de login que aparece.

A faixa no topo da janela mostra sempre o **servidor** e a **base de dados** em uso.
Quando o `CompanyDB` contém `PRD`, ela fica vermelha com o aviso `PRODUCAO`.

### Duas contas, de propósito

| Etapa | Conta |
|---|---|
| Consultar os elegíveis | Conta de serviço do `appsettings.json` |
| Gravar o aumento | **Conta do SAP de quem está aplicando**, pedida na hora |

A gravação não usa a conta de serviço. Assim o SAP registra em `OCRD`
(`UpdateDate` / `UserSign`) quem alterou cada parceiro, em vez de atribuir tudo a
um usuário genérico — e o CSV guarda a mesma informação na coluna `UsuarioSAP`.

A senha digitada existe apenas em memória enquanto a conexão é criada: não é
gravada em arquivo nem no log. A sessão aberta com ela é encerrada
(`LogoutAsync`) ao final, inclusive quando a aplicação falha no meio — a Service
Layer tem limite de sessões simultâneas.

### MaxCommitment

Junto com o `CreditLimit`, o `MaxCommitment` é igualado ao novo valor. Sem isso o
SAP recusa a gravação com *"O limite de compromisso deve ser maior que o limite de
crédito"*. No Service Layer a propriedade se chama `MaxCommitment`, igual ao DI API
(o campo físico é `OCRD.DebtLine`).

### O aumento é cumulativo

A ferramenta aplica o percentual sobre o valor **atual** do SAP. Rodar duas vezes
com 20% resulta em 44%, não 40% — ela não marca nem reconhece quem já recebeu
aumento. Confira o log antes de repetir uma execução.

## Configuração

O `appsettings.json` **não está no repositório** por conter credencial. Copie o
modelo e preencha:

```bash
cp appsettings.example.json appsettings.json
```

| Chave | Descrição |
|---|---|
| `ServiceLayer:BaseUrl` | URL da Service Layer, ex.: `https://servidor:50000/b1s/v1/` |
| `ServiceLayer:CompanyDB` | Base da empresa |
| `ServiceLayer:UserName` | Usuário do SAP |
| `ServiceLayer:Password` | Senha — pode ficar vazia e vir da variável `ServiceLayer__Password` (dois underlines) |
| `Ajuste:ModoSimulacaoPadrao` | Abre a tela com a trava de simulação marcada |
| `Ajuste:PastaLogs` | Pasta dos logs, relativa ao executável |

## Log de auditoria

Um CSV por execução, em `<pasta do exe>\Logs\Log_AumentoCreditLine_<data>_<hora>.csv`:

```
DataHora,Servidor,CompanyDB,UsuarioSAP,Maquina,UsuarioWindows,IP,CardCode,Percentual,
CreditLimitAnterior,CreditLimitNovo,ModoSimulacao,Status,Detalhe
```

As colunas de origem (servidor, máquina, usuário do Windows, IP) são repetidas em
cada linha para que arquivos coletados de estações diferentes possam ser juntados
sem perder de onde veio cada registro.

`Status` é `OK` ou `ERRO` — e significa apenas que a requisição não lançou exceção.

## Build

```bash
dotnet build -c Debug
```

.NET 10 (`net10.0-windows`), Windows Forms. A configuração **Release** dispara o
Obfuscar no post-build (`_Obfuscar\Obfuscar.Console.exe`).

## Estrutura

```
Program.cs                            composition root (DI) e entrada
FormPrincipal.cs                      tela principal
FormLogin.cs                          login do SAP, pedido ao aplicar
AmbienteExecucao.cs                   servidor, máquina, usuário e IP
CompanyLayer.cs                       seção "ServiceLayer" do appsettings
AjusteSettings.cs                     seção "Ajuste" do appsettings
ServiceLayerConnectionFactory.cs      conexão B1SLayer, com validação de login
BusinessPartnerCreditoQueryService.cs consulta dos elegíveis
AumentoCreditLineService.cs           cálculo, gravação e log
BusinessPartnerCredito.cs             modelo da grade
InvoiceAbertaDto.cs                   faturas em aberto (checagem de adimplência)
AumentoCreditLineResultado.cs         resultado por parceiro
```
