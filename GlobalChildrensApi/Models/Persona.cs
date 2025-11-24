using System;

namespace GlobalChildrensApi.Models
{
    public class Persona
    {
        public long personaid { get; set; }
        public string numero_documento { get; set; } = string.Empty;
        public string nombres { get; set; } = string.Empty;
        public string apellidos { get; set; } = string.Empty;
        public string correo { get; set; } = string.Empty;
        public string? telefono { get; set; }
        public bool activo { get; set; }
        public string estado { get; set; } = "ACT";
        public long tipodocumentoid { get; set; }
        public int rolid { get; set; }
        public DateTime fecha_creacion { get; set; }
    }
}