using System;

namespace GlobalChildrensApi.Models
{
    public class Tutor
    {
        public long TutorId { get; set; }
        public string estado { get; set; } = "ACT";
        public long PersonaId { get; set; }
        public DateTime fecha_creacion { get; set; }
    }
}