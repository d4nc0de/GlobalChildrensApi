using System;

namespace GlobalChildrensApi.Models
{
    public class AsistenciaEstudiante
    {
        public long asistenciaestudianteid { get; set; }
        public bool asistio { get; set; }
        public string? observacion { get; set; }
        public bool justificada { get; set; }
        public string estado { get; set; } = "ACT";
        public long sesionclaseid { get; set; }
        public long estudianteid { get; set; }
        public long? motivoinasistenciaestudianteid { get; set; }
        public DateTime fecha_creacion { get; set; } = DateTime.UtcNow;
    }
}