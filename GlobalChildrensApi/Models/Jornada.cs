using System;

namespace GlobalChildrensApi.Models
{
    public class Jornada
    {
        public long jornadaid { get; set; }
        public string codigo { get; set; } = string.Empty;
        public string descripcion { get; set; } = string.Empty;
        public string estado { get; set; } = "ACT";
        public DateTime fecha_creacion { get; set; } = DateTime.UtcNow;
    }
}