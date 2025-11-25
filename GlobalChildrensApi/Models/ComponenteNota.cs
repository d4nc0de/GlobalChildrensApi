using System;

namespace GlobalChildrensApi.Models
{
    public class ComponenteNota
    {
        public long componentenotaid { get; set; }
        public string nombre { get; set; } = string.Empty;
        public decimal porcentaje { get; set; }
        public bool activo { get; set; }
        public string estado { get; set; } = "ACT";
        public DateTime fecha_creacion { get; set; } = DateTime.UtcNow;
    }
}