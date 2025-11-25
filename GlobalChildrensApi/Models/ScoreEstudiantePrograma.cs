using System;

namespace GlobalChildrensApi.Models
{
    public class ScoreEstudiantePrograma
    {
        public long scoreestudianteprogramaid { get; set; }
        public string tipo_score { get; set; } = string.Empty;
        public decimal valor { get; set; }
        public DateTime fecha_registro { get; set; }
        public string estado { get; set; } = "ACT";
        public long estudianteid { get; set; }
        public long programaid { get; set; }
        public DateTime fecha_creacion { get; set; } = DateTime.UtcNow;
    }
}