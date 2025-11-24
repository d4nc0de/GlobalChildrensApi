using System;

namespace GlobalChildrensApi.Models
{
    public class MotivoNoClase
    {
        public long motivonoclaseid { get; set; }
        public string codigo { get; set; } = string.Empty;
        public string descripcion { get; set; } = string.Empty;
        public bool permite_reposicion { get; set; }
        public string estado { get; set; } = "ACT";
        public DateTime fecha_creacion { get; set; }
    }
}