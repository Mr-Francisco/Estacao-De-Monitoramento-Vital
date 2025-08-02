using CefSharp;
using CefSharp.DevTools.DOM;
using CefSharp.WinForms;
using Guna.UI2.WinForms;
using iTextSharp.text;
using iTextSharp.text.pdf;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using PdfFont = iTextSharp.text.Font;
using PdfFontFamily = iTextSharp.text.Font.FontFamily;
using PdfRectangle = iTextSharp.text.Rectangle;





namespace EstacaoDeMonitoramentoVital
{
    public partial class Form1 : Form
    {

        private LineSeries serieBPM;
        private LineSeries serieSpO2;
        private LiveCharts.WinForms.CartesianChart grafico;
        private int contadorTempo = 0;
        private string identificador = "By: F.A.(KetShat)";

        ChromiumWebBrowser browser;
        bool mapaPronto = false;
        bool graficoJaCriado = false;
        bool LOGIN = true;
        bool SERIAL = false;
        List<TabPage> paginasOcultas = new List<TabPage>();
        SerialPort portaSerial;
        System.Windows.Forms.Timer timerVerificacao;
         
        public Form1()
        {
            InitializeComponent();

            tamanho();
            if (!Banco.UsuarioExiste("Teste"))
            {
                Banco.CadastrarUsuario("Teste", "ITEL");
                MessageBox.Show($"Novo Usuario Cadastrado com sucesso!\n {identificador}");
            }
            var settings = new CefSettings();
            settings.CefCommandLineArgs.Add("disable-web-security", "1");
            settings.CefCommandLineArgs.Add("allow-file-access-from-files", "1");
            settings.CefCommandLineArgs.Add("allow-running-insecure-content", "1");

           

            // ✅ Inicializa CefSharp apenas uma vez por processo
            if (Cef.IsInitialized != true) // forma segura para lidar com bool?
            {
                CefSettings cefSettings = new CefSettings();
                Cef.Initialize(cefSettings);
            }


            string htmlPath = Path.Combine(System.Windows.Forms.Application.StartupPath, "mapa.html");

            // Cria o navegador Chromium
            browser = new ChromiumWebBrowser(htmlPath);
            browser.Dock = DockStyle.Fill;

            // ✅ Aguarda carregamento completo do navegador
            browser.IsBrowserInitializedChanged += (s, e) =>
            {
                if (browser.IsBrowserInitialized)
                {
                    browser.LoadingStateChanged += (sender, args) =>
                    {
                        if (!args.IsLoading)
                        {
                            mapaPronto = true;

                            // ✅ Executa no UI thread
                            this.Invoke(new Action(() =>
                            {
                                MessageBox.Show($"Mapa carregado com sucesso!\n {identificador}");
                            }));
                        }
                    };
                }
            };

            Tab_Mapa.Controls.Add(browser); // ✅ Correto!
            browser.BringToFront();

            LB_2_1.Visible = false;
            LB_2_2.Visible = false;
            LB_2_3.Visible = false;
            PANEL_2_1.Visible = false;
            PANEL_2_2.Visible = false;
            PIC_2_1.Visible = false;
            COMBOX_PORTA_SERIAL.Visible = false;
            BT_CONECTAR.Visible = false;
            LB_estado_conexao.Visible = false;
        }

        private void tamanho()
        {

            // ANCHOR para redimensionamento automático (em ambos os grupos)
            LB_1_1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            LB_1_2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            LB_1_3.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            LB_1_4.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            PANEL_1_1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            PANEL_1_2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            PIC_1_1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left ;

            TXT_PALAVRA_PASSE.Anchor = AnchorStyles.Top |  AnchorStyles.Left | AnchorStyles.Right;
            TXT_NOME_USUARIO.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            BT_LOGIN.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            LB_2_1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            LB_2_2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            LB_2_3.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            PANEL_2_1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            PANEL_2_2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            PIC_2_1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;

            COMBOX_PORTA_SERIAL.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            BT_CONECTAR.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        }
        private void Form1_Load(object sender, EventArgs e) { }

        public void AtualizarCoordenadas(double lat, double lon)
        {
            if (mapaPronto && browser != null && browser.IsBrowserInitialized && browser.GetBrowser() != null)
            {
                string script = $"atualizarLocal({lat.ToString(CultureInfo.InvariantCulture)}, {lon.ToString(CultureInfo.InvariantCulture)});";
                browser.ExecuteScriptAsync(script);
            }
            else
            {
                MessageBox.Show($"O mapa ainda não está pronto!\n {identificador}");
            }
        }

        private void CriarGraficoHospitalar()
        {
            PanelGrafico.Controls.Clear();

            grafico = new LiveCharts.WinForms.CartesianChart
            {
                Dock = DockStyle.Fill,
                LegendLocation = LegendLocation.Top
            };

            serieBPM = new LineSeries
            {
                Title = "BPM",
                Values = new ChartValues<ObservableValue>(),
                Stroke = System.Windows.Media.Brushes.Crimson,
                Fill = System.Windows.Media.Brushes.Transparent,
                PointGeometry = DefaultGeometries.Circle,
                PointGeometrySize = 6
            };

            serieSpO2 = new LineSeries
            {
                Title = "SpO2",
                Values = new ChartValues<ObservableValue>(),
                Stroke = System.Windows.Media.Brushes.DodgerBlue,
                Fill = System.Windows.Media.Brushes.Transparent,
                PointGeometry = DefaultGeometries.Square,
                PointGeometrySize = 6
            };

            grafico.Series = new SeriesCollection { serieBPM, serieSpO2 };

            grafico.AxisX.Add(new Axis
            {
                Title = "Tempo",
                LabelFormatter = value => value.ToString("0")
            });

            grafico.AxisY.Add(new Axis
            {
                Title = "Valores",
                MinValue = 40,
                MaxValue = 140,
            });

            PanelGrafico.Controls.Add(grafico);
        }

        private void LimparGrafico()
        {
            serieBPM?.Values.Clear();
            serieSpO2?.Values.Clear();
            contadorTempo = 0;
        }

        private void AlternarVisibilidadeComAnimacao(bool modoAtivo)
        {
            // Grupo 1 (Visível quando modoAtivo = true)
            LB_1_1.Visible = modoAtivo;
            LB_1_2.Visible = modoAtivo;
            LB_1_3.Visible = modoAtivo;
            LB_1_4.Visible = modoAtivo;
            PANEL_1_1.Visible = modoAtivo;
            PANEL_1_2.Visible = modoAtivo;
            PIC_1_1.Visible = modoAtivo;
            TXT_PALAVRA_PASSE.Visible = modoAtivo;
            TXT_NOME_USUARIO.Visible = modoAtivo;
            BT_LOGIN.Visible = modoAtivo;

            // Grupo 2 (Visível quando modoAtivo = false)
            LB_2_1.Visible = !modoAtivo;
            LB_2_2.Visible = !modoAtivo;
            LB_2_3.Visible = !modoAtivo;
            PANEL_2_1.Visible = !modoAtivo;
            PANEL_2_2.Visible = !modoAtivo;
            PIC_2_1.Visible = !modoAtivo;
            COMBOX_PORTA_SERIAL.Visible = !modoAtivo;
            BT_CONECTAR.Visible = !modoAtivo;
        }

        private void guna2TabControl1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (guna2TabControl1.SelectedTab == Tab_monitoramento && !graficoJaCriado)
            {
                CriarGraficoHospitalar();
                graficoJaCriado = true;
            }

            if (guna2TabControl1.SelectedTab == Tab_Resistro)
            {
                EstilizarGrid();
                CarregarDados();
            }
        }

        private void AdicionarBotoes()
        {
            if (!DTGRID1.Columns.Contains("Editar"))
            {
                DataGridViewButtonColumn btnEditar = new DataGridViewButtonColumn();
                btnEditar.Name = "Editar";
                btnEditar.HeaderText = "";
                btnEditar.Text = "✏️ Editar";
                btnEditar.UseColumnTextForButtonValue = true;
                btnEditar.Width = 80;
                DTGRID1.Columns.Add(btnEditar);
            }

            if (!DTGRID1.Columns.Contains("Excluir"))
            {
                DataGridViewButtonColumn btnExcluir = new DataGridViewButtonColumn();
                btnExcluir.Name = "Excluir";
                btnExcluir.HeaderText = "";
                btnExcluir.Text = "🗑 Excluir";
                btnExcluir.UseColumnTextForButtonValue = true;
                btnExcluir.Width = 80;
                DTGRID1.Columns.Add(btnExcluir);
            }
        }

        private void DTGRID1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if (DTGRID1.Columns[e.ColumnIndex].Name == "Editar")
                {
                    MessageBox.Show("Editar: " + DTGRID1.Rows[e.RowIndex].Cells[0].Value.ToString());
                }
                else if (DTGRID1.Columns[e.ColumnIndex].Name == "Excluir")
                {
                    DialogResult result = MessageBox.Show("Deseja excluir?", "Confirmação", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        DTGRID1.Rows.RemoveAt(e.RowIndex);
                    }
                }
            }
        }

        private void EstilizarGrid()
        {
            DTGRID1.ThemeStyle.AlternatingRowsStyle.BackColor = Color.FromArgb(25, 32, 50);
            DTGRID1.ThemeStyle.AlternatingRowsStyle.ForeColor = Color.White;

            DTGRID1.ThemeStyle.BackColor = Color.FromArgb(15, 23, 42);
            DTGRID1.BackgroundColor = Color.FromArgb(15, 23, 42);

            DTGRID1.ThemeStyle.RowsStyle.BackColor = Color.FromArgb(20, 28, 48);
            DTGRID1.ThemeStyle.RowsStyle.ForeColor = Color.White;
            DTGRID1.ThemeStyle.RowsStyle.Font = new System.Drawing.Font("Segoe UI", 10);

            DTGRID1.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(34, 212, 232);
            DTGRID1.ThemeStyle.HeaderStyle.ForeColor = Color.Black;
            DTGRID1.ThemeStyle.HeaderStyle.Font = new System.Drawing.Font("Segoe UI", 11, FontStyle.Bold);
            DTGRID1.ThemeStyle.HeaderStyle.Height = 30;

            DTGRID1.BorderStyle = BorderStyle.None;
            DTGRID1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            DTGRID1.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            DTGRID1.GridColor = Color.FromArgb(50, 70, 90);
            DTGRID1.EnableHeadersVisualStyles = false;
            DTGRID1.RowHeadersVisible = false;
            DTGRID1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            DTGRID1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(38, 255, 180);
            DTGRID1.DefaultCellStyle.SelectionForeColor = Color.Black;
        }

        private void CarregarDados()
        {
            // Evita colunas duplicadas
            if (DTGRID1.Columns.Count == 0)
            {
                DTGRID1.Columns.Add("id", "ID");
                DTGRID1.Columns.Add("dataHora", "Data e Hora");
                DTGRID1.Columns.Add("bpm", "BPM");
                DTGRID1.Columns.Add("spo2", "SpO2");
                DTGRID1.Columns.Add("latitude", "Latitude");
                DTGRID1.Columns.Add("longitude", "Longitude");
            }

            DTGRID1.Rows.Clear(); // Limpa antes de recarregar

            var leituras = Banco.ObterTodas();

            foreach (var l in leituras)
            {
                DTGRID1.Rows.Add(l.Id, l.DataHora.ToString("dd/MM/yyyy HH:mm:ss"), l.BPM, l.SpO2, l.Latitude, l.Longitude);
            }

            AdicionarBotoes(); // Se quiser manter os botões
        }


        private void BT_LOGIN_Click(object sender, EventArgs e)
        {

            string usuario = TXT_NOME_USUARIO.Text.Trim();
            string senha = TXT_PALAVRA_PASSE.Text.Trim();

 bool loginValido = Banco.ValidarLogin(usuario, senha);

            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(senha))
            {
                MessageBox.Show($"⚠ Por favor, preencha todos os campos!\n {identificador}", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!RAD_BT_CADASTRAR.Checked && !RAD_BT_ENTRAR.Checked) {
                MessageBox.Show($"⚠ Por favor, Selecione entre 'Entrar' e 'Cadastrar '!\n {identificador}", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }


            if (RAD_BT_ENTRAR.Checked && loginValido)
            {
                MessageBox.Show($"✅ Login realizado com sucesso!\n {identificador}", "Bem-vindo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AlternarVisibilidadeComAnimacao(SERIAL);
                foreach (var tab in paginasOcultas)
                {
                    if (!guna2TabControl1.TabPages.Contains(tab))
                        guna2TabControl1.TabPages.Add(tab); 
                }
            }
            else if (RAD_BT_CADASTRAR.Checked && loginValido)
            {
                MessageBox.Show($"Abrindo tela de Cadastra\n {identificador}", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Form2 form2 = new Form2();  
                form2.ShowDialog(); // Exibe o formulário de cadastro
            }
            else
            {
                MessageBox.Show($"❌ Usuário ou senha inválidos!\n {identificador}", "Erro de Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            TXT_NOME_USUARIO.Clear();
           TXT_PALAVRA_PASSE.Clear();

        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            CriarGraficoHospitalar();

            for (int i = 1; i < guna2TabControl1.TabPages.Count;)
            {
                paginasOcultas.Add(guna2TabControl1.TabPages[i]);
                guna2TabControl1.TabPages.RemoveAt(i);
            }

            guna2TabControl1.SelectedTab = TAB_CONEXAO;

            timerVerificacao = new System.Windows.Forms.Timer();
            timerVerificacao.Interval = 1000;
            timerVerificacao.Tick += VerificarConexaoSerial;

            AtualizarStatus(false);
        }

        private void BT_CONECTAR_Click(object sender, EventArgs e)
        {
          string porta = COMBOX_PORTA_SERIAL.Text.Trim();

            if (string.IsNullOrEmpty(porta))
            {
                MessageBox.Show($"⚠ Informe a porta (ex: COM3)\n {identificador}", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

                try
               {
                   if (portaSerial != null && portaSerial.IsOpen)
                   {
                       portaSerial.Close();
                       timerVerificacao.Stop();
                       AtualizarStatus(false);
                       BT_CONECTAR.Text = "Conectar";
                       BT_CONECTAR.FillColor = Color.FromArgb(255, 144, 66);
                       MessageBox.Show($" Desconectado da porta.\n {identificador}", "Serial", MessageBoxButtons.OK, MessageBoxIcon.Information);
                   }
                   else
                   {
                       portaSerial = new SerialPort(porta, 9600);
                       portaSerial.Open();
                       portaSerial.DataReceived += PortaSerial_DataReceived;
                       timerVerificacao.Start();
                       AtualizarStatus(true);
                       BT_CONECTAR.Text = "Desconectar";
                       BT_CONECTAR.FillColor = Color.FromArgb(0, 111, 189);
                       MessageBox.Show($" Conectado com sucesso!\n {identificador}", "Serial", MessageBoxButtons.OK, MessageBoxIcon.Information);
                   }
              
        }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show($"🚫 Porta em uso por outro programa.\n {identificador}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException)
            {
                MessageBox.Show($"❌ Porta não existe ou foi desconectada.\n {identificador}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❗ Erro ao conectar: \n {identificador}" + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void VerificarConexaoSerial(object sender, EventArgs e)
        {
            if (portaSerial == null || !portaSerial.IsOpen)
            {
                AtualizarStatus(false);
                BT_CONECTAR.Text = "Conectar";
                BT_CONECTAR.FillColor = Color.FromArgb(255, 144, 66);
                timerVerificacao.Stop();
            }
        }

        private void AtualizarStatus(bool conectado)
        {
           
            
            if (conectado)
            {

                this.Text = "Conectado";
                this.ForeColor = Color.LimeGreen;
            }
            else
            {
                this.Text = "Desconectado";
                this.ForeColor = Color.Red;
            }
        }

        private void PortaSerial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string linha = portaSerial.ReadExisting();
                Invoke(new Action(() => ProcessarDados(linha)));
            }
            catch (Exception ex) { 
               MessageBox.Show($"❗ Erro ao ler dados da porta serial: {ex.Message}\n {identificador}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ProcessarDados(string linha)
        {
            try
            {
                string[] partes = linha.Split(',');

                int bpm = 0, spo2 = 0;
                double lat = 0, lon = 0;

                foreach (string p in partes)
                {
                    try
                    {
                        if (p.StartsWith("BPM:"))
                            bpm = int.Parse(p.Replace("BPM:", ""));

                        else if (p.StartsWith("SpO2:"))
                            spo2 = int.Parse(p.Replace("SpO2:", ""));

                        else if (p.StartsWith("Lat:"))
                            lat = double.Parse(p.Replace("Lat:", ""), CultureInfo.InvariantCulture);

                        else if (p.StartsWith("Lon:"))
                            lon = double.Parse(p.Replace("Lon:", ""), CultureInfo.InvariantCulture);
                    }
                    catch { continue; }
                }

                if (bpm > 0 && spo2 > 0)
                {
                    AtualizarCoordenadas(lat, lon);

                    // Insere diretamente no banco com os parâmetros
                    Banco.Inserir(bpm, spo2, 0, 0, lat, lon);
                    LB_BPM_VALOR.Text = bpm.ToString();
                    LB_SPO2_VALOR.Text = spo2.ToString();

                    AvaliarEstadoBPM(bpm,spo2);
                    LB_ALERT_CONTADOR.Text = contador_alerta_TOTAL.ToString();
                   contador_alerta=contador_alerta_TOTAL;
                    if (contador_alerta%5==0 && contador_alerta>0)
                    {
                        contador_alerta_perigo= true;
                    }

                    ControlarAlertaSonoro(contador_alerta_perigo);


                    // Atualiza gráfico
                    contadorTempo++;

                    serieBPM.Values.Add(new ObservableValue(bpm));
                    serieSpO2.Values.Add(new ObservableValue(spo2));

                    if (serieBPM.Values.Count > 30)
                        serieBPM.Values.RemoveAt(0);
                    if (serieSpO2.Values.Count > 30)
                        serieSpO2.Values.RemoveAt(0);
                }
            }
            catch
            {
                // Ignora erro
            }
        }

        private int contador_alerta = 0;
        private bool contador_alerta_perigo = false;
        private int contador_alerta_TOTAL = 0;
        private void AvaliarEstadoBPM(int bpm, int spo2)
        {
            if (bpm < 50)
            {
                LB_BPM_ESTADO.Text = "Muito Baixo)";
                LB_sinalizacao.Text = "Procure atendimento médico imediatamente!";
                LB_sinalizacao.ForeColor = Color.Red;
                contador_alerta_TOTAL++;
            }
            else if (bpm >= 50 && bpm < 59)
            {
                LB_BPM_ESTADO.Text = "BPM Baixo";
                LB_sinalizacao.Text = "Possível cansaço ou condição clínica. Acompanhar.";
                LB_sinalizacao.ForeColor = Color.Orange;
                contador_alerta_TOTAL++;
            }
            else if (bpm >= 60 && bpm <= 100)
            {
                LB_BPM_ESTADO.Text = "BPM Normal";
                LB_sinalizacao.Text = "Dentro do padrão saudável.";
                LB_sinalizacao.ForeColor = Color.FromArgb(0, 192, 192);
            }
            else if (bpm > 101 && bpm <= 120)
            {
                LB_BPM_ESTADO.Text = "BPM Alto";
                LB_sinalizacao.Text = "Pode ser esforço, ansiedade ou febre.";
                LB_sinalizacao.ForeColor = Color.Orange;
                contador_alerta_TOTAL++;
            }
            else if (bpm > 120)
            {
                if (!contador_alerta_perigo)
                { 
                    contador_alerta_perigo = true;
                     // Reseta o contador de alertas
                }
                LB_BPM_ESTADO.Text = "Muito Alto";
                LB_sinalizacao.Text = "Risco cardíaco! Monitorar e buscar ajuda.";
                LB_sinalizacao.ForeColor = Color.Red;
            }

            // Avaliação do SpO2
            if (spo2 >= 95 && spo2 <= 100)
            {
                LB_SPO2_ESTADO.Text = "SpO2 Normal";
                LB_SPO2_ESTADO.ForeColor = Color.FromArgb(0, 192, 192);
            }
            else if (spo2 >= 90 && spo2 < 95)
            {
                LB_SPO2_ESTADO.Text = "saturação";
                LB_SPO2_ESTADO.ForeColor = Color.Orange;
            }
            else if (spo2 < 90)
            {
                LB_SPO2_ESTADO.Text = "Hipóxia baixa";
                LB_SPO2_ESTADO.ForeColor = Color.Orange;
            }
        }

        private CancellationTokenSource alertaSomToken;

        private async void ControlarAlertaSonoro(bool ativar)
        {
            // Se ativar = true e ainda não está tocando, começa
            if (ativar)
            {
                // Evita iniciar múltiplas instâncias
                if (alertaSomToken != null && !alertaSomToken.IsCancellationRequested)
                    return;

                alertaSomToken = new CancellationTokenSource();
                CancellationToken token = alertaSomToken.Token;

                await Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        System.Media.SystemSounds.Hand.Play();
                        await Task.Delay(2000); // toca a cada 2 segundos
                    }
                });
            }
            else
            {
                // Para o som se estiver ativo
                if (alertaSomToken != null)
                {
                    alertaSomToken.Cancel();
                    alertaSomToken.Dispose();
                    alertaSomToken = null;
                }
            }
        }


        // Métodos vazios (event handlers não implementados)
        private void guna2ShadowPanel1_Paint(object sender, PaintEventArgs e) { }
        private void tabPage1_Click(object sender, EventArgs e) { }
        private void PIC_2_1_Click(object sender, EventArgs e) { }
        private void guna2HtmlLabel7_Click(object sender, EventArgs e) { }
        private void guna2ShadowPanel2_Paint(object sender, PaintEventArgs e) { }
        private void guna2Button1_Click(object sender, EventArgs e) { }
        private void PanelGrafico_Paint(object sender, PaintEventArgs e) { }
        private void Tab_monitoramento_Click(object sender, EventArgs e) { }
        private void guna2ImageButton4_Click(object sender, EventArgs e) {


            if (contador_alerta_TOTAL == 0)
            {
                MessageBox.Show($"Nenhum alerta foi registrado durante esta sessão.\n {identificador}", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            else if( contador_alerta_TOTAL > 0 && !contador_alerta_perigo)
            {
                // Se o alerta não está em perigo, apenas informa o total
                MessageBox.Show($"Durante esta sessão, o BPM esteve em estado de alerta {contador_alerta_TOTAL} vezes.\n {identificador}", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
           
            if (contador_alerta_perigo)
            {
                contador_alerta_perigo = false;

                contador_alerta = 0;
                contador_alerta_TOTAL = 0;

                // Som mais sério para reforçar que é algo importante
                System.Media.SystemSounds.Asterisk.Play();

                // Mostra mensagem com contador atual
                MessageBox.Show(
                    $"📊 Alerta finalizado.\n\nDurante esta sessão, o BPM esteve em estado de alerta {contador_alerta_TOTAL} vezes.\n\nTodos os contadores foram reiniciados com sucesso.\n {identificador}",
                    "Resumo da Sessão - Estado Crítico de BPM",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
         
           
        }
        private void DTGRID1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }

        private void guna2HtmlLabel4_Click(object sender, EventArgs e)
        {

        }

        private void guna2HtmlLabel6_Click(object sender, EventArgs e)
        {

        }

        private void guna2Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void guna2ImageButton3_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog salvarDialogo = new SaveFileDialog())
            {
                salvarDialogo.Filter = "Imagem PNG (*.png)|*.png";
                salvarDialogo.Title = "Salvar gráfico como imagem";
                salvarDialogo.FileName = "grafico_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";

                if (salvarDialogo.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        Bitmap bmp = new Bitmap(grafico.Width, grafico.Height);
                        grafico.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height));
                        bmp.Save(salvarDialogo.FileName, ImageFormat.Png);

                        MessageBox.Show($"Gráfico salvo com sucesso!\n {identificador}", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao salvar: {ex.Message} \n {identificador}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

       


        private void guna2ImageButton5_Click(object sender, EventArgs e)
        {
            LimparGrafico();
        }

        private void guna2HtmlLabel5_Click(object sender, EventArgs e)
        {

        }

        private void BT_RESISTRO_LIMPAR_Click(object sender, EventArgs e)
        {
            DialogResult resultado = MessageBox.Show(
        $"Tem certeza de que deseja deletar todos os registros?\n {identificador}",
        "Confirmar exclusão",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Warning
    );

            if (resultado == DialogResult.Yes)
            {
                Banco.DeletarTodos();
                MessageBox.Show($"Todos os registros foram deletados com sucesso.\n {identificador}", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // Cancelado pelo usuário — opcional mostrar algo ou não fazer nada
                MessageBox.Show($"Operação cancelada.\n {identificador}", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void guna2ImageButton2_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog salvar = new SaveFileDialog())
            {
                salvar.Filter = "Arquivo PDF (*.pdf)|*.pdf";
                salvar.Title = "Salvar relatório em PDF";
                salvar.FileName = "relatorio_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf";

                if (salvar.ShowDialog() == DialogResult.OK)
                {
                    string caminho = salvar.FileName;

                    using (var doc = new Document(PageSize.A4))
                    {
                        PdfWriter.GetInstance(doc, new FileStream(caminho, FileMode.Create));
                        doc.Open();

                        var titulo = new Paragraph(
                            "Relatório de Monitoramento Vital",
                            new PdfFont(PdfFontFamily.HELVETICA, 18, PdfFont.BOLD)
                        );
                        titulo.Alignment = Element.ALIGN_CENTER;
                        doc.Add(titulo);
                        doc.Add(new Paragraph("\n"));

                        PdfPTable tabela = new PdfPTable(6);
                        tabela.WidthPercentage = 100;

                        string[] cabecalhos = { "ID", "BPM", "SpO2", "Latitude", "Longitude", "Data" };
                        foreach (var col in cabecalhos)
                        {
                            tabela.AddCell(new PdfPCell(new Phrase(col))
                            {
                                BackgroundColor = BaseColor.LIGHT_GRAY
                            });
                        }

                        using (var conn = Banco.Conexao())
                        using (var cmd = new SQLiteCommand("SELECT * FROM Leituras", conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tabela.AddCell(reader["Id"].ToString());
                                tabela.AddCell(reader["BPM"].ToString());
                                tabela.AddCell(reader["SpO2"].ToString());
                                tabela.AddCell(reader["Latitude"].ToString());
                                tabela.AddCell(reader["Longitude"].ToString());
                                tabela.AddCell(reader["DataHora"].ToString());

                            }
                        }

                        doc.Add(tabela);
                        doc.Close();
                    }

                    MessageBox.Show($"Relatório PDF salvo com sucesso!\n {identificador}", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        public static void GerarRelatorioPDF(string caminho)
        {
            using (var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4))
            {
                PdfWriter.GetInstance(doc, new FileStream(caminho, FileMode.Create));
                doc.Open();

                var titulo = new iTextSharp.text.Paragraph(
                    "Relatório de Monitoramento Vital",
                    new PdfFont(PdfFontFamily.HELVETICA, 18, PdfFont.BOLD)
                );
                titulo.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
                doc.Add(titulo);
                doc.Add(new iTextSharp.text.Paragraph("\n"));

                PdfPTable tabela = new PdfPTable(6);
                tabela.WidthPercentage = 100;

                string[] cabecalhos = { "ID", "BPM", "SpO2", "Latitude", "Longitude", "Data" };
                foreach (var col in cabecalhos)
                {
                    tabela.AddCell(new PdfPCell(new Phrase(col)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                }

                using (var conn = Banco.Conexao())
                using (var cmd = new SQLiteCommand("SELECT * FROM Leituras", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tabela.AddCell(reader["Id"].ToString());
                        tabela.AddCell(reader["bpm"].ToString());
                        tabela.AddCell(reader["spo2"].ToString());
                        tabela.AddCell(reader["lat"].ToString());
                        tabela.AddCell(reader["lon"].ToString());
                        tabela.AddCell(reader["data"].ToString());
                    }
                }

                doc.Add(tabela);
                doc.Close();
            }
        }

        private void guna2Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void LB_ALERT_CONTADOR_Click(object sender, EventArgs e)
        {

        }

        private async void guna2ImageButton1_Click(object sender, EventArgs e)
        {
 System.Media.SystemSounds.Question.Play();

            // Altera o texto (ou imagem) temporariamente para dar o efeito "modo secreto"
            var botao = (Guna.UI2.WinForms.Guna2ImageButton)sender;
            botao.Enabled = false;

            Color corOriginal = botao.BackColor;

            // Efeito de "piscar" 3 vezes
            for (int i = 0; i < 3; i++)
            {
                botao.BackColor = Color.DarkRed;
                await Task.Delay(150);
                botao.BackColor = corOriginal;
                await Task.Delay(150);
            }

            // Mostra a mensagem com humor e classe
            MessageBox.Show(
                $"🛠️ Esta funcionalidade está em construção.\n\nEstamos preparando...\n\nVolte em breve...\n {identificador}",
                "Função indisponível no momento",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            botao.Enabled = true;
        }

        private void guna2Button4_Click(object sender, EventArgs e)
        {
            CarregarDados();
        }

        private void guna2Button5_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog salvar = new SaveFileDialog())
            {
                salvar.Filter = "Arquivo PDF (*.pdf)|*.pdf";
                salvar.Title = "Salvar relatório em PDF";
                salvar.FileName = "relatorio_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf";

                if (salvar.ShowDialog() == DialogResult.OK)
                {
                    string caminho = salvar.FileName;

                    using (var doc = new Document(PageSize.A4))
                    {
                        PdfWriter.GetInstance(doc, new FileStream(caminho, FileMode.Create));
                        doc.Open();

                        var titulo = new Paragraph(
                            "Relatório de Monitoramento Vital",
                            new PdfFont(PdfFontFamily.HELVETICA, 18, PdfFont.BOLD)
                        );
                        titulo.Alignment = Element.ALIGN_CENTER;
                        doc.Add(titulo);
                        doc.Add(new Paragraph("\n"));

                        PdfPTable tabela = new PdfPTable(6);
                        tabela.WidthPercentage = 100;

                        string[] cabecalhos = { "ID", "BPM", "SpO2", "Latitude", "Longitude", "Data" };
                        foreach (var col in cabecalhos)
                        {
                            tabela.AddCell(new PdfPCell(new Phrase(col))
                            {
                                BackgroundColor = BaseColor.LIGHT_GRAY
                            });
                        }

                        using (var conn = Banco.Conexao())
                        using (var cmd = new SQLiteCommand("SELECT * FROM Leituras", conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tabela.AddCell(reader["Id"].ToString());
                                tabela.AddCell(reader["BPM"].ToString());
                                tabela.AddCell(reader["SpO2"].ToString());
                                tabela.AddCell(reader["Latitude"].ToString());
                                tabela.AddCell(reader["Longitude"].ToString());
                                tabela.AddCell(reader["DataHora"].ToString());

                            }
                        }

                        doc.Add(tabela);
                        doc.Close();
                    }

                    MessageBox.Show($"Relatório PDF salvo com sucesso!\n {identificador}", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}