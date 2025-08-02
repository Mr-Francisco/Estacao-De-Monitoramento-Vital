# EstacaoDeMonitoramentoVital

 Estação de Monitoramento Vital – Aplicação em Windows Forms 
A Estação de Monitoramento Vital é uma aplicação desenvolvida em Windows Forms (C#) voltada para o monitoramento local de sinais vitais, com comunicação direta via porta USB/Serial (TTL) com dispositivos como Arduino. O sistema exibe os dados em tempo real, permite análises visuais por meio de gráficos, e apresenta a localização do paciente/unidade por meio de um WebView embutido com mapa HTML.

⚙️ Principais características:
🔌 Recepção de dados em tempo real via porta serial (USB) usando protocolo simples com Arduino/conversor TTL.

🧾 Registro local em banco de dados SQLite, sem dependência de servidores externos.

📊 Visualização de dados com gráficos dinâmicos (barras, linha ou tempo real).

🗺️ Exibição de mapa interativo usando WebBrowser e HTML local.

🚨 Sistema de alertas locais, com limites configuráveis para cada parâmetro.

📁 Exportação de relatórios em PDF com resumo e histórico dos sinais.

🧠 Interface responsiva e simples, otimizada para uso em clínicas, ambulatórios ou laboratórios educacionais.

🧪 Tecnologias e recursos:
Plataforma: .NET Framework / Windows Forms

Linguagem: C#

Comunicação: SerialPort (System.IO.Ports)

Banco de dados: SQLite.

Mapa: WebBrowser + HTML

Exportação: PDF com iTextSharp ou PdfSharp

