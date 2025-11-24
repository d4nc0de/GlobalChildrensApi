using System;

namespace GlobalChildrensApi.Models
{
    public class Festivo
    {
        public long festivoid { get; set; }
        public DateTime fecha { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string estado { get; set; } = "ACT";
        public DateTime fecha_creacion { get; set; }
    }
}