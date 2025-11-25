using System;

namespace GlobalChildrensApi.Models
{
    public class AulaTutorHistorico
    {
        public long aulatutorhistoricoid { get; set; }
        public DateTime fecha_inicio { get; set; }
        public DateTime? fecha_fin { get; set; }
        public string? motivo_cambio { get; set; }
        public string estado { get; set; } = "ACT";
        public long aulaid { get; set; }
        public long tutorid { get; set; }
        public DateTime fecha_creacion { get; set; } = DateTime.UtcNow;
    }
}