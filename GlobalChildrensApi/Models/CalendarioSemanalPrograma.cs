using System;

namespace GlobalChildrensApi.Models
{
    public class CalendarioSemanalPrograma
    {
        public long CalendarioSemanalProgramaId { get; set; }
        public int anio { get; set; }
        public int numero_semana { get; set; }
        public DateTime fecha_inicio { get; set; }
        public DateTime fecha_fin { get; set; }
        public string estado { get; set; } = "ACT";
        public long ProgramaId { get; set; }
        public DateTime fecha_creacion { get; set; }
    }
}