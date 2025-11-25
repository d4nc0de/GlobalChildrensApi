using System;

namespace GlobalChildrensApi.Models
{
    public class PeriodoEvaluacion
    {
        public long periodoevaluacionid { get; set; }
        public string nombre { get; set; } = string.Empty;
        public DateTime fecha_inicio { get; set; }
        public DateTime fecha_fin { get; set; }
        public int orden { get; set; }
        public string estado { get; set; } = "ACT";
        public DateTime fecha_creacion { get; set; } = DateTime.UtcNow;
    }
}