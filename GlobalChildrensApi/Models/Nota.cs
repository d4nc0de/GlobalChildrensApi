using System;

namespace GlobalChildrensApi.Models
{
    public class Nota
    {
        public long notaid { get; set; }
        public decimal valor { get; set; }
        public DateTime fecha_registro { get; set; }
        public string estado { get; set; } = "ACT";
        public long estudianteid { get; set; }
        public long componentenotaid { get; set; }
        public long periodoevaluacionid { get; set; }
        public long tutorid { get; set; }
        public DateTime fecha_creacion { get; set; } = DateTime.UtcNow;
    }
}