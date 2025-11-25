using System;

namespace GlobalChildrensApi.Models
{
    public class Horario
    {
        public long horarioid { get; set; }
        public int minutos_por_unidad { get; set; }
        public string? descripcion { get; set; }
        public string estado { get; set; } = "ACT";
        public long jornadaid { get; set; }
        public DateTime fecha_creacion { get; set; } = DateTime.UtcNow;
    }
}