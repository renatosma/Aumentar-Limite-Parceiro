using System.ComponentModel;
using Microsoft.Extensions.Options;

public class FormPrincipal : Form
{
    private readonly BusinessPartnerCreditoQueryService _queryService;
    private readonly AumentoCreditLineService _aumentoService;
    private readonly ServiceLayerConnectionFactory _connectionFactory;
    private readonly AjusteSettings _ajusteSettings;

    private readonly BindingList<BusinessPartnerCredito> _parceiros = new();
    private readonly BindingSource _bindingParceiros = new();

    private bool _carregando;

    private DataGridView dgvParceiros = null!;
    private Button btnConsultar = null!;
    private Button btnCalcular = null!;
    private Button btnMarcarTodos = null!;
    private Button btnDesmarcarTodos = null!;
    private Button btnAplicar = null!;
    private NumericUpDown nudPercentual = null!;
    private CheckBox chkModoSimulacao = null!;
    private Label lblAmbiente = null!;
    private Label lblStatus = null!;

    public FormPrincipal(BusinessPartnerCreditoQueryService queryService, AumentoCreditLineService aumentoService, ServiceLayerConnectionFactory connectionFactory, IOptions<AjusteSettings> ajusteOptions)
    {
        _queryService = queryService;
        _aumentoService = aumentoService;
        _connectionFactory = connectionFactory;
        _ajusteSettings = ajusteOptions.Value;

        MontarLayout();

        _bindingParceiros.DataSource = _parceiros;
        dgvParceiros.DataSource = _bindingParceiros;
    }

    private void MontarLayout()
    {
        Text = "Aumento de CreditLine - Clientes Ativos e Adimplentes";
        Width = 1100;
        Height = 720;
        StartPosition = FormStartPosition.CenterScreen;

        // Abaixo disso a barra de botoes comeca a quebrar em varias linhas e o grid
        // fica menor que a soma das larguras minimas das colunas.
        MinimumSize = new Size(960, 540);

        var painelTopo = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Padding = new Padding(8)
        };

        btnConsultar = new Button { Text = "Conectar e Consultar", AutoSize = true };
        btnConsultar.Click += BtnConsultar_Click;

        var lblPercentual = new Label { Text = "Percentual de aumento (%):", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(16, 10, 4, 0) };

        nudPercentual = new NumericUpDown
        {
            Minimum = -100,
            Maximum = 1000,
            DecimalPlaces = 2,
            Increment = 0.5m,
            Value = 0,
            Width = 90,
            Margin = new Padding(0, 6, 0, 0)
        };

        btnCalcular = new Button { Text = "Calcular", AutoSize = true, Enabled = false, Margin = new Padding(4, 6, 4, 0) };
        btnCalcular.Click += (s, e) => RecalcularTodos();

        chkModoSimulacao = new CheckBox
        {
            Text = "Modo simulacao (trava a gravacao no SAP)",
            AutoSize = true,
            Checked = _ajusteSettings.ModoSimulacaoPadrao,
            Margin = new Padding(16, 10, 4, 0)
        };
        // Trava de seguranca: so e possivel aplicar depois de desmarcar explicitamente.
        chkModoSimulacao.CheckedChanged += (s, e) => AtualizarBotaoAplicar();

        btnMarcarTodos = new Button { Text = "Marcar Todos", AutoSize = true, Enabled = false, Margin = new Padding(16, 6, 4, 0) };
        btnMarcarTodos.Click += (s, e) => MarcarTodos(true);

        btnDesmarcarTodos = new Button { Text = "Desmarcar Todos", AutoSize = true, Enabled = false };
        btnDesmarcarTodos.Click += (s, e) => MarcarTodos(false);

        btnAplicar = new Button { Text = "Aplicar Aumento", AutoSize = true, Enabled = false, Margin = new Padding(16, 6, 4, 0) };
        btnAplicar.Click += BtnAplicar_Click;

        painelTopo.Controls.Add(btnConsultar);
        painelTopo.Controls.Add(lblPercentual);
        painelTopo.Controls.Add(nudPercentual);
        painelTopo.Controls.Add(btnCalcular);
        painelTopo.Controls.Add(chkModoSimulacao);
        painelTopo.Controls.Add(btnMarcarTodos);
        painelTopo.Controls.Add(btnDesmarcarTodos);
        painelTopo.Controls.Add(btnAplicar);

        lblAmbiente = MontarRotuloAmbiente();

        lblStatus = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = "Nao conectado.",
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0)
        };

        dgvParceiros = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            ReadOnly = false
        };
        // Colunas de largura fixa (AutoSizeMode = None) mantem o tamanho ao redimensionar;
        // "Nome" e "Mensagem" sao Fill e dividem entre si todo o espaco que sobra, na
        // proporcao do FillWeight. Sem isso, maximizar a janela so aumentava a area vazia
        // a direita da ultima coluna.
        dgvParceiros.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(BusinessPartnerCredito.Selecionado), HeaderText = "Sel.", Width = 40, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        dgvParceiros.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BusinessPartnerCredito.CardCode), HeaderText = "CardCode", Width = 90, ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        dgvParceiros.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BusinessPartnerCredito.CardName), HeaderText = "Nome", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 40, MinimumWidth = 180 });
        dgvParceiros.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BusinessPartnerCredito.CreditLimit), HeaderText = "CreditLimit atual", Width = 120, ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
        dgvParceiros.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BusinessPartnerCredito.NovoCreditLimit), HeaderText = "CreditLimit novo", Width = 120, ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
        dgvParceiros.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BusinessPartnerCredito.Status), HeaderText = "Status", Width = 80, ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        dgvParceiros.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BusinessPartnerCredito.Mensagem), HeaderText = "Mensagem", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 60, MinimumWidth = 220 });
        dgvParceiros.CurrentCellDirtyStateChanged += (s, e) =>
        {
            if (dgvParceiros.IsCurrentCellDirty)
            {
                dgvParceiros.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };

        Controls.Add(dgvParceiros);
        Controls.Add(lblStatus);
        Controls.Add(painelTopo);

        // Adicionado por ultimo para ficar no topo da janela: com Dock.Top, o ultimo
        // controle adicionado e o que fica mais acima.
        Controls.Add(lblAmbiente);
    }

    /// <summary>
    /// Faixa fixa no topo com o servidor e a base de dados em uso, visivel desde a
    /// abertura — antes de conectar e antes de aplicar qualquer aumento.
    /// </summary>
    private Label MontarRotuloAmbiente()
    {
        var servidor = AmbienteExecucao.FormatarServidor(_queryService.Settings.BaseUrl);
        var companyDb = _queryService.Settings.CompanyDB;
        var producao = companyDb.Contains("PRD", StringComparison.OrdinalIgnoreCase);

        var rotulo = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Font = new Font(Font, FontStyle.Bold),
            Text = $"Servidor: {servidor}    |    Base de dados: {(string.IsNullOrWhiteSpace(companyDb) ? "(nao configurada)" : companyDb)}"
        };

        if (producao)
        {
            // Mesmo criterio do aviso que ja existe na confirmacao do aumento.
            rotulo.Text += "    |    PRODUCAO";
            rotulo.BackColor = Color.FromArgb(176, 0, 32);
            rotulo.ForeColor = Color.White;
        }
        else
        {
            rotulo.BackColor = Color.FromArgb(232, 232, 232);
        }

        return rotulo;
    }

    private void MarcarTodos(bool valor)
    {
        foreach (var p in _parceiros)
        {
            p.Selecionado = valor;
        }
        dgvParceiros.Refresh();
    }

    private void RecalcularTodos()
    {
        foreach (var p in _parceiros)
        {
            p.NovoCreditLimit = AumentoCreditLineService.CalcularNovoValor(p.CreditLimit ?? 0, nudPercentual.Value);
        }
        dgvParceiros.Refresh();
    }

    private async void BtnConsultar_Click(object? sender, EventArgs e)
    {
        DefinirCarregando(true, "Conectando e consultando clientes ativos e adimplentes...");

        try
        {
            _parceiros.Clear(); // grid some ja no clique, pra nao ficar mostrando resultado da consulta anterior durante o carregamento
            nudPercentual.Value = 0; // consulta sempre traz o valor atual do SAP, sem nenhum percentual (nem o da consulta anterior) ja aplicado

            var resultado = await _queryService.ConsultarElegiveisAsync();

            foreach (var p in resultado)
            {
                p.NovoCreditLimit = p.CreditLimit ?? 0; // "novo" comeca igual ao "atual" ate o usuario informar um percentual e clicar em Calcular
                _parceiros.Add(p);
            }

            btnMarcarTodos.Enabled = true;
            btnDesmarcarTodos.Enabled = true;
            AtualizarBotaoAplicar();

            // Servidor e base ficam na faixa do topo, entao aqui so o resultado da consulta.
            lblStatus.Text = $"{resultado.Count} cliente(s) ativo(s) e adimplente(s) consultado(s).";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Falha ao consultar", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = "Falha na consulta.";
        }
        finally
        {
            DefinirCarregando(false, lblStatus.Text);
        }
    }

    private async void BtnAplicar_Click(object? sender, EventArgs e)
    {
        var percentual = nudPercentual.Value;
        if (percentual == 0)
        {
            MessageBox.Show(this, "Informe um percentual diferente de zero.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var selecionados = _parceiros.Where(p => p.Selecionado).ToList();
        if (selecionados.Count == 0)
        {
            MessageBox.Show(this, "Nenhum cliente selecionado.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var companyDb = _queryService.Settings.CompanyDB;
        var modoSimulacao = chkModoSimulacao.Checked;
        var alertaProducao = companyDb.Contains("PRD", StringComparison.OrdinalIgnoreCase)
            ? "\n\nATENCAO: o CompanyDB atual parece ser de PRODUCAO."
            : string.Empty;

        var confirmar = MessageBox.Show(
            this,
            $"Confirma aplicar um aumento de {percentual}% no CreditLine de {selecionados.Count} cliente(s) " +
            $"no CompanyDB '{companyDb}'?\n\nModo simulacao: {(modoSimulacao ? "SIM (nada sera gravado no SAP)" : "NAO (vai gravar no SAP)")}{alertaProducao}",
            "Confirmar aumento de CreditLine",
            MessageBoxButtons.YesNo,
            modoSimulacao ? MessageBoxIcon.Question : MessageBoxIcon.Warning);

        if (confirmar != DialogResult.Yes)
        {
            return;
        }

        // A gravacao usa a conta de quem esta aplicando, e nao a conta de servico da
        // consulta: assim o SAP registra em OCRD quem alterou cada parceiro.
        var (conexao, usuarioSap) = await AutenticarParaGravacaoAsync();
        if (conexao is null)
        {
            return;
        }

        DefinirCarregando(true, $"Aplicando aumento de CreditLine como {usuarioSap}...");

        try
        {
            var resultados = await _aumentoService.AplicarAsync(conexao, usuarioSap, selecionados, percentual, modoSimulacao, (parceiro, resultado) =>
            {
                dgvParceiros.Refresh();
                lblStatus.Text = $"Processando... {parceiro.CardCode}: {parceiro.Status}";
            });

            var sucesso = resultados.Count(r => r.Sucesso);
            var erro = resultados.Count(r => !r.Sucesso);

            lblStatus.Text = $"{(modoSimulacao ? "Simulacao" : "Aumento")} concluido(a) por {usuarioSap}: {sucesso} OK, {erro} erro(s).";
            MessageBox.Show(this, lblStatus.Text, "Concluido", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Falha ao aplicar aumento", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            try
            {
                // A Service Layer tem limite de sessoes simultaneas: cada login feito
                // aqui precisa ser encerrado, mesmo quando a aplicacao falha no meio.
                await conexao.LogoutAsync();
            }
            catch
            {
                // Sessao ja expirada ou servidor indisponivel: nao atrapalha o usuario.
            }

            DefinirCarregando(false, lblStatus.Text);
        }
    }

    /// <summary>
    /// Pede usuario e senha do SAP e devolve a conexao autenticada, ou null se o usuario
    /// cancelar ou a autenticacao falhar (nesses casos a mensagem ja foi exibida).
    /// </summary>
    private async Task<(B1SLayer.SLConnection? Conexao, string Usuario)> AutenticarParaGravacaoAsync()
    {
        using var login = new FormLogin(
            AmbienteExecucao.FormatarServidor(_connectionFactory.Settings.BaseUrl),
            _connectionFactory.Settings.CompanyDB);

        if (login.ShowDialog(this) != DialogResult.OK)
        {
            return (null, string.Empty);
        }

        var usuarioSap = login.Usuario;
        DefinirCarregando(true, $"Autenticando {usuarioSap} no SAP...");

        try
        {
            return (await _connectionFactory.CriarAsync(login.Usuario, login.Senha), usuarioSap);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Falha na autenticacao", MessageBoxButtons.OK, MessageBoxIcon.Error);
            DefinirCarregando(false, "Autenticacao cancelada.");
            return (null, string.Empty);
        }
    }

    private void InitializeComponent()
    {

    }

    /// <summary>
    /// O "Aplicar Aumento" so fica disponivel com o modo simulacao DESMARCADO: marcado,
    /// ele trava a gravacao no SAP. Para conferir os valores antes, use o "Calcular",
    /// que preenche a coluna "CreditLimit novo" sem gravar nada.
    /// </summary>
    private void AtualizarBotaoAplicar()
    {
        btnAplicar.Enabled = !_carregando && _parceiros.Count > 0 && !chkModoSimulacao.Checked;
    }

    private void DefinirCarregando(bool carregando, string status)
    {
        _carregando = carregando;

        btnConsultar.Enabled = !carregando;
        btnCalcular.Enabled = !carregando && _parceiros.Count > 0;
        btnMarcarTodos.Enabled = !carregando && _parceiros.Count > 0;
        btnDesmarcarTodos.Enabled = !carregando && _parceiros.Count > 0;
        chkModoSimulacao.Enabled = !carregando;
        AtualizarBotaoAplicar();
        nudPercentual.Enabled = !carregando;
        lblStatus.Text = status;
        Cursor = carregando ? Cursors.WaitCursor : Cursors.Default;
    }
}
