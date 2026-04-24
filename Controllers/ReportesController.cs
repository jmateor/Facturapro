using Facturapro.Data;
using Facturapro.Models.Entities;
using Facturapro.Models.ViewModels.Reportes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Facturapro.Controllers
{
    [Authorize]
    public class ReportesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportesController> _logger;

        public ReportesController(ApplicationDbContext context, ILogger<ReportesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Reportes/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var inicioAnio = new DateTime(hoy.Year, 1, 1);
            var hace7Dias = hoy.AddDays(-6);
            var mesAnterior = inicioMes.AddMonths(-1);

            var viewModel = new DashboardViewModel();

            // Ventas de hoy
            var facturasHoy = await _context.Facturas
                .Where(f => f.FechaEmision.Date == hoy && (f.EstadoDGII == "Aprobado" || f.EstadoDGII == "Firmado"))
                .ToListAsync();
            viewModel.VentasHoy = facturasHoy.Sum(f => f.Total);
            viewModel.FacturasHoy = facturasHoy.Count;

            // Ventas del mes
            var facturasMes = await _context.Facturas
                .Where(f => f.FechaEmision >= inicioMes && (f.EstadoDGII == "Aprobado" || f.EstadoDGII == "Firmado"))
                .ToListAsync();
            viewModel.VentasMes = facturasMes.Sum(f => f.Total);
            viewModel.FacturasMes = facturasMes.Count;

            // Ventas del año
            var facturasAnio = await _context.Facturas
                .Where(f => f.FechaEmision >= inicioAnio && (f.EstadoDGII == "Aprobado" || f.EstadoDGII == "Firmado"))
                .ToListAsync();
            viewModel.VentasAnio = facturasAnio.Sum(f => f.Total);

            // Totales generales
            viewModel.TotalClientes = await _context.Clientes.CountAsync(c => c.Activo);
            viewModel.TotalProductos = await _context.Productos.CountAsync(p => p.Activo);

            // Variación vs mes anterior
            var facturasMesAnterior = await _context.Facturas
                .Where(f => f.FechaEmision >= mesAnterior && f.FechaEmision < inicioMes
                    && (f.EstadoDGII == "Aprobado" || f.EstadoDGII == "Firmado"))
                .SumAsync(f => (decimal?)f.Total) ?? 0;

            if (facturasMesAnterior > 0)
            {
                viewModel.VariacionVentasMes = ((viewModel.VentasMes - facturasMesAnterior) / facturasMesAnterior) * 100;
            }
            else if (viewModel.VentasMes > 0)
            {
                viewModel.VariacionVentasMes = 100;
            }

            // Ventas últimos 7 días
            viewModel.VentasUltimos7Dias = await GetVentasUltimos7Dias(hace7Dias, hoy);

            // Ventas por mes (últimos 6 meses)
            viewModel.VentasPorMes = await GetVentasPorMes(inicioAnio, inicioMes.AddMonths(1).AddDays(-1));

            // Top 5 productos vendidos
            viewModel.TopProductos = await GetTopProductos(inicioMes, hoy.AddDays(1));

            // Top 5 clientes
            viewModel.TopClientes = await GetTopClientes(inicioMes, hoy.AddDays(1));

            // Facturas por estado
            viewModel.FacturasPorEstado = await GetFacturasPorEstado();

            // Alertas
            viewModel.Alertas = await GetAlertas();

            return View(viewModel);
        }

        // GET: Reportes/Ventas
        public async Task<IActionResult> Ventas(
            DateTime? desde = null,
            DateTime? hasta = null,
            string? tipoECF = null,
            int? clienteId = null,
            string? estadoDGII = null)
        {
            // Establecer fechas por defecto (mes actual)
            var fechaDesde = desde ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var fechaHasta = hasta ?? DateTime.Today.AddDays(1).AddSeconds(-1);

            var query = _context.Facturas
                .Include(f => f.Cliente)
                .Where(f => f.FechaEmision >= fechaDesde && f.FechaEmision <= fechaHasta)
                .AsQueryable();

            if (!string.IsNullOrEmpty(tipoECF))
                query = query.Where(f => f.TipoECF == tipoECF);

            if (clienteId.HasValue)
                query = query.Where(f => f.ClienteId == clienteId.Value);

            if (!string.IsNullOrEmpty(estadoDGII))
                query = query.Where(f => f.EstadoDGII == estadoDGII);

            var facturas = await query.OrderByDescending(f => f.FechaEmision).ToListAsync();

            var viewModel = new ReporteVentasViewModel
            {
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                TipoECF = tipoECF,
                ClienteId = clienteId,
                EstadoDGII = estadoDGII,
                TotalVentas = facturas.Sum(f => f.Total),
                TotalITBIS = facturas.Sum(f => f.MontoITBIS),
                TotalFacturas = facturas.Count,
                PromedioFactura = facturas.Count > 0 ? facturas.Average(f => f.Total) : 0,
                Facturas = facturas.Select(f => new VentaDetalleViewModel
                {
                    FacturaId = f.Id,
                    NumeroFactura = f.NumeroFactura,
                    ENCF = f.eNCF,
                    TipoECF = f.TipoECF ?? "31",
                    TipoECFTexto = GetTipoECFTexto(f.TipoECF),
                    FechaEmision = f.FechaEmision,
                    ClienteNombre = f.Cliente?.Nombre ?? "N/A",
                    ClienteRNC = f.Cliente?.RNC,
                    Subtotal = f.Subtotal,
                    ITBIS = f.MontoITBIS,
                    Total = f.Total,
                    EstadoDGII = f.EstadoDGII ?? "Pendiente"
                }).ToList(),
                VentasPorDia = GetVentasPorDia(facturas),
                VentasPorTipo = GetVentasPorTipo(facturas),
                Clientes = await _context.Clientes
                    .Where(c => c.Activo)
                    .Select(c => new ClienteFilterViewModel
                    {
                        Id = c.Id,
                        Nombre = c.Nombre,
                        RNC = c.RNC
                    })
                    .OrderBy(c => c.Nombre)
                    .ToListAsync()
            };

            // Preparar filtros para la vista
            ViewBag.TiposECF = GetTiposECFSelectList(tipoECF);
            ViewBag.EstadosDGII = GetEstadosDGIISelectList(estadoDGII);
            ViewBag.Clientes = new SelectList(viewModel.Clientes, "Id", "Nombre", clienteId);

            return View(viewModel);
        }

        // GET: Reportes/Inventario
        public async Task<IActionResult> Inventario(int? categoriaId = null, string? estadoStock = null)
        {
            var query = _context.Productos
                .Include(p => p.Categoria)
                .AsQueryable();

            if (categoriaId.HasValue)
                query = query.Where(p => p.CategoriaId == categoriaId);

            var productos = await query.ToListAsync();

            // Filtrar por estado de stock (usando 10 como stock mínimo por defecto)
            const int STOCK_MINIMO = 10;
            if (!string.IsNullOrEmpty(estadoStock))
            {
                productos = estadoStock.ToLower() switch
                {
                    "bajo" => productos.Where(p => p.Stock <= STOCK_MINIMO && p.Stock > 0).ToList(),
                    "sin_stock" => productos.Where(p => p.Stock <= 0).ToList(),
                    _ => productos
                };
            }

            var viewModel = new ReporteInventarioViewModel
            {
                CategoriaId = categoriaId,
                EstadoStock = estadoStock,
                TotalProductos = productos.Count,
                ValorInventarioTotal = productos.Sum(p => p.Stock * p.Precio * 0.6m), // Estimación del costo (60% del precio)
                ProductosBajoStock = productos.Count(p => p.Stock <= STOCK_MINIMO && p.Stock > 0),
                ProductosSinStock = productos.Count(p => p.Stock <= 0),
                Productos = productos.Select(p => new ProductoInventarioViewModel
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    CodigoBarra = p.CodigoBarras,
                    Categoria = p.Categoria?.Nombre ?? "Sin categoría",
                    StockActual = p.Stock,
                    StockMinimo = STOCK_MINIMO,
                    Costo = p.Precio * 0.6m, // Estimación del costo
                    Precio = p.Precio
                }).ToList()
            };

            ViewBag.Categorias = new SelectList(
                await _context.Categorias.Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync(),
                "Id", "Nombre", categoriaId);

            return View(viewModel);
        }

        // GET: Reportes/General
        public async Task<IActionResult> General(
            DateTime? desde,
            DateTime? hasta,
            int? clienteId,
            int? categoriaId,
            int? productoId,
            string? ciudad,
            int? anio,
            bool incluirAnuladas = false,
            bool incluirDevoluciones = false,
            string? tipoReporte = null)
        {
            // Establecer fechas por defecto (mes actual)
            var fechaDesde = desde ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var fechaHasta = hasta ?? DateTime.Today;

            // Si se seleccionó año, usar ese año completo
            if (anio.HasValue)
            {
                fechaDesde = new DateTime(anio.Value, 1, 1);
                fechaHasta = new DateTime(anio.Value, 12, 31);
            }

            var query = _context.Facturas
                .Include(f => f.Cliente)
                .Include(f => f.Lineas)
                .AsQueryable();

            // Filtros
            query = query.Where(f => f.FechaEmision >= fechaDesde && f.FechaEmision <= fechaHasta.AddDays(1));

            if (!string.IsNullOrEmpty(tipoReporte) && tipoReporte == "anuladas")
            {
                query = query.Where(f => f.EstadoDGII == "Rechazado" || f.EstadoDGII == "Cancelada");
            }
            else if (!string.IsNullOrEmpty(tipoReporte) && tipoReporte == "devoluciones")
            {
                query = query.Where(f => f.TipoECF == "33" || f.TipoECF == "34");
            }
            else if (!incluirAnuladas)
            {
                query = query.Where(f => f.EstadoDGII != "Rechazado" && f.EstadoDGII != "Cancelada");
            }

            if (clienteId.HasValue)
                query = query.Where(f => f.ClienteId == clienteId.Value);

            // Filtro por categoría - requiere join con productos
            if (categoriaId.HasValue)
            {
                var productosCategoria = await _context.Productos
                    .Where(p => p.CategoriaId == categoriaId.Value)
                    .Select(p => p.Nombre)
                    .ToListAsync();

                query = query.Where(f => f.Lineas!.Any(l => productosCategoria.Contains(l.NombreItem ?? "")));
            }

            // Filtro por producto - busca por nombre en las líneas
            if (productoId.HasValue)
            {
                var producto = await _context.Productos.FindAsync(productoId.Value);
                if (producto != null)
                {
                    query = query.Where(f => f.Lineas!.Any(l => l.NombreItem == producto.Nombre));
                }
            }

            if (!string.IsNullOrEmpty(ciudad))
            {
                query = query.Where(f => f.Cliente != null && f.Cliente.Ciudad == ciudad);
            }

            var facturas = await query.ToListAsync();

            var viewModel = new ReporteGeneralViewModel
            {
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                ClienteId = clienteId,
                CategoriaId = categoriaId,
                ProductoId = productoId,
                Ciudad = ciudad,
                Anio = anio,
                TipoReporte = tipoReporte,
                IncluirAnuladas = incluirAnuladas,
                IncluirDevoluciones = incluirDevoluciones,
                Facturas = facturas.Select(f => new FacturaReporteViewModel
                {
                    FacturaId = f.Id,
                    NumeroFactura = f.NumeroFactura,
                    ENCF = f.eNCF,
                    TipoECF = f.TipoECF ?? "31",
                    TipoECFTexto = GetTipoECFTexto(f.TipoECF),
                    FechaEmision = f.FechaEmision,
                    ClienteNombre = f.Cliente?.Nombre ?? "Consumidor Final",
                    ClienteRNC = f.Cliente?.RNC,
                    ClienteCiudad = f.Cliente?.Ciudad,
                    Subtotal = f.Subtotal,
                    ITBIS = f.MontoITBIS,
                    Descuento = 0,
                    Total = f.Total,
                    EstadoDGII = f.EstadoDGII ?? "Pendiente"
                }).ToList(),
                TotalVentas = facturas.Sum(f => f.Total),
                TotalITBIS = facturas.Sum(f => f.MontoITBIS),
                TotalFacturas = facturas.Count
            };

            // Preparar filtros para la vista
            ViewBag.Clientes = new SelectList(
                await _context.Clientes.Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync(),
                "Id", "Nombre", clienteId);

            ViewBag.Categorias = new SelectList(
                await _context.Categorias.Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync(),
                "Id", "Nombre", categoriaId);

            ViewBag.Productos = new SelectList(
                await _context.Productos.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync(),
                "Id", "Nombre", productoId);

            ViewBag.Ciudades = new SelectList(
                await _context.Clientes.Where(c => c.Activo && !string.IsNullOrEmpty(c.Ciudad))
                    .Select(c => c.Ciudad!).Distinct().OrderBy(c => c).ToListAsync(),
                null, null, ciudad);

            ViewBag.Anios = await _context.Facturas
                .Select(f => f.FechaEmision.Year)
                .Distinct()
                .OrderByDescending(a => a)
                .ToListAsync();

            return View(viewModel);
        }

        // GET: Reportes/ProductosMasVendidos
        public async Task<IActionResult> ProductosMasVendidos(
            DateTime? desde,
            DateTime? hasta,
            int? categoriaId)
        {
            var fechaDesde = desde ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var fechaHasta = hasta ?? DateTime.Today;

            var lineas = await _context.FacturaLineas
                .Include(l => l.Factura)
                .ThenInclude(f => f!.Cliente)
                .Where(l => l.Factura != null
                    && l.Factura.FechaEmision >= fechaDesde
                    && l.Factura.FechaEmision <= fechaHasta
                    && (l.Factura.EstadoDGII == "Aprobado" || l.Factura.EstadoDGII == "Firmado"))
                .ToListAsync();

            if (categoriaId.HasValue)
            {
                var productosCategoria = await _context.Productos
                    .Where(p => p.CategoriaId == categoriaId.Value)
                    .Select(p => p.Nombre)
                    .ToListAsync();
                lineas = lineas.Where(l => productosCategoria.Contains(l.NombreItem ?? "")).ToList();
            }

            var productosMasVendidos = lineas
                .GroupBy(l => l.NombreItem)
                .Select(g => new ProductosMasVendidosViewModel
                {
                    ProductoId = 0,
                    Nombre = g.Key ?? "Sin nombre",
                    CantidadVendida = g.Sum(l => l.Cantidad),
                    TotalVentas = g.Sum(l => l.MontoItem),
                    NumeroFacturas = g.Select(l => l.FacturaId).Distinct().Count()
                })
                .OrderByDescending(p => p.CantidadVendida)
                .Take(50)
                .ToList();

            ViewBag.Categorias = new SelectList(
                await _context.Categorias.Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync(),
                "Id", "Nombre", categoriaId);

            return View(productosMasVendidos);
        }

        // GET: Reportes/VentasPorCategoria
        public async Task<IActionResult> VentasPorCategoria(
            DateTime? desde,
            DateTime? hasta)
        {
            var fechaDesde = desde ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var fechaHasta = hasta ?? DateTime.Today;

            var facturas = await _context.Facturas
                .Include(f => f.Lineas)
                .Where(f => f.FechaEmision >= fechaDesde
                    && f.FechaEmision <= fechaHasta
                    && (f.EstadoDGII == "Aprobado" || f.EstadoDGII == "Firmado"))
                .ToListAsync();

            var categorias = await _context.Categorias
                .Where(c => c.Activo)
                .ToListAsync();

            var ventasPorCategoria = new List<VentasPorCategoriaViewModel>();
            var totalGeneral = facturas.Sum(f => f.Total);

            foreach (var categoria in categorias)
            {
                var productosCategoria = await _context.Productos
                    .Where(p => p.CategoriaId == categoria.Id)
                    .Select(p => p.Nombre)
                    .ToListAsync();

                var lineasCategoria = facturas
                    .SelectMany(f => f.Lineas ?? Enumerable.Empty<FacturaLinea>())
                    .Where(l => productosCategoria.Contains(l.NombreItem ?? ""))
                    .ToList();

                var totalCategoria = lineasCategoria.Sum(l => l.MontoItem);

                ventasPorCategoria.Add(new VentasPorCategoriaViewModel
                {
                    CategoriaId = categoria.Id,
                    CategoriaNombre = categoria.Nombre,
                    TotalVentas = totalCategoria,
                    CantidadProductos = productosCategoria.Count,
                    Porcentaje = totalGeneral > 0 ? (totalCategoria / totalGeneral) * 100 : 0
                });
            }

            return View(ventasPorCategoria.OrderByDescending(c => c.TotalVentas).ToList());
        }

        // GET: Reportes/VentasPorCiudad
        public async Task<IActionResult> VentasPorCiudad(
            DateTime? desde,
            DateTime? hasta)
        {
            var fechaDesde = desde ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var fechaHasta = hasta ?? DateTime.Today;

            var facturas = await _context.Facturas
                .Include(f => f.Cliente)
                .Where(f => f.FechaEmision >= fechaDesde
                    && f.FechaEmision <= fechaHasta
                    && (f.EstadoDGII == "Aprobado" || f.EstadoDGII == "Firmado")
                    && f.Cliente != null
                    && !string.IsNullOrEmpty(f.Cliente.Ciudad))
                .ToListAsync();

            var ciudades = facturas
                .Where(f => f.Cliente != null && !string.IsNullOrEmpty(f.Cliente.Ciudad))
                .Select(f => f.Cliente!.Ciudad!)
                .Distinct()
                .ToList();

            var totalGeneral = facturas.Sum(f => f.Total);
            var ventasPorCiudad = new List<VentasPorCiudadViewModel>();

            foreach (var ciudad in ciudades)
            {
                var facturasCiudad = facturas.Where(f => f.Cliente!.Ciudad == ciudad).ToList();

                ventasPorCiudad.Add(new VentasPorCiudadViewModel
                {
                    Ciudad = ciudad,
                    CantidadClientes = facturasCiudad.Select(f => f.ClienteId).Distinct().Count(),
                    CantidadFacturas = facturasCiudad.Count,
                    TotalVentas = facturasCiudad.Sum(f => f.Total),
                    Porcentaje = totalGeneral > 0 ? (facturasCiudad.Sum(f => f.Total) / totalGeneral) * 100 : 0
                });
            }

            return View(ventasPorCiudad.OrderByDescending(c => c.TotalVentas).ToList());
        }

        // GET: Reportes/VentasPorAnio
        public async Task<IActionResult> VentasPorAnio()
        {
            var facturas = await _context.Facturas
                .Where(f => (f.EstadoDGII == "Aprobado" || f.EstadoDGII == "Firmado"))
                .ToListAsync();

            var ventasPorAnio = facturas
                .GroupBy(f => f.FechaEmision.Year)
                .Select(g => new VentasPorAnioViewModel
                {
                    Anio = g.Key,
                    TotalVentas = g.Sum(f => f.Total),
                    CantidadFacturas = g.Count(),
                    PromedioMensual = g.Sum(f => f.Total) / 12
                })
                .OrderByDescending(v => v.Anio)
                .ToList();

            return View(ventasPorAnio);
        }

        // GET: Reportes/Devoluciones
        public async Task<IActionResult> Devoluciones(
            DateTime? desde,
            DateTime? hasta,
            int? clienteId)
        {
            var fechaDesde = desde ?? new DateTime(DateTime.Today.Year, 1, 1);
            var fechaHasta = hasta ?? DateTime.Today;

            var devoluciones = await _context.Facturas
                .Include(f => f.Cliente)
                .Where(f => f.FechaEmision >= fechaDesde
                    && f.FechaEmision <= fechaHasta
                    && (f.TipoECF == "33" || f.TipoECF == "34"))
                .OrderByDescending(f => f.FechaEmision)
                .ToListAsync();

            if (clienteId.HasValue)
            {
                devoluciones = devoluciones.Where(f => f.ClienteId == clienteId.Value).ToList();
            }

            var viewModel = devoluciones.Select(f => new DevolucionReporteViewModel
            {
                FacturaId = f.Id,
                NumeroFactura = f.NumeroFactura,
                ENCF = f.eNCF,
                FechaEmision = f.FechaEmision,
                ClienteNombre = f.Cliente?.Nombre ?? "Consumidor Final",
                ClienteRNC = f.Cliente?.RNC,
                TotalDevolucion = f.Total
            }).ToList();

            ViewBag.Clientes = new SelectList(
                await _context.Clientes.Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync(),
                "Id", "Nombre", clienteId);

            return View(viewModel);
        }

        // GET: Reportes/ExportarPDF
        public async Task<IActionResult> ExportarPDF(
            DateTime? desde,
            DateTime? hasta,
            int? clienteId,
            string? tipoReporte)
        {
            var fechaDesde = desde ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var fechaHasta = hasta ?? DateTime.Today;

            var query = _context.Facturas
                .Include(f => f.Cliente)
                .Include(f => f.Lineas)
                .Where(f => f.FechaEmision >= fechaDesde && f.FechaEmision <= fechaHasta.AddDays(1));

            if (!string.IsNullOrEmpty(tipoReporte) && tipoReporte == "anuladas")
            {
                query = query.Where(f => f.EstadoDGII == "Rechazado" || f.EstadoDGII == "Cancelada");
            }
            else if (!string.IsNullOrEmpty(tipoReporte) && tipoReporte == "devoluciones")
            {
                query = query.Where(f => f.TipoECF == "33" || f.TipoECF == "34");
            }
            else
            {
                query = query.Where(f => f.EstadoDGII != "Rechazado" && f.EstadoDGII != "Cancelada");
            }

            if (clienteId.HasValue)
                query = query.Where(f => f.ClienteId == clienteId.Value);

            var facturas = await query.OrderByDescending(f => f.FechaEmision).ToListAsync();

            var empresa = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();

            var pdfService = new Facturapro.Services.PDF.PdfService();
            var pdfBytes = pdfService.GenerarReportePDF(facturas, empresa, fechaDesde, fechaHasta, tipoReporte);

            var fileName = $"Reporte_{tipoReporte ?? "Ventas"}_{fechaDesde:yyyyMMdd}_{fechaHasta:yyyyMMdd}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        #region Métodos Auxiliares

        private async Task<List<VentaPorDiaViewModel>> GetVentasUltimos7Dias(DateTime desde, DateTime hasta)
        {
            var resultado = new List<VentaPorDiaViewModel>();

            for (var fecha = desde; fecha <= hasta; fecha = fecha.AddDays(1))
            {
                var facturasDia = await _context.Facturas
                    .Where(f => f.FechaEmision.Date == fecha.Date
                        && (f.EstadoDGII == "Aprobado" || f.EstadoDGII == "Firmado"))
                    .ToListAsync();

                resultado.Add(new VentaPorDiaViewModel
                {
                    Fecha = fecha,
                    Total = facturasDia.Sum(f => f.Total),
                    CantidadFacturas = facturasDia.Count
                });
            }

            return resultado;
        }

        private async Task<List<VentaPorMesViewModel>> GetVentasPorMes(DateTime desde, DateTime hasta)
        {
            var facturas = await _context.Facturas
                .Where(f => f.FechaEmision >= desde && f.FechaEmision <= hasta
                    && (f.EstadoDGII == "Aprobado" || f.EstadoDGII == "Firmado"))
                .ToListAsync();

            return facturas
                .GroupBy(f => new { f.FechaEmision.Year, f.FechaEmision.Month })
                .Select(g => new VentaPorMesViewModel
                {
                    Anio = g.Key.Year,
                    Mes = g.Key.Month,
                    Total = g.Sum(f => f.Total),
                    CantidadFacturas = g.Count()
                })
                .OrderBy(v => v.Anio).ThenBy(v => v.Mes)
                .ToList();
        }

        private async Task<List<ProductoTopViewModel>> GetTopProductos(DateTime desde, DateTime hasta)
        {
            // Obtener primero los datos filtrados y luego agrupar en memoria
            var lineas = await _context.FacturaLineas
                .Include(fl => fl.Factura)
                .Where(fl => fl.Factura != null
                    && fl.Factura.FechaEmision >= desde
                    && fl.Factura.FechaEmision < hasta
                    && (fl.Factura.EstadoDGII == "Aprobado" || fl.Factura.EstadoDGII == "Firmado"))
                .ToListAsync();

            return lineas
                .GroupBy(fl => fl.NombreItem)
                .Select(g => new ProductoTopViewModel
                {
                    ProductoId = 0,
                    Nombre = g.Key ?? "Producto sin nombre",
                    CantidadVendida = g.Sum(fl => fl.Cantidad),
                    TotalVenta = g.Sum(fl => fl.MontoItem)
                })
                .OrderByDescending(p => p.TotalVenta)
                .Take(5)
                .ToList();
        }

        private async Task<List<ClienteTopViewModel>> GetTopClientes(DateTime desde, DateTime hasta)
        {
            return await _context.Facturas
                .Where(f => f.FechaEmision >= desde
                    && f.FechaEmision < hasta
                    && (f.EstadoDGII == "Aprobado" || f.EstadoDGII == "Firmado")
                    && f.Cliente != null)
                .GroupBy(f => new { f.ClienteId, f.Cliente!.Nombre, f.Cliente.RNC })
                .Select(g => new ClienteTopViewModel
                {
                    ClienteId = g.Key.ClienteId,
                    Nombre = g.Key.Nombre,
                    RNC = g.Key.RNC,
                    CantidadFacturas = g.Count(),
                    TotalCompras = g.Sum(f => f.Total)
                })
                .OrderByDescending(c => c.TotalCompras)
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<FacturaPorEstadoViewModel>> GetFacturasPorEstado()
        {
            return await _context.Facturas
                .GroupBy(f => f.EstadoDGII ?? "Pendiente")
                .Select(g => new FacturaPorEstadoViewModel
                {
                    Estado = g.Key,
                    Cantidad = g.Count(),
                    Total = g.Sum(f => f.Total)
                })
                .OrderByDescending(x => x.Total)
                .ToListAsync();
        }

        private async Task<List<AlertaViewModel>> GetAlertas()
        {
            var alertas = new List<AlertaViewModel>();

            // Productos bajo stock (usando 10 como mínimo por defecto)
            const int STOCK_MINIMO_ALERTA = 10;
            var productosBajoStock = await _context.Productos
                .Where(p => p.Stock <= STOCK_MINIMO_ALERTA && p.Stock > 0 && p.Activo)
                .CountAsync();

            if (productosBajoStock > 0)
            {
                alertas.Add(new AlertaViewModel
                {
                    Tipo = "warning",
                    Titulo = "Stock Bajo",
                    Mensaje = $"{productosBajoStock} productos están por debajo del stock mínimo.",
                    AccionUrl = Url.Action("Inventario", new { estadoStock = "bajo" }),
                    AccionTexto = "Ver Productos"
                });
            }

            // Productos sin stock
            var productosSinStock = await _context.Productos
                .Where(p => p.Stock <= 0 && p.Activo)
                .CountAsync();

            if (productosSinStock > 0)
            {
                alertas.Add(new AlertaViewModel
                {
                    Tipo = "danger",
                    Titulo = "Sin Stock",
                    Mensaje = $"{productosSinStock} productos están agotados.",
                    AccionUrl = Url.Action("Inventario", new { estadoStock = "sin_stock" }),
                    AccionTexto = "Ver Productos"
                });
            }

            // Facturas rechazadas por DGII
            var facturasRechazadas = await _context.Facturas
                .Where(f => f.EstadoDGII == "Rechazado")
                .CountAsync();

            if (facturasRechazadas > 0)
            {
                alertas.Add(new AlertaViewModel
                {
                    Tipo = "danger",
                    Titulo = "Facturas Rechazadas",
                    Mensaje = $"{facturasRechazadas} facturas fueron rechazadas por la DGII.",
                    AccionUrl = Url.Action("Index", "Facturas", new { estadoDGII = "Rechazado" }),
                    AccionTexto = "Ver Facturas"
                });
            }

            return alertas;
        }

        private List<VentaPorDiaViewModel> GetVentasPorDia(List<Factura> facturas)
        {
            return facturas
                .GroupBy(f => f.FechaEmision.Date)
                .Select(g => new VentaPorDiaViewModel
                {
                    Fecha = g.Key,
                    Total = g.Sum(f => f.Total),
                    CantidadFacturas = g.Count()
                })
                .OrderBy(v => v.Fecha)
                .ToList();
        }

        private List<VentaPorTipoViewModel> GetVentasPorTipo(List<Factura> facturas)
        {
            var total = facturas.Sum(f => f.Total);
            if (total == 0) total = 1; // Evitar división por cero

            return facturas
                .GroupBy(f => f.TipoECF ?? "31")
                .Select(g => new VentaPorTipoViewModel
                {
                    TipoECF = g.Key,
                    TipoECFTexto = GetTipoECFTexto(g.Key),
                    Cantidad = g.Count(),
                    Total = g.Sum(f => f.Total),
                    Porcentaje = (g.Sum(f => f.Total) / total) * 100
                })
                .OrderByDescending(v => v.Total)
                .ToList();
        }

        private string GetTipoECFTexto(string? tipoECF)
        {
            return tipoECF switch
            {
                "31" => "Crédito Fiscal",
                "32" => "Consumo",
                "33" => "Nota Débito",
                "34" => "Nota Crédito",
                "41" => "Compras",
                "43" => "Gastos Menores",
                "44" => "Régimen Especial",
                "45" => "Gubernamental",
                "46" => "Exportaciones",
                "47" => "Pagos al Exterior",
                _ => "Factura"
            };
        }

        private SelectList GetTiposECFSelectList(string? selected)
        {
            var items = new[]
            {
                new { Value = "", Text = "Todos los tipos" },
                new { Value = "31", Text = "E31 - Crédito Fiscal" },
                new { Value = "32", Text = "E32 - Consumo" },
                new { Value = "33", Text = "E33 - Nota Débito" },
                new { Value = "34", Text = "E34 - Nota Crédito" }
            };
            return new SelectList(items, "Value", "Text", selected);
        }

        private SelectList GetEstadosDGIISelectList(string? selected)
        {
            var items = new[]
            {
                new { Value = "", Text = "Todos los estados" },
                new { Value = "Pendiente", Text = "Pendiente" },
                new { Value = "Firmado", Text = "Firmado" },
                new { Value = "Enviado", Text = "Enviado" },
                new { Value = "Aprobado", Text = "Aprobado" },
                new { Value = "Rechazado", Text = "Rechazado" }
            };
            return new SelectList(items, "Value", "Text", selected);
        }

        #endregion
    }
}
