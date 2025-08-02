using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace EstacaoDeMonitoramentoVital
{
    public static class Banco
    {
        private static string dbFile = "monitoramento_vital.db";

        static Banco()
        {
            if (!File.Exists(dbFile))
            {
                SQLiteConnection.CreateFile(dbFile);
                CriarTabela();
                CriarTabelaUsuarios();
            }
        }

        public static SQLiteConnection Conexao()
        {
            var conn = new SQLiteConnection($"Data Source={dbFile};Version=3;");
            conn.Open();
            return conn;
        }

        private static void CriarTabela()
        {
            using (var conn = Conexao())
            {
                string sql = @"
                    CREATE TABLE IF NOT EXISTS Leituras (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        DataHora TEXT,
                        BPM INTEGER,
                        SpO2 INTEGER,
                        IR INTEGER,
                        RED INTEGER,
                        Latitude REAL,
                        Longitude REAL
                    );";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void CriarTabelaUsuarios()
        {
            using (var conn = Conexao())
            {
                string sql = @"
            CREATE TABLE IF NOT EXISTS Usuarios (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome TEXT NOT NULL,
                Senha TEXT NOT NULL
            );";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static bool UsuarioExiste(string nome)
        {
            using (var conn = Conexao())
            {
                string sql = "SELECT COUNT(*) FROM Usuarios WHERE Nome = @Nome;";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Nome", nome);
                    long count = (long)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        public static void CadastrarUsuario(string nome, string senha)
        {
            using (var conn = Conexao())
            {
                string sql = @"INSERT INTO Usuarios (Nome, Senha) VALUES (@Nome, @Senha);";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Nome", nome);
                    cmd.Parameters.AddWithValue("@Senha", senha);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static List<(int Id, string Nome, string Senha)> ListarUsuarios()
        {
            var lista = new List<(int, string, string)>();

            using (var conn = Conexao())
            {
                string sql = "SELECT Id, Nome, Senha FROM Usuarios ORDER BY Nome ASC";

                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        string nome = reader.GetString(1);
                        string senha = reader.GetString(2);

                        lista.Add((id, nome, senha));
                    }
                }
            }

            return lista;
        }



        public static bool ValidarLogin(string nome, string senha)
        {
            using (var conn = Conexao())
            {
                string sql = "SELECT * FROM Usuarios WHERE Nome = @Nome AND Senha = @Senha";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Nome", nome);
                    cmd.Parameters.AddWithValue("@Senha", senha);

                    using (var reader = cmd.ExecuteReader())
                    {
                        return reader.HasRows;
                    }
                }
            }
        }




        // ✅ CREATE com parâmetros simples e DataHora interna
        public static void Inserir(int bpm, int spo2, int ir, int red, double latitude, double longitude)
        {
            using (var conn = Conexao())
            {
                string sql = @"INSERT INTO Leituras 
                    (DataHora, BPM, SpO2, IR, RED, Latitude, Longitude)
                    VALUES (@DataHora, @BPM, @SpO2, @IR, @RED, @Latitude, @Longitude);";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@DataHora", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@BPM", bpm);
                    cmd.Parameters.AddWithValue("@SpO2", spo2);
                    cmd.Parameters.AddWithValue("@IR", ir);
                    cmd.Parameters.AddWithValue("@RED", red);
                    cmd.Parameters.AddWithValue("@Latitude", latitude);
                    cmd.Parameters.AddWithValue("@Longitude", longitude);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 🔵 READ (todos)
        public static List<Leitura> ObterTodas()
        {
            var lista = new List<Leitura>();
            using (var conn = Conexao())
            {
                string sql = "SELECT * FROM Leituras ORDER BY DataHora DESC";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new Leitura
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                DataHora = DateTime.Parse(reader["DataHora"].ToString()),
                                BPM = Convert.ToInt32(reader["BPM"]),
                                SpO2 = Convert.ToInt32(reader["SpO2"]),
                                IR = Convert.ToInt32(reader["IR"]),
                                RED = Convert.ToInt32(reader["RED"]),
                                Latitude = Convert.ToDouble(reader["Latitude"]),
                                Longitude = Convert.ToDouble(reader["Longitude"])
                            });
                        }
                    }
                }
            }
            return lista;
        }

        // 🟡 UPDATE
        public static void Atualizar(Leitura l)
        {
            using (var conn = Conexao())
            {
                string sql = @"UPDATE Leituras SET 
                    DataHora=@DataHora, BPM=@BPM, SpO2=@SpO2, IR=@IR, RED=@RED, Latitude=@Latitude, Longitude=@Longitude
                    WHERE Id=@Id";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@DataHora", l.DataHora.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@BPM", l.BPM);
                    cmd.Parameters.AddWithValue("@SpO2", l.SpO2);
                    cmd.Parameters.AddWithValue("@IR", l.IR);
                    cmd.Parameters.AddWithValue("@RED", l.RED);
                    cmd.Parameters.AddWithValue("@Latitude", l.Latitude);
                    cmd.Parameters.AddWithValue("@Longitude", l.Longitude);
                    cmd.Parameters.AddWithValue("@Id", l.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 🔴 DELETE
        public static void Deletar(int id)
        {
            using (var conn = Conexao())
            {
                string sql = "DELETE FROM Leituras WHERE Id=@Id";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DeletarTodos()
        {
            using (var conn = Conexao())
            {
                string sql = "DELETE FROM Leituras";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }


        // 🔍 FILTRAR POR INTERVALO DE DATA
        public static List<Leitura> FiltrarPorData(DateTime inicio, DateTime fim)
        {
            var lista = new List<Leitura>();
            using (var conn = Conexao())
            {
                string sql = @"SELECT * FROM Leituras 
                            WHERE DataHora BETWEEN @inicio AND @fim 
                            ORDER BY DataHora DESC";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@inicio", inicio.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@fim", fim.ToString("yyyy-MM-dd HH:mm:ss"));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new Leitura
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                DataHora = DateTime.Parse(reader["DataHora"].ToString()),
                                BPM = Convert.ToInt32(reader["BPM"]),
                                SpO2 = Convert.ToInt32(reader["SpO2"]),
                                IR = Convert.ToInt32(reader["IR"]),
                                RED = Convert.ToInt32(reader["RED"]),
                                Latitude = Convert.ToDouble(reader["Latitude"]),
                                Longitude = Convert.ToDouble(reader["Longitude"])
                            });
                        }
                    }
                }
            }
            return lista;
        }

        // 📊 ESTATÍSTICAS BÁSICAS
        public static (int total, double mediaBPM, double mediaSpO2) ObterEstatisticas()
        {
            using (var conn = Conexao())
            {
                string sql = "SELECT COUNT(*) AS Total, AVG(BPM) AS MediaBPM, AVG(SpO2) AS MediaSpO2 FROM Leituras";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return (
                                Convert.ToInt32(reader["Total"]),
                                Convert.ToDouble(reader["MediaBPM"]),
                                Convert.ToDouble(reader["MediaSpO2"])
                            );
                        }
                    }
                }
            }
            return (0, 0, 0);
        }
    }
}
