using System;

namespace GlobalChildrensApi.Models
{
    public class AulaTutorHistorico
    {
        public long AulaTutorHistoricoId { get; set; }
        public DateTime fecha_inicio { get; set; }
        public DateTime? fecha_fin { get; set; }
        public string? motivo_cambio { get; set; }
        public string estado { get; set; } = "ACT";
        public long AulaId { get; set; }
        public long TutorId { get; set; }
        public DateTime fecha_creacion { get; set; }
    }
}