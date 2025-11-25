using System;

namespace GlobalChildrensApi.Models
{
    public class CalendarioSemanalPrograma
    {
        public long calendariosemanalprogramaid { get; set; }
        public int anio { get; set; }
        public int numero_semana { get; set; }
        public DateTime fecha_inicio { get; set; }
        public DateTime fecha_fin { get; set; }
        public string estado { get; set; } = "ACT";
        public long programaid { get; set; }
        public DateTime fecha_creacion { get; set; } = DateTime.UtcNow;
    }
}