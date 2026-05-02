using System.ComponentModel.DataAnnotations;

namespace Facturapro.Models.Entities
{
    public class ConfiguracionDispositivo
    {
        public int Id { get; set; }

        // Impresora Térmica
        [Display(Name = "Habilitar Impresora Térmica")]
        public bool HabilitarImpresora { get; set; } = true;

        [Display(Name = "Ancho de Papel")]
        public string AnchoPapel { get; set; } = "80mm"; // 58mm, 80mm

        [Display(Name = "Cortar Papel Automáticamente")]
        public bool CorteAutomatico { get; set; } = true;

        [Display(Name = "Abrir Cajón de Dinero")]
        public bool AbrirCajon { get; set; } = true;

        [Display(Name = "Imprimir Copia de Seguridad")]
        public bool ImprimirCopia { get; set; } = false;

        // Lector de Código de Barras
        [Display(Name = "Habilitar Lector de Código")]
        public bool HabilitarLector { get; set; } = true;

        [Display(Name = "Modo de Escaneo")]
        public string ModoEscaneo { get; set; } = "Automatico"; // Automatico, Manual

        [Display(Name = "Sufijo de Lectura")]
        public string SufijoLectura { get; set; } = "Enter"; // Enter, Tab, None

        [Display(Name = "Pitido al Escanear")]
        public bool SonidoEscaneo { get; set; } = true;

        // Otros Dispositivos
        [Display(Name = "Habilitar Pantalla de Cliente")]
        public bool HabilitarPantallaCliente { get; set; } = false;

        [Display(Name = "Puerto Pantalla Cliente")]
        public string? PuertoPantallaCliente { get; set; }

        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    }
}
