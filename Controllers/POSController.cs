using Facturapro.Data;
using Facturapro.Models.Entities;
using Facturapro.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Facturapro.Controllers
{
    [Authorize]
    public class POSController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Services.IAuditService _auditService;
        private readonly Services.Currency.IExchangeRateService _exchangeRateService;

        public POSController(ApplicationDbContext context, Services.IAuditService auditService, Services.Currency.IExchangeRateService exchangeRateService)
        {
            _context = context;
            _auditService = auditService;
            _exchangeRateService = exchangeRateService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTasaUSD()
        {
            var rate = await _exchangeRateService.GetLatestRateAsync();
            return Json(new { rate });
        }

        // GET: POS/VentasDelDia
        public async Task<IActionResult> VentasDelDia()
        {
            var hoy = DateTime.Now.Date;
            var manana = hoy.AddDays(1);

            var ventas = await _context.Facturas
                .Where(f => f.FechaEmision >= hoy && f.FechaEmision < manana)
                .OrderByDescending(f => f.FechaEmision)
                .Take(10)
                .Include(f => f.Cliente)
                .ToListAsync();

            var totalVentas = ventas.Sum(f => f.Total);
            var totalTransacciones = ventas.Count;

            return Json(new
            {
                ventas = ventas.Select(f => new
                {
                    f.Id,
                    f.NumeroFactura,
                    f.Total,
                    Fecha = f.FechaEmision.ToString("HH:mm"),
                    Cliente = f.Cliente?.Nombre ?? "Consumidor Final"
                }),
                resumen = new
                {
                    total = totalVentas.ToString("C", new System.Globalization.CultureInfo("es-DO")),
                    totalRaw = totalVentas,
                    transacciones = totalTransacciones
                }
            });
        }

        public async Task<IActionResult> GetCategorias()
        {
            var almacen = await _context.Almacenes.FirstOrDefaultAsync(a => a.EsPrincipalAlmacen && a.Activo);
            
            var categorias = await _context.Categorias
                .Where(c => c.Activo)
                .Select(c => new
                {
                    c.Id,
                    c.Nombre,
                    CantidadProductos = _context.StocksAlmacen.Count(s => s.AlmacenId == almacen.Id && s.Producto.CategoriaId == c.Id && s.Producto.Activo && s.Cantidad > 0)
                })
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            return Json(categorias);
        }

        // POST: POS/FiltrarProductos
        [HttpPost]
        public async Task<IActionResult> FiltrarProductos(int? categoriaId, string termino, bool soloStock = false)
        {
            // Obtener el almacén principal por defecto (por ahora el primero activo)
            var almacen = await _context.Almacenes.FirstOrDefaultAsync(a => a.EsPrincipalAlmacen && a.Activo);
            if (almacen == null) return Json(new List<object>());

            var query = _context.StocksAlmacen
                .Include(s => s.Producto)
                .ThenInclude(p => p.Categoria)
                .Where(s => s.AlmacenId == almacen.Id && s.Producto.Activo && s.Cantidad > 0);

            if (categoriaId.HasValue && categoriaId.Value > 0)
            {
                query = query.Where(s => s.Producto.CategoriaId == categoriaId.Value);
            }

            if (!string.IsNullOrEmpty(termino))
            {
                query = query.Where(s =>
                    s.Producto.Nombre.Contains(termino) ||
                    s.Producto.Codigo.Contains(termino) ||
                    (s.Producto.Categoria != null && s.Producto.Categoria.Nombre.Contains(termino)));
            }

            if (soloStock)
            {
                query = query.Where(s => s.Cantidad > 10);
            }

            var productos = await query
                .OrderBy(s => s.Producto.Nombre)
                .Take(50)
                .ToListAsync();

            return Json(productos.Select(s => new
            {
                s.Producto.Id,
                s.Producto.Codigo,
                s.Producto.CodigoBarras,
                s.Producto.Nombre,
                s.Producto.Precio,
                Stock = s.Cantidad,
                Categoria = s.Producto.Categoria?.Nombre,
                StockBajo = s.Cantidad <= 5
            }));
        }

        // GET: POS
        public async Task<IActionResult> Index()
        {
            var userId = _context.Users.FirstOrDefault(u => u.UserName == User.Identity!.Name)?.Id;
            var sesionActiva = await _context.SesionesCaja
                .FirstOrDefaultAsync(s => s.UsuarioId == userId && s.Estado == "Abierta");

            ViewBag.RequiereAperturaCaja = sesionActiva == null;

            // Cargar clientes activos
            var clientes = await _context.Clientes
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            // Obtener el almacén principal
            var almacenPrincipal = await _context.Almacenes.FirstOrDefaultAsync(a => a.EsPrincipalAlmacen && a.Activo);
            
            if (almacenPrincipal == null)
            {
                // Si no hay almacén, redirigir al Home para que se ejecute la auto-sanación
                TempData["InfoMessage"] = "Inicializando sistema de sucursales...";
                return RedirectToAction("Index", "Home");
            }

            // Cargar productos activos con stock en ese almacén
            var productos = await _context.StocksAlmacen
                .Include(s => s.Producto)
                .ThenInclude(p => p.Categoria)
                .Where(s => s.AlmacenId == almacenPrincipal.Id && s.Producto.Activo && s.Cantidad > 0)
                .Select(s => s.Producto)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            // Recuperar carrito de la sesión
            var carrito = ObtenerCarrito();

            var viewModel = new POSViewModel
            {
                Clientes = clientes,
                Productos = productos,
                Carrito = carrito,
                CarritoJson = System.Text.Json.JsonSerializer.Serialize(carrito, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }),
                TasaUSD = (await _context.ConfiguracionIntegraciones.FirstOrDefaultAsync())?.TasaUSD ?? 58.50m
            };

            CalcularTotales(carrito);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> AbrirCaja(decimal montoInicial)
        {
            var userId = _context.Users.FirstOrDefault(u => u.UserName == User.Identity!.Name)?.Id;
            if (userId == null) return Json(new { success = false, message = "Usuario no encontrado" });

            var sesionActiva = await _context.SesionesCaja.FirstOrDefaultAsync(s => s.UsuarioId == userId && s.Estado == "Abierta");
            if (sesionActiva != null) return Json(new { success = false, message = "Ya tienes una caja abierta" });

            var sesion = new SesionCaja
            {
                UsuarioId = userId,
                FechaApertura = DateTime.Now,
                MontoInicial = montoInicial,
                Estado = "Abierta"
            };

            _context.SesionesCaja.Add(sesion);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> CerrarCaja(decimal montoDeclarado, string? notas)
        {
            var userId = _context.Users.FirstOrDefault(u => u.UserName == User.Identity!.Name)?.Id;
            if (userId == null) return Json(new { success = false, message = "Usuario no encontrado" });

            var sesionActiva = await _context.SesionesCaja.FirstOrDefaultAsync(s => s.UsuarioId == userId && s.Estado == "Abierta");
            if (sesionActiva == null) return Json(new { success = false, message = "No hay caja abierta" });

            // Calcular ventas en esta sesión (para este ejemplo tomamos las facturas desde la apertura)
            var ventasSesion = await _context.Facturas
                .Where(f => f.FechaEmision >= sesionActiva.FechaApertura && f.Estado == "Pagada")
                .SumAsync(f => f.MontoEfectivo); // Solo efectivo para cuadre de caja, tarjeta va al banco

            sesionActiva.MontoFinalCalculado = sesionActiva.MontoInicial + ventasSesion;
            sesionActiva.MontoFinalDeclarado = montoDeclarado;
            sesionActiva.FechaCierre = DateTime.Now;
            sesionActiva.Estado = "Cerrada";
            sesionActiva.Notas = notas;

            await _context.SaveChangesAsync();

            return Json(new { success = true, diferencia = sesionActiva.Diferencia });
        }

        [HttpPost]
        public async Task<IActionResult> AgregarAlCarrito(int productoId, int cantidad = 1)
        {
            var almacen = await _context.Almacenes.FirstOrDefaultAsync(a => a.EsPrincipalAlmacen && a.Activo);
            var stockAlmacen = await _context.StocksAlmacen
                .Include(s => s.Producto)
                .FirstOrDefaultAsync(s => s.ProductoId == productoId && s.AlmacenId == almacen.Id);

            if (stockAlmacen == null)
            {
                return Json(new { success = false, message = "Producto no disponible en este almacén" });
            }

            if (stockAlmacen.Cantidad < cantidad)
            {
                return Json(new { success = false, message = $"Stock insuficiente. Disponible: {stockAlmacen.Cantidad}" });
            }

            var carrito = ObtenerCarrito();

            // Verificar si el producto ya está en el carrito
            var itemExistente = carrito.FirstOrDefault(i => i.ProductoId == productoId);
            if (itemExistente != null)
            {
                // Verificar stock con la nueva cantidad
                if (stockAlmacen.Cantidad < (itemExistente.Cantidad + cantidad))
                {
                    return Json(new { success = false, message = $"Stock insuficiente. Disponible: {stockAlmacen.Cantidad}" });
                }
                itemExistente.Cantidad += cantidad;
            }
            else
            {
                carrito.Add(new CarritoItem
                {
                    ProductoId = stockAlmacen.Producto.Id,
                    Codigo = stockAlmacen.Producto.Codigo,
                    Nombre = stockAlmacen.Producto.Nombre,
                    Precio = stockAlmacen.Producto.Precio,
                    Cantidad = cantidad
                });
            }

            GuardarCarrito(carrito);

            var totales = CalcularTotales(carrito);
            return Json(new { success = true, carritoCount = carrito.Count, totales });
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarCantidad(int productoId, int cantidad)
        {
            if (cantidad < 1)
            {
                return Json(new { success = false, message = "La cantidad debe ser mayor a cero" });
            }

            var almacen = await _context.Almacenes.FirstOrDefaultAsync(a => a.EsPrincipalAlmacen && a.Activo);
            var stockAlmacen = await _context.StocksAlmacen
                .FirstOrDefaultAsync(s => s.ProductoId == productoId && s.AlmacenId == almacen.Id);

            if (stockAlmacen == null)
            {
                return Json(new { success = false, message = "Producto no encontrado en almacén" });
            }

            if (stockAlmacen.Cantidad < cantidad)
            {
                return Json(new { success = false, message = $"Stock insuficiente en almacén. Disponible: {stockAlmacen.Cantidad}" });
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
            // Validar si la caja está abierta
            var userId = _context.Users.FirstOrDefault(u => u.UserName == User.Identity!.Name)?.Id;
            var sesionActiva = await _context.SesionesCaja.FirstOrDefaultAsync(s => s.UsuarioId == userId && s.Estado == "Abierta");
            if (sesionActiva == null)
            {
                TempData["ErrorMessage"] = "Debe abrir una caja antes de procesar ventas";
                return RedirectToAction(nameof(Index));
            }

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
                // Verificar stock disponible en el almacén
                var almacenVenta = await _context.Almacenes.FirstOrDefaultAsync(a => a.EsPrincipalAlmacen && a.Activo);
                if (almacenVenta == null)
                {
                    TempData["ErrorMessage"] = "No hay almacén configurado. Contacte al administrador.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var item in carrito)
                {
                    var stockAlmacen = await _context.StocksAlmacen
                        .FirstOrDefaultAsync(s => s.ProductoId == item.ProductoId && s.AlmacenId == almacenVenta.Id);
                    if (stockAlmacen == null || stockAlmacen.Cantidad < item.Cantidad)
                    {
                        var disponible = stockAlmacen?.Cantidad ?? 0;
                        TempData["ErrorMessage"] = $"Stock insuficiente para {item.Nombre}. Disponible: {disponible}";
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
                    Lineas = new List<FacturaLinea>(),
                    TipoPago = 1 // Contado para ventas POS
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
                        Factura = factura,
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

        // POST: POS/ProcesarPago
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarPago([Bind("ClienteId,TipoNcf,Notas,CondicionPago,MetodoPago,MontoEfectivo,MontoTarjeta,MontoTransferencia,MontoRecibido,NumeroTarjeta,AutorizacionTarjeta,Moneda,TasaCambio")] POSPagoViewModel pago)
        {
            // Validar si la caja está abierta
            var userId = _context.Users.FirstOrDefault(u => u.UserName == User.Identity!.Name)?.Id;
            var sesionActiva = await _context.SesionesCaja.FirstOrDefaultAsync(s => s.UsuarioId == userId && s.Estado == "Abierta");
            if (sesionActiva == null)
            {
                return Json(new { success = false, message = "Debe abrir una caja antes de procesar pagos" });
            }

            var carrito = ObtenerCarrito();
            if (!carrito.Any())
            {
                return Json(new { success = false, message = "El carrito está vacío" });
            }

            // Validar cliente
            var cliente = await _context.Clientes.FindAsync(pago.ClienteId);
            if (cliente == null)
            {
                return Json(new { success = false, message = "Debe seleccionar un cliente" });
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
                        return Json(new { success = false, message = $"Stock insuficiente para {item.Nombre}" });
                    }
                }

                // Calcular totales
                var subtotal = carrito.Sum(i => i.SubTotal);
                var montoItbis = carrito.Sum(i => i.ITBIS);
                var total = carrito.Sum(i => i.Total);

                // Si es efectivo y no viene MontoRecibido, usar el total como monto pagado
                if (pago.MetodoPago == "Efectivo" && pago.MontoRecibido <= 0)
                {
                    pago.MontoRecibido = total;
                }

                // Asignar MontoEfectivo desde MontoRecibido si es necesario
                if (pago.MetodoPago == "Efectivo" && pago.MontoEfectivo <= 0 && pago.MontoRecibido > 0)
                {
                    pago.MontoEfectivo = pago.MontoRecibido;
                }

                // Validar monto pagado
                decimal totalPagado = pago.MontoEfectivo + pago.MontoTarjeta + pago.MontoTransferencia;

                // Si solo es efectivo, usar MontoRecibido
                if (pago.MetodoPago == "Efectivo" && pago.MontoRecibido > 0)
                {
                    totalPagado = pago.MontoRecibido;
                }

                // Convertir totalPagado a DOP para la validación contra el total del carrito (que está en DOP)
                decimal totalPagadoDOP = totalPagado;
                if (pago.Moneda == "USD")
                {
                    totalPagadoDOP = totalPagado * pago.TasaCambio;
                }

                // Si es al contado, el pago debe ser igual o mayor al total
                if (pago.CondicionPago != "Crédito" && Math.Round(totalPagadoDOP, 2) < Math.Round(total, 2))
                {
                    string montoMostrar = pago.Moneda == "USD" ? $"US${totalPagado:N2}" : $"RD${totalPagado:N2}";
                    string totalMostrar = pago.Moneda == "USD" ? $"US${(total / pago.TasaCambio):N2}" : $"RD${total:N2}";
                    return Json(new { success = false, message = $"El monto pagado ({montoMostrar}) es menor que el total ({totalMostrar})" });
                }

                // Generar número de factura
                var ultimaFactura = await _context.Facturas
                    .OrderByDescending(f => f.Id)
                    .FirstOrDefaultAsync();
                var numeroFactura = $"FAC-{DateTime.Now:yyyy}-{((ultimaFactura?.Id ?? 0) + 1):D5}";

                // Crear la factura
                var factura = new Factura
                {
                    NumeroFactura = numeroFactura,
                    ClienteId = pago.ClienteId,
                    FechaEmision = DateTime.Now,
                    FechaVencimiento = DateTime.Now.AddDays(30),
                    Estado = pago.CondicionPago == "Crédito" ? (totalPagado > 0 ? "Pago Parcial" : "Pendiente") : "Pagada",
                    Notas = pago.Notas,
                    Moneda = pago.Moneda ?? "DOP",
                    TasaCambio = pago.TasaCambio > 0 ? pago.TasaCambio : 1.0m,
                    PorcentajeITBIS = 18,
                    TotalDOP = total, // total ya viene en DOP desde el carrito
                    Lineas = new List<FacturaLinea>(),
                    TipoPago = pago.CondicionPago == "Crédito" ? 2 : 1
                };

                // Ajustar montos según la moneda
                if (factura.Moneda == "USD")
                {
                    factura.Subtotal = subtotal / factura.TasaCambio;
                    factura.MontoITBIS = montoItbis / factura.TasaCambio;
                    factura.TotalITBIS = montoItbis / factura.TasaCambio;
                    factura.Total = total / factura.TasaCambio;
                }
                else
                {
                    factura.Subtotal = subtotal;
                    factura.MontoITBIS = montoItbis;
                    factura.TotalITBIS = montoItbis;
                    factura.Total = total;
                }

                // Asignar los montos pagados (ya vienen en la moneda de la factura desde el POS)
                factura.MontoEfectivo = pago.MontoEfectivo;
                factura.MontoTarjeta = pago.MontoTarjeta;
                factura.MontoTransferencia = pago.MontoTransferencia;

                // Si es solo efectivo y hay vuelto, ajustar el monto efectivo al total de la factura
                if (pago.MetodoPago == "Efectivo" && factura.MontoEfectivo > factura.Total)
                {
                    factura.MontoEfectivo = factura.Total;
                }

                // Asignar NCF si es RD
                if (!string.IsNullOrEmpty(pago.TipoNcf) && pago.TipoNcf != "NO_NCF")
                {
                    var rango = await _context.RangoNumeraciones
                        .FirstOrDefaultAsync(r => r.TipoECF == pago.TipoNcf && r.Estado == EstadoRango.Activo);

                    if (rango != null)
                    {
                        factura.TipoECF = pago.TipoNcf;
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
                        Factura = factura,
                        FechaMovimiento = DateTime.Now,
                        UsuarioRegistro = User.Identity?.Name ?? "POS"
                    });
                }

                _context.Facturas.Add(factura);
                await _context.SaveChangesAsync();
                
                // Si hubo un pago parcial/total, registrar el recibo
                if (totalPagado > 0)
                {
                    var recibo = new ReciboPago
                    {
                        FacturaId = factura.Id,
                        ClienteId = factura.ClienteId,
                        UsuarioId = userId ?? string.Empty,
                        FechaPago = DateTime.Now,
                        MontoEfectivo = pago.MontoEfectivo,
                        MontoTarjeta = pago.MontoTarjeta,
                        MontoTransferencia = pago.MontoTransferencia,
                        Referencia = pago.AutorizacionTarjeta,
                        Notas = $"Pago POS - {pago.MetodoPago}"
                    };
                    _context.RecibosPago.Add(recibo);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // Registro de Auditoría
                await _auditService.LogAsync(
                    User.Identity?.Name ?? "Sistema",
                    "POS",
                    "Venta",
                    $"Venta procesada exitosamente. Factura: {numeroFactura}, Total: {total:C}, Método: {pago.MetodoPago}",
                    factura.Id.ToString(),
                    "Info",
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                // Limpiar carrito
                HttpContext.Session.Remove("CarritoPOS");

                // Calcular vuelto
                var vuelto = totalPagado - total;

                return Json(new
                {
                    success = true,
                    facturaId = factura.Id,
                    numeroFactura = numeroFactura,
                    total = total.ToString("C", new System.Globalization.CultureInfo("es-DO")),
                    vuelto = vuelto.ToString("C", new System.Globalization.CultureInfo("es-DO")),
                    metodoPago = pago.MetodoPago
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = $"Error al procesar la venta: {ex.Message}" });
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

        [HttpPost]
        public async Task<IActionResult> BuscarProductos(string termino)
        {
            var almacen = await _context.Almacenes.FirstOrDefaultAsync(a => a.EsPrincipalAlmacen && a.Activo);
            if (almacen == null) return Json(new List<object>());

            var productos = await _context.StocksAlmacen
                .Include(s => s.Producto)
                .ThenInclude(p => p.Categoria)
                .Where(s => s.AlmacenId == almacen.Id && s.Producto.Activo &&
                           (s.Producto.Nombre.Contains(termino) ||
                            s.Producto.Codigo.Contains(termino) ||
                            (s.Producto.Categoria != null && s.Producto.Categoria.Nombre.Contains(termino))))
                .Take(10)
                .ToListAsync();

            return Json(productos.Select(s => new
            {
                s.Producto.Id,
                s.Producto.Codigo,
                s.Producto.Nombre,
                s.Producto.Precio,
                Stock = s.Cantidad,
                Categoria = s.Producto.Categoria?.Nombre
            }));
        }

        [HttpPost]
        public async Task<IActionResult> BuscarPorCodigoBarras(string codigoBarras)
        {
            if (string.IsNullOrEmpty(codigoBarras))
            {
                return Json(new { success = false, message = "Código de barras vacío" });
            }

            var almacen = await _context.Almacenes.FirstOrDefaultAsync(a => a.EsPrincipalAlmacen && a.Activo);
            if (almacen == null) return Json(new { success = false, message = "No hay almacén configurado" });

            var stockAlmacen = await _context.StocksAlmacen
                .Include(s => s.Producto)
                .FirstOrDefaultAsync(s => s.AlmacenId == almacen.Id && 
                                         (s.Producto.CodigoBarras == codigoBarras || s.Producto.Codigo == codigoBarras));

            if (stockAlmacen == null)
            {
                return Json(new { success = false, message = "Producto no encontrado en este almacén" });
            }

            if (!stockAlmacen.Producto.Activo)
            {
                return Json(new { success = false, message = "Producto inactivo" });
            }

            if (stockAlmacen.Cantidad <= 0)
            {
                return Json(new { success = false, message = $"Stock insuficiente en almacén. Disponible: {stockAlmacen.Cantidad}" });
            }

            return Json(new
            {
                success = true,
                producto = new
                {
                    stockAlmacen.Producto.Id,
                    stockAlmacen.Producto.Codigo,
                    stockAlmacen.Producto.Nombre,
                    stockAlmacen.Producto.Precio,
                    Stock = stockAlmacen.Cantidad
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

    public class POSPagoViewModel
    {
        public int ClienteId { get; set; }
        public string? TipoNcf { get; set; }
        public string? Notas { get; set; }
        public string CondicionPago { get; set; } = "Contado"; // Contado, Crédito
        public string MetodoPago { get; set; } = "Efectivo"; // Efectivo, Tarjeta, Transferencia, Mixto
        public decimal MontoEfectivo { get; set; }
        public decimal MontoTarjeta { get; set; }
        public decimal MontoTransferencia { get; set; }
        public decimal MontoRecibido { get; set; } // Para cálculo de vuelto
        public string? NumeroTarjeta { get; set; } // Últimos 4 dígitos
        public string? AutorizacionTarjeta { get; set; }
        public string Moneda { get; set; } = "DOP";
        public decimal TasaCambio { get; set; } = 1.0m;
    }
}
