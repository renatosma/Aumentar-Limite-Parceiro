/// <summary>
/// Pede o usuario e a senha do SAP de quem vai aplicar o aumento. A gravacao usa
/// essa conta, e nao a conta de servico do appsettings, para que o SAP registre em
/// OCRD quem alterou cada parceiro.
///
/// A senha existe apenas em memoria enquanto o dialogo esta aberto e a conexao e
/// criada: nao e gravada em arquivo nem no log.
/// </summary>
public sealed class FormLogin : Form
{
    private readonly TextBox _txtUsuario;
    private readonly TextBox _txtSenha;
    private readonly Button _btnOk;

    public FormLogin(string servidor, string companyDb)
    {
        Text = "Login do SAP";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(420, 170);

        // O destino aparece aqui tambem: quem digita a senha precisa ver em qual
        // servidor e base ela sera usada.
        var lblDestino = new Label
        {
            Text = $"Servidor: {servidor}\r\nBase de dados: {companyDb}",
            Location = new Point(12, 12),
            Size = new Size(396, 34),
            TextAlign = ContentAlignment.TopLeft
        };

        var lblUsuario = new Label { Text = "Usuario:", Location = new Point(12, 58), Size = new Size(70, 20), TextAlign = ContentAlignment.MiddleRight };
        _txtUsuario = new TextBox { Location = new Point(88, 56), Size = new Size(320, 23) };

        var lblSenha = new Label { Text = "Senha:", Location = new Point(12, 88), Size = new Size(70, 20), TextAlign = ContentAlignment.MiddleRight };
        _txtSenha = new TextBox { Location = new Point(88, 86), Size = new Size(320, 23), UseSystemPasswordChar = true };

        _btnOk = new Button { Text = "Entrar", DialogResult = DialogResult.OK, Location = new Point(232, 126), Size = new Size(84, 28), Enabled = false };
        var btnCancelar = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(324, 126), Size = new Size(84, 28) };

        _txtUsuario.TextChanged += (s, e) => AtualizarBotaoEntrar();
        _txtSenha.TextChanged += (s, e) => AtualizarBotaoEntrar();

        Controls.AddRange(new Control[] { lblDestino, lblUsuario, _txtUsuario, lblSenha, _txtSenha, _btnOk, btnCancelar });

        AcceptButton = _btnOk;
        CancelButton = btnCancelar;
    }

    public string Usuario => _txtUsuario.Text.Trim();

    public string Senha => _txtSenha.Text;

    private void AtualizarBotaoEntrar()
    {
        _btnOk.Enabled = _txtUsuario.Text.Trim().Length > 0 && _txtSenha.Text.Length > 0;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Nao deixa a senha no controle depois que o dialogo e fechado.
            _txtSenha.Clear();
        }

        base.Dispose(disposing);
    }
}
