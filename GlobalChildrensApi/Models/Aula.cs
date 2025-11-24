using System;

namespace GlobalChildrensApi.Models
{
    public class Aula
    {
        public long AulaId { get; set; }
        public int grado { get; set; }
        public string nombre { get; set; } = string.Empty;
        public int cupo_maximo { get; set; }
        public bool activo { get; set; }
        public string estado { get; set; } = "ACT";
        public long SedeId { get; set; }
        public long ProgramaId { get; set; }
        public long JornadaId { get; set; }
        public long TutorId { get; set; }
        public DateTime fecha_creacion { get; set; }
    }
}