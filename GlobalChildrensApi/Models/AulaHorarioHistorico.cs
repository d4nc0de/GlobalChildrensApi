using System;

namespace GlobalChildrensApi.Models
{
    public class AulaHorarioHistorico
    {
        public long aulahorariohistoricoid { get; set; }
        public DateTime fecha_inicio { get; set; }
        public DateTime? fecha_fin { get; set; }
        public string? motivo_cambio { get; set; }
        public string estado { get; set; } = "ACT";
        public long aulaid { get; set; }
        public long horarioid { get; set; }
        public DateTime fecha_creacion { get; set; }
    }
}