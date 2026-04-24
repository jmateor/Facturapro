using Facturapro.Data;
using Facturapro.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Facturapro.Controllers
{
    [Authorize]
    public class POSController : Controller
    {
        private readonly ApplicationDbContext _context;

        public POSController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: POS
        public async Task<IActionResult> Index()
        {
            // Cargar clientes activos
            ViewData["Clientes"] = await _context.Clientes
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            // Cargar productos activos con stock
            ViewData["Productos"] = await _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Activo && p.Stock > 0)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            // Recuperar carrito de la sesión
            var carrito = ObtenerCarrito();
            ViewData["Carrito"] = carrito;
            CalcularTotales(carrito);

            return View();
        }

        // POST: POS/AgregarAlCarrito
        [HttpPost]
        public async Task<IActionResult> AgregarAlCarrito(int productoId, int cantidad = 1)
        {
            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null)
            {
                return Json(new { success = false, message = "Producto no encontrado" });
            }

            if (producto.Stock < cantidad)
            {
                return Json(new { success = false, message = $"Stock insuficiente. Disponible: {producto.Stock}" });
            }

            var carrito = ObtenerCarrito();

            // Verificar si el producto ya está en el carrito
            var itemExistente = carrito.FirstOrDefault(i => i.ProductoId == productoId);
            if (itemExistente != null)
            {
                // Verificar stock con la nueva cantidad
                if (producto.Stock < (itemExistente.Cantidad + cantidad))
                {
                    return Json(new { success = false, message = $"Stock insuficiente. Disponible: {producto.Stock}" });
                }
                itemExistente.Cantidad += cantidad;
            }
            else
            {
                carrito.Add(new CarritoItem
                {
                    ProductoId = producto.Id,
                    Codigo = producto.Codigo,
                    Nombre = producto.Nombre,
                    Precio = producto.Precio,
                    Cantidad = cantidad
                });
            }

            GuardarCarrito(carrito);

            var totales = CalcularTotales(carrito);
            return Json(new { success = true, carritoCount = carrito.Count, totales });
        }

        // POST: POS/ActualizarCantidad
        [HttpPost]
        public async Task<IActionResult> ActualizarCantidad(int productoId, int cantidad)
        {
            if (cantidad < 1)
            {
                return Json(new { success = false, message = "La cantidad debe ser mayor a cero" });
            }

            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null)
            {
                return Json(new { success = false, message = "Producto no encontrado" });
            }

            if (producto.Stock < cantidad)
            {
                return Json(new { success = false, message = $"Stock insuficiente. Disponible: {producto.Stock}" });
            }

            var carrito = ObtenerCarrito();
            var item = carrito.FirstOrDefault(i => i.ProductoId == productoId);

            if (item != null)
            {
                item.Cantidad = cantidad;
                GuardarCarrito(carrito);
            }

            var totales = CalcularTotales(carrito);
            return Json(new { success = true, totales });
        }

        // POST: POS/EliminarDelCarrito
        [HttpPost]
        public IActionResult EliminarDelCarrito(int productoId)
        {
            var carrito = ObtenerCarrito();
            carrito.RemoveAll(i => i.ProductoId == productoId);
            GuardarCarrito(carrito);

            var totales = CalcularTotales(carrito);
            return Json(new { success = true, carritoCount = carrito.Count, totales });
        }

        // POST: POS/LimpiarCarrito
        [HttpPost]
        public IActionResult LimpiarCarrito()
        {
            HttpContext.Session.Remove("CarritoPOS");
            return Json(new { success = true });
        }

        // POST: POS/ProcesarVenta
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarVenta([Bind("ClienteId,TipoNcf,Notas")] POSVentaViewModel venta)
        {
            var carrito = ObtenerCarrito();
            if (!carrito.Any())
            {
                TempData["ErrorMessage"] = "El carrito está vacío";
                return RedirectToAction(nameof(Index));
            }

            // Validar cliente
            var cliente = await _context.Clientes.FindAsync(venta.ClienteId);
            if (cliente == null)
            {
                TempData["ErrorMessage"] = "Debe seleccionar un cliente";
                return RedirectToAction(nameof(Index));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Verificar stock disponible
                foreach (var item in carrito)
                {
                    var producto = await _context.Productos.FindAsync(item.ProductoId);
                    if (producto == null || producto.Stock < item.Cantidad)
                    {
                        TempData["ErrorMessage"] = $"Stock insuficiente para {item.Nombre}";
                        return RedirectToAction(nameof(Index));
                    }
                }

                // Generar número de factura
                var ultimaFactura = await _context.Facturas
                    .OrderByDescending(f => f.Id)
                    .FirstOrDefaultAsync();
                var numeroFactura = $"FAC-{DateTime.Now:yyyy}-{((ultimaFactura?.Id ?? 0) + 1):D5}";

                // Calcular totales
                var subtotal = carrito.Sum(i => i.SubTotal);
                var montoItbis = carrito.Sum(i => i.ITBIS);
                var total = carrito.Sum(i => i.Total);

                // Crear la factura
                var factura = new Factura
                {
                    NumeroFactura = numeroFactura,
                    ClienteId = venta.ClienteId,
                    FechaEmision = DateTime.Now,
                    FechaVencimiento = DateTime.Now.AddDays(30),
                    Estado = "Pagada",
                    Notas = venta.Notas,
                    Subtotal = subtotal,
                    PorcentajeITBIS = 18,
                    MontoITBIS = montoItbis,
                    TotalITBIS = montoItbis,
                    Total = total,
                    Lineas = new List<FacturaLinea>()
                };

                // Asignar NCF si es RD
                if (!string.IsNullOrEmpty(venta.TipoNcf) && venta.TipoNcf != "NO_NCF")
                {
                    var rango = await _context.RangoNumeraciones
                        .FirstOrDefaultAsync(r => r.TipoECF == venta.TipoNcf && r.Estado == EstadoRango.Activo);

                    if (rango != null)
                    {
                        factura.TipoECF = venta.TipoNcf;
                        factura.eNCF = rango.ObtenerSiguienteNumero();
                        factura.EstadoDGII = "Emitida";

                        // Incrementar el contador del rango
                        rango.Incrementar();
                        _context.RangoNumeraciones.Update(rango);
                    }
                }

                // Crear líneas de factura y actualizar stock
                int lineaNumero = 1;
                foreach (var item in carrito)
                {
                    var producto = await _context.Productos.FindAsync(item.ProductoId);

                    factura.Lineas.Add(new FacturaLinea
                    {
                        FacturaId = factura.Id,
                        NumeroLinea = lineaNumero++,
                        Descripcion = item.Nombre,
                        NombreItem = item.Nombre,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.Precio,
                        UnidadMedida = "UNIDAD",
                        Descuento = 0,
                        MontoDescuento = 0,
                        MontoITBIS = item.ITBIS,
                        Subtotal = item.SubTotal,
                        Orden = lineaNumero - 1
                    });

                    // Actualizar stock
                    var stockAnterior = producto!.Stock;
                    producto.Stock -= item.Cantidad;

                    // Registrar movimiento de inventario
                    _context.MovimientosInventario.Add(new MovimientoInventario
                    {
                        ProductoId = item.ProductoId,
                        TipoMovimiento = TipoMovimiento.SalidaVenta,
                        Cantidad = item.Cantidad,
                        StockAnterior = stockAnterior,
                        StockNuevo = producto.Stock,
                        Motivo = $"Venta POS - Factura {numeroFactura}",
                        FacturaId = factura.Id,
                        FechaMovimiento = DateTime.Now,
                        UsuarioRegistro = User.Identity?.Name ?? "POS"
                    });
                }

                _context.Facturas.Add(factura);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Limpiar carrito
                HttpContext.Session.Remove("CarritoPOS");

                TempData["SuccessMessage"] = $"Venta procesada exitosamente. Factura: {numeroFactura}";
                return RedirectToAction(nameof(Completada), new { id = factura.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Error al procesar la venta: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: POS/Completada/5
        public async Task<IActionResult> Completada(int id)
        {
            var factura = await _context.Facturas
                .Include(f => f.Cliente)
                .Include(f => f.Lineas)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null)
            {
                return NotFound();
            }

            return View(factura);
        }

        // POST: POS/BuscarProductos
        [HttpPost]
        public async Task<IActionResult> BuscarProductos(string termino)
        {
            var productos = await _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Activo && p.Stock > 0 &&
                           (p.Nombre.Contains(termino) ||
                            p.Codigo.Contains(termino) ||
                            (p.Categoria != null && p.Categoria.Nombre.Contains(termino))))
                .Take(10)
                .ToListAsync();

            return Json(productos.Select(p => new
            {
                p.Id,
                p.Codigo,
                p.Nombre,
                p.Precio,
                p.Stock,
                Categoria = p.Categoria?.Nombre
            }));
        }

        // POST: POS/BuscarPorCodigoBarras
        [HttpPost]
        public async Task<IActionResult> BuscarPorCodigoBarras(string codigoBarras)
        {
            if (string.IsNullOrEmpty(codigoBarras))
            {
                return Json(new { success = false, message = "Código de barras vacío" });
            }

            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.CodigoBarras == codigoBarras || p.Codigo == codigoBarras);

            if (producto == null)
            {
                return Json(new { success = false, message = "Producto no encontrado" });
            }

            if (!producto.Activo)
            {
                return Json(new { success = false, message = "Producto inactivo" });
            }

            if (producto.Stock <= 0)
            {
                return Json(new { success = false, message = $"Stock insuficiente. Disponible: {producto.Stock}" });
            }

            return Json(new
            {
                success = true,
                producto = new
                {
                    producto.Id,
                    producto.Codigo,
                    producto.Nombre,
                    producto.Precio,
                    producto.Stock
                }
            });
        }

        // POST: POS/AgregarPorCodigoBarras
        [HttpPost]
        public async Task<IActionResult> AgregarPorCodigoBarras(string codigoBarras)
        {
            if (string.IsNullOrEmpty(codigoBarras))
            {
                return Json(new { success = false, message = "Código de barras vacío" });
            }

            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.CodigoBarras == codigoBarras || p.Codigo == codigoBarras);

            if (producto == null)
            {
                return Json(new { success = false, message = "Producto no encontrado" });
            }

            // Usar el método existente para agregar al carrito
            return await AgregarAlCarrito(producto.Id, 1);
        }

        private List<CarritoItem> ObtenerCarrito()
        {
            var carritoJson = HttpContext.Session.GetString("CarritoPOS");
            if (string.IsNullOrEmpty(carritoJson))
            {
                return new List<CarritoItem>();
            }
            return JsonSerializer.Deserialize<List<CarritoItem>>(carritoJson) ?? new List<CarritoItem>();
        }

        private void GuardarCarrito(List<CarritoItem> carrito)
        {
            HttpContext.Session.SetString("CarritoPOS", JsonSerializer.Serialize(carrito));
        }

        private object CalcularTotales(List<CarritoItem> carrito)
        {
            decimal subtotal = carrito.Sum(i => i.SubTotal);
            decimal itbis = carrito.Sum(i => i.ITBIS);
            decimal total = carrito.Sum(i => i.Total);

            return new
            {
                subtotal = subtotal.ToString("C", new System.Globalization.CultureInfo("es-DO")),
                itbis = itbis.ToString("C", new System.Globalization.CultureInfo("es-DO")),
                total = total.ToString("C", new System.Globalization.CultureInfo("es-DO")),
                subtotalRaw = subtotal,
                itbisRaw = itbis,
                totalRaw = total
            };
        }
    }

    public class CarritoItem
    {
        public int ProductoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
        public decimal SubTotal => Precio * Cantidad;
        public decimal ITBIS => SubTotal * 0.18m; // 18% ITBIS
        public decimal Total => SubTotal + ITBIS;
    }

    public class POSVentaViewModel
    {
        public int ClienteId { get; set; }
        public string? TipoNcf { get; set; }
        public string? Notas { get; set; }
    }
}
