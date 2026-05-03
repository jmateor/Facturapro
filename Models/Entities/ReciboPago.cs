using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Facturapro.Models.Entities
{
    public class ReciboPago
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Factura")]
        public int FacturaId { get; set; }
        public Factura? Factura { get; set; }

        [Required]
        [Display(Name = "Cliente")]
        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        [Required]
        [Display(Name = "Fecha de Pago")]
        public DateTime FechaPago { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Monto Efectivo")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoEfectivo { get; set; }

        [Required]
        [Display(Name = "Monto Tarjeta")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoTarjeta { get; set; }

        [Required]
        [Display(Name = "Monto Transferencia/Cheque")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoTransferencia { get; set; }

        [Display(Name = "Monto Total Aplicado")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoTotal => MontoEfectivo + MontoTarjeta + MontoTransferencia;

        [StringLength(100)]
        [Display(Name = "Referencia (Cheque, Transf, Bauche)")]
        public string? Referencia { get; set; }

        [StringLength(500)]
        [Display(Name = "Notas")]
        public string? Notas { get; set; }

        [Required]
        [StringLength(450)]
        [Display(Name = "Usuario que recibe")]
        public string UsuarioId { get; set; } = string.Empty;
        public ApplicationUser? Usuario { get; set; }
    }
}
