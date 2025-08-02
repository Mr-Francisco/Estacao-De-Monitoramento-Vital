using CefSharp;
using CefSharp.WinForms;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EstacaoDeMonitoramentoVital
{
    public partial class Form2 : Form
    {
     
        public const string Senha_Chefe_Para_Visulizar_senha = "KetShap"; // Código para mostrar a senha
        public Form2()
        {
            InitializeComponent();
            // Configuração do ListView
            this.listView1.View = View.Details; // Exibir como tabela
            this.listView1.FullRowSelect = true; // Seleciona linha toda
            this.listView1.GridLines = true; // Linhas visíveis
            this.listView1.BorderStyle = BorderStyle.FixedSingle;

            // Estilo visual moderno
            this.listView1.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            this.listView1.BackColor = Color.FromArgb(245, 245, 245); // Fundo claro
            this.listView1.ForeColor = Color.FromArgb(40, 40, 40);
            listView1.Columns.Add("ID", 80);
            listView1.Columns.Add("Nome", 200);
            listView1.Columns.Add("Senha", 200); // ← essa será escondida

            listView1.Columns[2].Width = 0;

            CarregarUsuariosNaLista();
        }
        private void CarregarUsuariosNaLista()
        {
            listView1.Items.Clear();
            var usuarios = Banco.ListarUsuarios();

            foreach (var usuario in usuarios)
            {
                var item = new ListViewItem(usuario.Id.ToString());
                item.SubItems.Add(usuario.Nome);
                item.SubItems.Add(usuario.Senha); // Senha vai para a 3ª coluna (invisível)
                listView1.Items.Add(item);
            }
        }


        private void Form2_Load(object sender, EventArgs e)
        {
            // Exemplo automático (pode ser removido)
            // AtualizarCoordenadas(-8.8383, 13.2344);
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            string nome = TXT_NOME.Text.Trim();
            string senha = TXT_SENHA.Text;

            if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(senha))
            {
                MessageBox.Show("Por favor, preencha todos os campos!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Banco. CadastrarUsuario(nome, senha);
                MessageBox.Show("Usuário cadastrado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Limpa os campos após cadastro
                TXT_NOME.Text = "";
                TXT_SENHA.Text = "";
                CarregarUsuariosNaLista();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao cadastrar usuário: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
                
        
        private bool voltando = false;
        private void BT_VOLTAR_Click(object sender, EventArgs e)
        {

           

      
            if (voltando) return;
            voltando = true;
;
            this.Close();
   
    }

        private async void BT_Mostrar_senha_Click(object sender, EventArgs e)
        {
           
            string codigoDigitado = TXT_senha_list.Text.Trim();
            TXT_senha_list.Clear(); // Limpa o código digitado
            if (codigoDigitado == Senha_Chefe_Para_Visulizar_senha)
            {
                listView1.Columns[2].Width = 200; // Mostrar senha
                // Espera 5 segundos
                await Task.Delay(5000);

                // Oculta a senha
                listView1.Columns[2].Width = 0;
                TXT_senha_list.Clear(); // Limpa o código digitado
            }
            else
            {
                MessageBox.Show("Código incorreto.", "Acesso Negado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        

        }

        private void TXT_senha_list_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
