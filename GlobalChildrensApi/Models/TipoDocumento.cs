using System;

namespace GlobalChildrensApi.Models
{
    public class TipoDocumento
    {
        public long tipodocumentoid { get; set; }
        public string codigo { get; set; } = string.Empty;
        public string nombre { get; set; } = string.Empty;
        public string estado { get; set; } = "ACT";
        public DateTime fecha_creacion { get; set; }
    }
}