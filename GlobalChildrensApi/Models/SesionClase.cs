using System;

namespace GlobalChildrensApi.Models
{
    public class SesionClase
    {
        public long sesionclaseid { get; set; }
        public DateTime fecha_real { get; set; }
        public short dia_semana { get; set; }
        public TimeSpan hora_inicio_programada { get; set; }
        public TimeSpan hora_fin_programada { get; set; }
        public int minutos_dictados { get; set; }
        public bool clase_dictada { get; set; }
        public bool es_reposicion { get; set; }
        public string estado { get; set; } = "ACT";
        public long tutorid { get; set; }
        public long aulaid { get; set; }
        public long calendariosemenalprogramaid { get; set; }
        public long? motivonoclaseid { get; set; }
        public long? festivoid { get; set; }
        public long? sesionrepuestaid { get; set; }
        public DateTime fecha_creacion { get; set; }
    }
}