using Facturapro.Data;
using Facturapro.Models.Entities;
using Facturapro.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using ClosedXML.Excel;

namespace Facturapro.Controllers
{
    [Authorize]
    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBarcodeService _barcodeService;

        public ProductosController(ApplicationDbContext context, IBarcodeService barcodeService)
        {
            _context = context;
            _barcodeService = barcodeService;
        }

        // GET: Productos
        public async Task<IActionResult> Index(int? categoriaId, string buscar)
        {
            var query = _context.Productos
                .Include(p => p.Categoria)
                .AsQueryable();

            if (categoriaId.HasValue)
            {
                query = query.Where(p => p.CategoriaId == categoriaId);
            }

            if (!string.IsNullOrEmpty(buscar))
            {
                query = query.Where(p => p.Nombre.Contains(buscar) ||
                                       p.Codigo.Contains(buscar) ||
                                       (p.Descripcion != null && p.Descripcion.Contains(buscar)));
            }

            ViewData["Categorias"] = await _context.Categorias
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            ViewData["CategoriaSeleccionada"] = categoriaId;
            ViewData["Busqueda"] = buscar;

            return View(await query.OrderBy(p => p.Nombre).ToListAsync());
        }

        // GET: Productos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }

        // GET: Productos/Create
        public async Task<IActionResult> Create()
        {
            await CargarCategoriasAsync();
            await CargarProveedoresAsync();
            return View();
        }

        // POST: Productos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Codigo,Nombre,Descripcion,Precio,PrecioCompra,Stock,StockMinimo,CategoriaId,ProveedorId,TipoProducto,ControlaStock,Ubicacion,NumeroLote,UnidadMedida,PesoPorUnidad,FechaVencimiento,CodigoBarras,Activo")] Producto producto)
        {
            // Validar que el código no exista
            if (await _context.Productos.AnyAsync(p => p.Codigo == producto.Codigo))
            {
                ModelState.AddModelError("Codigo", "Ya existe un producto con este código");
            }

            if (ModelState.IsValid)
            {
                producto.FechaCreacion = DateTime.Now;

                // Si es servicio, no controlar stock
                if (producto.TipoProducto == 2)
                {
                    producto.ControlaStock = false;
                    producto.Stock = 0;
                }

                _context.Add(producto);
                await _context.SaveChangesAsync();

                // Generar código de barras si no se proporcionó
                if (string.IsNullOrEmpty(producto.CodigoBarras))
                {
                    producto.CodigoBarras = _barcodeService.GenerarCodigoBarras(producto.Codigo, producto.Id);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Producto creado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            await CargarCategoriasAsync(producto.CategoriaId);
            await CargarProveedoresAsync(producto.ProveedorId);
            return View(producto);
        }

        // GET: Productos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            await CargarCategoriasAsync(producto.CategoriaId);
            await CargarProveedoresAsync(producto.ProveedorId);
            return View(producto);
        }

        // POST: Productos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Codigo,Nombre,Descripcion,Precio,PrecioCompra,Stock,StockMinimo,CategoriaId,ProveedorId,TipoProducto,ControlaStock,Ubicacion,NumeroLote,UnidadMedida,PesoPorUnidad,FechaVencimiento,CodigoBarras,Activo")] Producto producto)
        {
            if (id != producto.Id)
            {
                return NotFound();
            }

            // Validar que el código no exista en otro producto
            if (await _context.Productos.AnyAsync(p => p.Codigo == producto.Codigo && p.Id != producto.Id))
            {
                ModelState.AddModelError("Codigo", "Ya existe un producto con este código");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Si es servicio, no controlar stock
                    if (producto.TipoProducto == 2)
                    {
                        producto.ControlaStock = false;
                        producto.Stock = 0;
                    }

                    _context.Update(producto);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Producto actualizado correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductoExists(producto.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            await CargarCategoriasAsync(producto.CategoriaId);
            await CargarProveedoresAsync(producto.ProveedorId);
            return View(producto);
        }

        // GET: Productos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (producto == null)
            {
                return NotFound();
            }

            // Verificar si el producto está siendo usado en facturas
            var estaEnFacturas = await _context.FacturaLineas
                .AnyAsync(fl => fl.NombreItem == producto.Nombre || fl.Descripcion.Contains(producto.Nombre));

            ViewData["EstaEnFacturas"] = estaEnFacturas;

            return View(producto);
        }

        // POST: Productos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
            {
                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Producto eliminado correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Productos/Importar
        public async Task<IActionResult> Importar()
        {
            await CargarCategoriasAsync();
            return View();
        }

        // POST: Productos/Importar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Importar(IFormFile archivoExcel, int? categoriaIdPorDefecto)
        {
            if (archivoExcel == null || archivoExcel.Length == 0)
            {
                TempData["ErrorMessage"] = "Por favor seleccione un archivo Excel (.xlsx)";
                await CargarCategoriasAsync(categoriaIdPorDefecto);
                return View();
            }

            var resultado = new ImportacionResultado
            {
                ProductosImportados = 0,
                ProductosDuplicados = 0,
                ProductosConError = 0,
                Errores = new List<string>(),
                CodigosDuplicados = new List<string>()
            };

            try
            {
                var codigosExistentes = await _context.Productos
                    .Select(p => p.Codigo.ToLower())
                    .ToListAsync();

                var productosNuevos = new List<Producto>();
                
                using (var stream = archivoExcel.OpenReadStream())
                {
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Saltar encabezado

                        foreach (var row in rows)
                        {
                            try
                            {
                                var codigo = row.Cell(1).GetValue<string>()?.Trim();
                                var nombre = row.Cell(2).GetValue<string>()?.Trim();
                                var descripcion = row.Cell(3).GetValue<string>()?.Trim();
                                var precio = row.Cell(4).GetValue<decimal>();
                                var stock = row.Cell(5).GetValue<int>();
                                var categoriaTexto = row.Cell(6).GetValue<string>()?.Trim();

                                if (string.IsNullOrEmpty(codigo) || string.IsNullOrEmpty(nombre))
                                {
                                    resultado.ProductosConError++;
                                    resultado.Errores.Add($"Fila {row.RowNumber()}: Código y nombre son obligatorios");
                                    continue;
                                }

                                if (codigosExistentes.Contains(codigo.ToLower()) ||
                                    productosNuevos.Any(p => p.Codigo.ToLower() == codigo.ToLower()))
                                {
                                    resultado.ProductosDuplicados++;
                                    resultado.CodigosDuplicados.Add(codigo);
                                    continue;
                                }

                                int? categoriaId = categoriaIdPorDefecto;
                                if (!string.IsNullOrEmpty(categoriaTexto))
                                {
                                    var categoria = await _context.Categorias
                                        .FirstOrDefaultAsync(c => c.Nombre.ToLower() == categoriaTexto.ToLower());
                                    if (categoria != null)
                                    {
                                        categoriaId = categoria.Id;
                                    }
                                }

                                productosNuevos.Add(new Producto
                                {
                                    Codigo = codigo,
                                    Nombre = nombre,
                                    Descripcion = descripcion,
                                    Precio = precio,
                                    Stock = stock,
                                    CategoriaId = categoriaId,
                                    Activo = true,
                                    FechaCreacion = DateTime.Now
                                });
                                resultado.ProductosImportados++;
                            }
                            catch (Exception ex)
                            {
                                resultado.ProductosConError++;
                                resultado.Errores.Add($"Fila {row.RowNumber()}: {ex.Message}");
                            }
                        }
                    }
                }

                if (productosNuevos.Any())
                {
                    _context.Productos.AddRange(productosNuevos);
                    await _context.SaveChangesAsync();

                    foreach (var producto in productosNuevos)
                    {
                        producto.CodigoBarras = _barcodeService.GenerarCodigoBarras(producto.Codigo, producto.Id);
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["ImportacionResultado"] = System.Text.Json.JsonSerializer.Serialize(resultado);
                return RedirectToAction(nameof(ResultadoImportacion));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al procesar el archivo Excel: {ex.Message}";
                await CargarCategoriasAsync(categoriaIdPorDefecto);
                return View();
            }
        }

        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString().Trim());
            return result;
        }

        // GET: Productos/ResultadoImportacion
        public IActionResult ResultadoImportacion()
        {
            if (TempData["ImportacionResultado"] == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var resultado = System.Text.Json.JsonSerializer.Deserialize<ImportacionResultado>(
                TempData["ImportacionResultado"].ToString());

            return View(resultado);
        }

        // GET: Productos/Exportar
        public async Task<IActionResult> Exportar(int? categoriaId)
        {
            var query = _context.Productos
                .Include(p => p.Categoria)
                .AsQueryable();

            if (categoriaId.HasValue)
                query = query.Where(p => p.CategoriaId == categoriaId);

            var productos = await query.OrderBy(p => p.Nombre).ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Productos");
                
                // Encabezados con estilo
                var headerRow = worksheet.Row(1);
                headerRow.Cell(1).Value = "Código";
                headerRow.Cell(2).Value = "Nombre";
                headerRow.Cell(3).Value = "Descripción";
                headerRow.Cell(4).Value = "Precio";
                headerRow.Cell(5).Value = "Stock";
                headerRow.Cell(6).Value = "Categoría";
                headerRow.Cell(7).Value = "Activo";

                var headerStyle = workbook.Style;
                headerStyle.Font.Bold = true;
                headerStyle.Fill.BackgroundColor = XLColor.FromHtml("#405189");
                headerStyle.Font.FontColor = XLColor.White;
                worksheet.Range("A1:G1").Style = headerStyle;

                // Datos
                int rowNum = 2;
                foreach (var p in productos)
                {
                    worksheet.Cell(rowNum, 1).Value = p.Codigo;
                    worksheet.Cell(rowNum, 2).Value = p.Nombre;
                    worksheet.Cell(rowNum, 3).Value = p.Descripcion;
                    worksheet.Cell(rowNum, 4).Value = p.Precio;
                    worksheet.Cell(rowNum, 5).Value = p.Stock;
                    worksheet.Cell(rowNum, 6).Value = p.Categoria?.Nombre ?? "Sin categoría";
                    worksheet.Cell(rowNum, 7).Value = p.Activo ? "Sí" : "No";
                    rowNum++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    var fileName = $"Productos_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        private string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }

        // GET: Productos/DescargarPlantilla
        public IActionResult DescargarPlantilla()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Plantilla Importación");

                // Encabezados
                worksheet.Cell(1, 1).Value = "Código";
                worksheet.Cell(1, 2).Value = "Nombre";
                worksheet.Cell(1, 3).Value = "Descripción";
                worksheet.Cell(1, 4).Value = "Precio";
                worksheet.Cell(1, 5).Value = "Stock";
                worksheet.Cell(1, 6).Value = "Categoría";

                // Estilo encabezados
                var headerRange = worksheet.Range("A1:F1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                // Datos de ejemplo
                worksheet.Cell(2, 1).Value = "PROD001";
                worksheet.Cell(2, 2).Value = "Producto de Ejemplo";
                worksheet.Cell(2, 3).Value = "Opcional";
                worksheet.Cell(2, 4).Value = 100.00;
                worksheet.Cell(2, 5).Value = 50;
                worksheet.Cell(2, 6).Value = "General";

                // Instrucciones en otra hoja o celdas distantes
                worksheet.Cell(5, 1).Value = "INSTRUCCIONES:";
                worksheet.Cell(5, 1).Style.Font.Bold = true;
                worksheet.Cell(6, 1).Value = "- Código y Nombre son obligatorios.";
                worksheet.Cell(7, 1).Value = "- El Código debe ser único.";
                worksheet.Cell(8, 1).Value = "- El formato de archivo debe ser .xlsx";

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Plantilla_Productos.xlsx");
                }
            }
        }

        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.Id == id);
        }

        // POST: Productos/GenerarCodigoBarras/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarCodigoBarras(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            producto.CodigoBarras = _barcodeService.GenerarCodigoBarras(producto.Codigo, producto.Id);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Código de barras generado correctamente.";
            return RedirectToAction(nameof(Details), new { id = producto.Id });
        }

        private async Task CargarCategoriasAsync(int? categoriaSeleccionada = null)
        {
            var categorias = await _context.Categorias
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            ViewData["CategoriaId"] = new SelectList(categorias, "Id", "Nombre", categoriaSeleccionada);
        }

        private async Task CargarProveedoresAsync(int? proveedorSeleccionado = null)
        {
            var proveedores = await _context.Proveedores
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            ViewData["ProveedorId"] = new SelectList(proveedores, "Id", "Nombre", proveedorSeleccionado);
        }

        [HttpGet]
        public async Task<IActionResult> GetProductosJson(int? categoriaId, string? buscar, int page = 1, int pageSize = 10)
        {
            var query = _context.Productos
                .Include(p => p.Categoria)
                .AsQueryable();

            if (categoriaId.HasValue)
            {
                query = query.Where(p => p.CategoriaId == categoriaId);
            }

            if (!string.IsNullOrEmpty(buscar))
            {
                query = query.Where(p => p.Nombre.Contains(buscar) ||
                                       p.Codigo.Contains(buscar) ||
                                       (p.Descripcion != null && p.Descripcion.Contains(buscar)));
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var productos = await query
                .OrderBy(p => p.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Codigo,
                    p.Nombre,
                    p.Descripcion,
                    p.Precio,
                    p.Stock,
                    p.Activo,
                    CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : "Sin categoría"
                })
                .ToListAsync();

            return Json(new
            {
                items = productos,
                totalItems,
                totalPages,
                currentPage = page,
                pageSize
            });
        }
    }

    // ViewModel para el resultado de importación
    public class ImportacionResultado
    {
        public int TotalFilas { get; set; }
        public int ProductosImportados { get; set; }
        public int ProductosDuplicados { get; set; }
        public int ProductosConError { get; set; }
        public List<string> Errores { get; set; } = new List<string>();
        public List<string> CodigosDuplicados { get; set; } = new List<string>();
    }
}
