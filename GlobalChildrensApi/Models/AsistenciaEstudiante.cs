using System;

namespace GlobalChildrensApi.Models
{
    public class AsistenciaEstudiante
    {
        public long AsistenciaEstudianteId { get; set; }
        public bool asistio { get; set; }
        public string? observacion { get; set; }
        public bool justificada { get; set; }
        public string estado { get; set; } = "ACT";
        public long SesionClaseId { get; set; }
        public long EstudianteId { get; set; }
        public long? MotivoInasistenciaEstudianteId { get; set; }
        public DateTime fecha_creacion { get; set; }
    }
}