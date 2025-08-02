using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EstacaoDeMonitoramentoVital
{
    public class Leitura
    {
        public int Id { get; set; }
        public DateTime DataHora { get; set; }
        public int BPM { get; set; }
        public int SpO2 { get; set; }
        public int IR { get; set; }
        public int RED { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}

