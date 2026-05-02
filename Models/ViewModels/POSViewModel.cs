namespace Facturapro.Models.ViewModels
{
    public class POSViewModel
    {
        public List<Entities.Cliente> Clientes { get; set; } = new();
        public List<Entities.Producto> Productos { get; set; } = new();
        public List<Controllers.CarritoItem> Carrito { get; set; } = new();
        public string CarritoJson { get; set; } = "[]";
        public decimal TasaUSD { get; set; } = 58.50m;
    }
}
