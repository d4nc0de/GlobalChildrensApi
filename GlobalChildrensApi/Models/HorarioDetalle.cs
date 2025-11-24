using System;

namespace GlobalChildrensApi.Models
{
    public class HorarioDetalle
    {
        public long horariodetalleid { get; set; }
        public short dia_semana { get; set; }
        public TimeSpan hora_inicio { get; set; }
        public TimeSpan hora_fin { get; set; }
        public int unidades { get; set; }
        public string estado { get; set; } = "ACT";
        public long horarioid { get; set; }
        public DateTime fecha_creacion { get; set; }
    }
}