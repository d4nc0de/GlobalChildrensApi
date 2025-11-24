using System;

namespace GlobalChildrensApi.Models
{
    public class Estudiante
    {
        public long EstudianteId { get; set; }
        public string numero_documento { get; set; } = string.Empty;
        public string nombres { get; set; } = string.Empty;
        public string apellidos { get; set; } = string.Empty;
        public DateTime fecha_nacimiento { get; set; }
        public char sexo { get; set; }
        public bool activo { get; set; }
        public string estado { get; set; } = "ACT";
        public long TipoDocumentoId { get; set; }
        public long AulaId { get; set; }
        public DateTime fecha_creacion { get; set; }
    }
}