using System;

namespace GlobalChildrensApi.Models
{
    public class Nota
    {
        public long NotaId { get; set; }
        public decimal valor { get; set; }
        public DateTime fecha_registro { get; set; }
        public string estado { get; set; } = "ACT";
        public long EstudianteId { get; set; }
        public long ComponenteNotaId { get; set; }
        public long PeriodoEvaluacionId { get; set; }
        public long TutorId { get; set; }
        public DateTime fecha_creacion { get; set; }
    }
}