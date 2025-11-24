using System;

namespace GlobalChildrensApi.Models
{
    public class EstudianteAulaHistorico
    {
        public long estudianteaulahistoricoid { get; set; }
        public DateTime fecha_inicio { get; set; }
        public DateTime? fecha_fin { get; set; }
        public string? motivo_cambio { get; set; }
        public string estado { get; set; } = "ACT";
        public long estudianteid { get; set; }
        public long aulaid { get; set; }
        public DateTime fecha_creacion { get; set; }
    }
}