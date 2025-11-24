using System;

namespace GlobalChildrensApi.Models
{
    public class MotivoInasistenciaEstudiante
    {
        public long motivoinasistenciaestudianteid { get; set; }
        public string codigo { get; set; } = string.Empty;
        public string descripcion { get; set; } = string.Empty;
        public string estado { get; set; } = "ACT";
        public DateTime fecha_creacion { get; set; }
    }
}