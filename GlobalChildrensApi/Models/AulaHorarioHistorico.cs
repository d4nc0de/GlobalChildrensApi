using System;

namespace GlobalChildrensApi.Models
{
    public class AulaHorarioHistorico
    {
        public long AulaHorarioHistoricoId { get; set; }
        public DateTime fecha_inicio { get; set; }
        public DateTime? fecha_fin { get; set; }
        public string? motivo_cambio { get; set; }
        public string estado { get; set; } = "ACT";
        public long AulaId { get; set; }
        public long HorarioId { get; set; }
        public DateTime fecha_creacion { get; set; }
    }
}