using System;

namespace GlobalChildrensApi.Models
{
    public class Sede
    {
        public long sedeid { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string direccion { get; set; } = string.Empty;
        public bool es_principal { get; set; }
        public string estado { get; set; } = "ACT";
        public long institucionid { get; set; }
        public DateTime fecha_creacion { get; set; } = DateTime.UtcNow;
    }
}
