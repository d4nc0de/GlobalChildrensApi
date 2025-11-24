using System;

namespace GlobalChildrensApi.Models
{
    public class Programa
    {
        public long ProgramaId { get; set; }
        public string codigo { get; set; } = string.Empty;
        public string nombre { get; set; } = string.Empty;
        public string? descripcion { get; set; }
        public string estado { get; set; } = "ACT";
        public DateTime fecha_creacion { get; set; }
    }
}