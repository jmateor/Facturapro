using Facturapro.Data;
using Microsoft.EntityFrameworkCore;

namespace Facturapro.Services.DGII
{
    /// <summary>
    /// Servicio en background para consultar periódicamente el estado de comprobantes enviados a la DGII
    /// </summary>
    public class DGIIBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DGIIBackgroundService> _logger;

        public DGIIBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<DGIIBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de consulta DGII iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var apiService = scope.ServiceProvider.GetRequiredService<IFacturacionElectronicaAPIService>();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // Consultar facturas en estado EnProceso (enviadas pero sin respuesta final)
                    var facturasPendientes = await context.Facturas
                        .Where(f => f.EstadoDGII == "EnProceso" || f.EstadoDGII == "Enviado")
                        .CountAsync(stoppingToken);

                    if (facturasPendientes > 0)
                    {
                        _logger.LogInformation("Consultando estado de {Cantidad} facturas pendientes", facturasPendientes);
                        await apiService.ConsultarEstadoPendientesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en servicio de consulta DGII");
                }

                // Esperar 5 minutos antes de la siguiente consulta
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
