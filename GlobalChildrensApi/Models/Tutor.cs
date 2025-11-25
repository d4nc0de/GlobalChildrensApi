using System;

namespace GlobalChildrensApi.Models
{
    public class Tutor
    {
        public long tutorid { get; set; }
        public string estado { get; set; } = "ACT";
        public long personaid { get; set; }
        public DateTime fecha_creacion { get; set; } = DateTime.UtcNow;
    }
}