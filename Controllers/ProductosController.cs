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
            return View();
        }

        // POST: Productos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Codigo,Nombre,Descripcion,Precio,Stock,CategoriaId")] Producto producto)
        {
            // Validar que el código no exista
            if (await _context.Productos.AnyAsync(p => p.Codigo == producto.Codigo))
            {
                ModelState.AddModelError("Codigo", "Ya existe un producto con este código");
            }

            if (ModelState.IsValid)
            {
                producto.FechaCreacion = DateTime.Now;
                _context.Add(producto);
                await _context.SaveChangesAsync();

                // Generar código de barras
                producto.CodigoBarras = _barcodeService.GenerarCodigoBarras(producto.Codigo, producto.Id);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Producto creado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            await CargarCategoriasAsync(producto.CategoriaId);
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
            return View(producto);
        }

        // POST: Productos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Codigo,Nombre,Descripcion,Precio,Stock,CategoriaId,Activo")] Producto producto)
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
                TempData["ErrorMessage"] = "Por favor seleccione un archivo CSV";
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
                // Obtener códigos existentes para detectar duplicados
                var codigosExistentes = await _context.Productos
                    .Select(p => p.Codigo.ToLower())
                    .ToListAsync();

                var productosNuevos = new List<Producto>();
                int rowNumber = 1;

                using (var stream = archivoExcel.OpenReadStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    // Leer encabezados
                    var headerLine = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(headerLine))
                    {
                        TempData["ErrorMessage"] = "El archivo no contiene datos";
                        await CargarCategoriasAsync(categoriaIdPorDefecto);
                        return View();
                    }

                    string? line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        rowNumber++;

                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        try
                        {
                            var campos = ParseCsvLine(line);

                            // Validar mínimo de columnas
                            if (campos.Count < 2)
                            {
                                resultado.ProductosConError++;
                                resultado.Errores.Add($"Fila {rowNumber}: Formato CSV inválido");
                                continue;
                            }

                            var codigo = campos[0]?.Trim() ?? "";
                            var nombre = campos[1]?.Trim() ?? "";
                            var descripcion = campos.Count > 2 ? campos[2]?.Trim() : null;
                            var precioTexto = campos.Count > 3 ? campos[3]?.Trim() ?? "0" : "0";
                            var stockTexto = campos.Count > 4 ? campos[4]?.Trim() ?? "0" : "0";
                            var categoriaTexto = campos.Count > 5 ? campos[5]?.Trim() : null;

                            // Validaciones básicas
                            if (string.IsNullOrEmpty(codigo) || string.IsNullOrEmpty(nombre))
                            {
                                resultado.ProductosConError++;
                                resultado.Errores.Add($"Fila {rowNumber}: Código y nombre son obligatorios");
                                continue;
                            }

                            // Verificar duplicado
                            if (codigosExistentes.Contains(codigo.ToLower()) ||
                                productosNuevos.Any(p => p.Codigo.ToLower() == codigo.ToLower()))
                            {
                                resultado.ProductosDuplicados++;
                                resultado.CodigosDuplicados.Add(codigo);
                                continue;
                            }

                            // Parsear precio y stock
                            if (!decimal.TryParse(precioTexto.Replace("$", "").Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var precio))
                            {
                                precio = 0;
                            }

                            if (!int.TryParse(stockTexto, out var stock))
                            {
                                stock = 0;
                            }

                            // Determinar categoría
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

                            var producto = new Producto
                            {
                                Codigo = codigo,
                                Nombre = nombre,
                                Descripcion = descripcion,
                                Precio = precio,
                                Stock = stock,
                                CategoriaId = categoriaId,
                                Activo = true,
                                FechaCreacion = DateTime.Now
                            };

                            productosNuevos.Add(producto);
                            resultado.ProductosImportados++;
                        }
                        catch (Exception ex)
                        {
                            resultado.ProductosConError++;
                            resultado.Errores.Add($"Fila {rowNumber}: {ex.Message}");
                        }
                    }

                    resultado.TotalFilas = rowNumber - 1;
                }

                // Guardar productos nuevos
                if (productosNuevos.Any())
                {
                    _context.Productos.AddRange(productosNuevos);
                    await _context.SaveChangesAsync();

                    // Generar códigos de barras para productos importados
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
                TempData["ErrorMessage"] = $"Error al procesar el archivo: {ex.Message}";
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

            var csv = new StringBuilder();
            csv.AppendLine("Código,Nombre,Descripción,Precio,Stock,Categoría,Activo");

            foreach (var producto in productos)
            {
                var line = string.Join(",",
                    EscapeCsvField(producto.Codigo),
                    EscapeCsvField(producto.Nombre),
                    EscapeCsvField(producto.Descripcion),
                    producto.Precio.ToString(CultureInfo.InvariantCulture),
                    producto.Stock.ToString(),
                    EscapeCsvField(producto.Categoria?.Nombre ?? "Sin categoría"),
                    producto.Activo ? "Sí" : "No"
                );
                csv.AppendLine(line);
            }

            // Agregar BOM para que Excel reconozca UTF-8 correctamente
            var utf8WithBom = new UTF8Encoding(true);
            var bytes = utf8WithBom.GetBytes(csv.ToString());
            var stream = new MemoryStream(bytes);

            var fileName = $"Productos_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(stream, "text/csv", fileName);
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
            var csv = new StringBuilder();

            // Encabezados
            csv.AppendLine("Código,Nombre,Descripción,Precio,Stock,Categoría");

            // Ejemplo de datos
            csv.AppendLine("PROD001,Producto de ejemplo,Descripción opcional,100.00,50,General");
            csv.AppendLine("PROD002,Otro producto,,250.50,25,Electrónicos");

            // Instrucciones como comentarios (líneas que empiezan con # no se procesan)
            csv.AppendLine("# INSTRUCCIONES:");
            csv.AppendLine("# - Los campos Código y Nombre son obligatorios");
            csv.AppendLine("# - El código debe ser único (no debe existir en el sistema)");
            csv.AppendLine("# - La categoría debe coincidir exactamente con una existente o dejar en blanco");
            csv.AppendLine("# - Precio y stock deben ser valores numéricos");
            csv.AppendLine("# - Use comillas dobles si un campo contiene comas");

            // Agregar BOM para que Excel reconozca UTF-8 correctamente
            var utf8WithBom = new UTF8Encoding(true);
            var bytes = utf8WithBom.GetBytes(csv.ToString());
            var stream = new MemoryStream(bytes);

            return File(stream, "text/csv", "Plantilla_Productos.csv");
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
