using System;

namespace GlobalChildrensApi.Models
{
    public class EstudianteAulaHistorico
    {
        public long EstudianteAulaHistoricoId { get; set; }
        public DateTime fecha_inicio { get; set; }
        public DateTime? fecha_fin { get; set; }
        public string? motivo_cambio { get; set; }
        public string estado { get; set; } = "ACT";
        public long EstudianteId { get; set; }
        public long AulaId { get; set; }
        public DateTime fecha_creacion { get; set; }
    }
}