using System;

namespace GlobalChildrensApi.Models
{
    public class SesionClase
    {
        public long SesionClaseId { get; set; }
        public DateTime fecha_real { get; set; }
        public short dia_semana { get; set; }
        public TimeSpan hora_inicio_programada { get; set; }
        public TimeSpan hora_fin_programada { get; set; }
        public int minutos_dictados { get; set; }
        public bool clase_dictada { get; set; }
        public bool es_reposicion { get; set; }
        public string estado { get; set; } = "ACT";
        public long TutorId { get; set; }
        public long AulaId { get; set; }
        public long CalendarioSemanalProgramaId { get; set; }
        public long? MotivoNoClaseId { get; set; }
        public long? FestivoId { get; set; }
        public long? SesionRepuestaId { get; set; }
        public DateTime fecha_creacion { get; set; }
    }
}