using System;

namespace GlobalChildrensApi.Models
{
    public class Aula
    {
        public long aulaid { get; set; }
        public int grado { get; set; }
        public string nombre { get; set; } = string.Empty;
        public int cupo_maximo { get; set; }
        public bool activo { get; set; } = true;
        public string estado { get; set; } = "ACT";
        public long sedeid { get; set; }
        public long programaid { get; set; }
        public long jornadaid { get; set; }
        public long tutorid { get; set; }
        public DateTime fecha_creacion { get; set; } = DateTime.UtcNow;
    }
}

