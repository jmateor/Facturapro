using Facturapro.Data;
using Facturapro.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Facturapro.Services.DGII
{
    /// <summary>
    /// Servicio para gestión de rangos de numeración e-CF
    /// </summary>
    public class RangoNumeracionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RangoNumeracionService> _logger;

        public RangoNumeracionService(
            ApplicationDbContext context,
            ILogger<RangoNumeracionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el siguiente número de e-CF disponible para un tipo de comprobante
        /// </summary>
        public async Task<ResultadoOperacion> ObtenerSiguienteNumeroAsync(string tipoECF)
        {
            try
            {
                // Buscar rango activo para el tipo de comprobante
                var rango = await _context.RangoNumeraciones
                    .Where(r => r.TipoECF == tipoECF
                            && r.Activo
                            && r.Estado == EstadoRango.Activo)
                    .OrderBy(r => r.FechaVencimiento)
                    .FirstOrDefaultAsync();

                if (rango == null)
                {
                    return new ResultadoOperacion
                    {
                        Exito = false,
                        Mensaje = $"No hay rangos activos disponibles para el tipo de comprobante {tipoECF}"
                    };
                }

                // Verificar si el rango está por agotarse
                if (rango.PorcentajeUsado >= 80)
                {
                    _logger.LogWarning("El rango {RangoId} está al {Porcentaje}% de uso",
                        rango.Id, rango.PorcentajeUsado);
                }

                // Verificar vencimiento
                if (rango.FechaVencimiento <= DateTime.Now)
                {
                    rango.Estado = EstadoRango.Vencido;
                    await _context.SaveChangesAsync();

                    return new ResultadoOperacion
                    {
                        Exito = false,
                        Mensaje = "El rango de numeración ha vencido. Solicite uno nuevo a la DGII."
                    };
                }

                // Obtener el siguiente número
                var siguienteNumero = rango.ObtenerSiguienteNumero();

                if (string.IsNullOrEmpty(siguienteNumero))
                {
                    return new ResultadoOperacion
                    {
                        Exito = false,
                        Mensaje = "El rango de numeración se ha agotado"
                    };
                }

                // Incrementar el contador
                rango.Incrementar();
                await _context.SaveChangesAsync();

                return new ResultadoOperacion
                {
                    Exito = true,
                    Mensaje = "Número obtenido exitosamente",
                    Data = siguienteNumero
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener siguiente número de e-CF");
                return new ResultadoOperacion
                {
                    Exito = false,
                    Mensaje = "Error interno al procesar la solicitud"
                };
            }
        }

        /// <summary>
        /// Crea un nuevo rango de numeración
        /// </summary>
        public async Task<ResultadoOperacion> CrearRangoAsync(
            string tipoECF,
            string rangoDesde,
            string rangoHasta,
            DateTime fechaVencimiento,
            string? observaciones = null)
        {
            try
            {
                // Validar formato
                if (!ValidarFormatoNumeroECF(rangoDesde) || !ValidarFormatoNumeroECF(rangoHasta))
                {
                    return new ResultadoOperacion
                    {
                        Exito = false,
                        Mensaje = "Formato de número e-CF inválido. Debe ser: E + 2 dígitos tipo + 10 dígitos secuencial"
                    };
                }

                // Validar que el tipo coincida
                var tipoDesde = rangoDesde.Substring(1, 2);
                var tipoHasta = rangoHasta.Substring(1, 2);

                if (tipoDesde != tipoECF || tipoHasta != tipoECF)
                {
                    return new ResultadoOperacion
                    {
                        Exito = false,
                        Mensaje = "El tipo de comprobante no coincide con los rangos especificados"
                    };
                }

                // Crear rango
                var rango = new RangoNumeracion
                {
                    TipoECF = tipoECF,
                    RangoDesde = rangoDesde,
                    RangoHasta = rangoHasta,
                    NumeroActual = long.Parse(rangoDesde.Substring(3)) - 1, // Empezar antes del primer número
                    FechaVencimiento = fechaVencimiento,
                    Observaciones = observaciones,
                    Estado = EstadoRango.Activo,
                    Activo = true
                };

                _context.RangoNumeraciones.Add(rango);
                await _context.SaveChangesAsync();

                return new ResultadoOperacion
                {
                    Exito = true,
                    Mensaje = "Rango creado exitosamente",
                    Data = rango
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear rango de numeración");
                return new ResultadoOperacion
                {
                    Exito = false,
                    Mensaje = "Error al crear el rango"
                };
            }
        }

        /// <summary>
        /// Verifica si hay rangos disponibles para un tipo de comprobante
        /// </summary>
        public async Task<bool> ExisteRangoDisponibleAsync(string tipoECF)
        {
            return await _context.RangoNumeraciones
                .AnyAsync(r => r.TipoECF == tipoECF
                        && r.Activo
                        && r.Estado == EstadoRango.Activo
                        && r.FechaVencimiento > DateTime.Now);
        }

        /// <summary>
        /// Obtiene estadísticas de uso de rangos
        /// </summary>
        public async Task<EstadisticasRangos> ObtenerEstadisticasAsync()
        {
            var rangos = await _context.RangoNumeraciones.ToListAsync();

            return new EstadisticasRangos
            {
                TotalRangos = rangos.Count,
                RangosActivos = rangos.Count(r => r.Estado == EstadoRango.Activo),
                RangosAgotados = rangos.Count(r => r.Estado == EstadoRango.Agotado),
                RangosVencidos = rangos.Count(r => r.Estado == EstadoRango.Vencido),
                RangosConAlerta = rangos.Where(r => r.Estado == EstadoRango.Activo)
                                       .Count(r => r.PorcentajeUsado >= 80),
                DetallePorTipo = rangos.GroupBy(r => r.TipoECF)
                                      .Select(g => new EstadisticaPorTipo
                                      {
                                          TipoECF = g.Key,
                                          TipoECFNombre = ObtenerNombreTipo(g.Key),
                                          Total = g.Count(),
                                          Disponibles = g.Count(r => r.Estado == EstadoRango.Activo && r.CantidadDisponible > 0)
                                      })
                                      .ToList()
            };
        }

        /// <summary>
        /// Valida el formato de un número e-CF
        /// </summary>
        private static bool ValidarFormatoNumeroECF(string numeroECF)
        {
            if (string.IsNullOrEmpty(numeroECF) || numeroECF.Length != 13)
                return false;

            if (numeroECF[0] != 'E')
                return false;

            var tipo = numeroECF.Substring(1, 2);
            var secuencial = numeroECF.Substring(3, 10);

            // Validar que el tipo sea numérico
            if (!int.TryParse(tipo, out _))
                return false;

            // Validar que el secuencial sea numérico
            return long.TryParse(secuencial, out _);
        }

        private static string ObtenerNombreTipo(string tipoECF)
        {
            return tipoECF switch
            {
                "31" => "Factura de Crédito Fiscal",
                "32" => "Factura de Consumo",
                "33" => "Nota de Débito",
                "34" => "Nota de Crédito",
                "41" => "Comprobante de Compras",
                "43" => "Gastos Menores",
                "44" => "Regímenes Especiales",
                "45" => "Gubernamental",
                "46" => "Exportaciones",
                "47" => "Pagos al Exterior",
                _ => "Desconocido"
            };
        }
    }

    public class ResultadoOperacion
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    public class EstadisticasRangos
    {
        public int TotalRangos { get; set; }
        public int RangosActivos { get; set; }
        public int RangosAgotados { get; set; }
        public int RangosVencidos { get; set; }
        public int RangosConAlerta { get; set; }
        public List<EstadisticaPorTipo> DetallePorTipo { get; set; } = new();
    }

    public class EstadisticaPorTipo
    {
        public string TipoECF { get; set; } = string.Empty;
        public string TipoECFNombre { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Disponibles { get; set; }
    }
}
