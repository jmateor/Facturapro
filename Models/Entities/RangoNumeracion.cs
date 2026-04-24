using System.ComponentModel.DataAnnotations;

namespace Facturapro.Models.Entities
{
    /// <summary>
    /// Rangos de numeración autorizados por la DGII para emisión de e-CF
    /// </summary>
    public class RangoNumeracion
    {
        public int Id { get; set; }

        [Required]
        [StringLength(2)]
        [Display(Name = "Tipo de Comprobante")]
        public string TipoECF { get; set; } = "31"; // 31, 32, 33, 34, etc.

        [Required]
        [StringLength(13)]
        [Display(Name = "Rango Desde")]
        public string RangoDesde { get; set; } = string.Empty; // Ej: E31000000001

        [Required]
        [StringLength(13)]
        [Display(Name = "Rango Hasta")]
        public string RangoHasta { get; set; } = string.Empty; // Ej: E31000999999

        [Display(Name = "Número Actual")]
        public long NumeroActual { get; set; }

        [Display(Name = "Número Siguiente")]
        public long NumeroSiguiente => NumeroActual + 1;

        [Display(Name = "Cantidad Disponible")]
        public long CantidadDisponible => long.Parse(RangoHasta.Substring(3)) - NumeroActual;

        [Display(Name = "Vencimiento")]
        [DataType(DataType.Date)]
        public DateTime FechaVencimiento { get; set; }

        [Display(Name = "Estado")]
        public EstadoRango Estado { get; set; } = EstadoRango.Activo;

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        [StringLength(500)]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Método para obtener el siguiente número de e-CF
        public string? ObtenerSiguienteNumero()
        {
            if (Estado != EstadoRango.Activo || CantidadDisponible <= 0)
                return null;

            var secuencial = NumeroActual + 1;
            var numeroFormateado = secuencial.ToString("D10");
            return $"E{TipoECF}{numeroFormateado}";
        }

        // Método para incrementar el contador
        public bool Incrementar()
        {
            if (CantidadDisponible <= 0)
            {
                Estado = EstadoRango.Agotado;
                return false;
            }

            NumeroActual++;

            if (CantidadDisponible == 0)
            {
                Estado = EstadoRango.Agotado;
            }

            return true;
        }

        // Propiedad de solo lectura
        [Display(Name = "Progreso")]
        public double PorcentajeUsado
        {
            get
            {
                var total = long.Parse(RangoHasta.Substring(3)) - long.Parse(RangoDesde.Substring(3)) + 1;
                var usado = NumeroActual - long.Parse(RangoDesde.Substring(3)) + 1;
                return (usado / (double)total) * 100;
            }
        }
    }

    public enum EstadoRango
    {
        Activo = 1,
        Agotado = 2,
        Vencido = 3,
        Suspendido = 4
    }
}
