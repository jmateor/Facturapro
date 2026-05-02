using Facturapro.Data;
using Facturapro.Models.Entities;

namespace Facturapro.Services
{
    public interface IAuditService
    {
        Task LogAsync(string usuario, string modulo, string accion, string descripcion, string? entidadId = null, string nivel = "Info", string? ipAddress = null);
    }

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string usuario, string modulo, string accion, string descripcion, string? entidadId = null, string nivel = "Info", string? ipAddress = null)
        {
            var log = new LogAuditoria
            {
                Usuario = usuario,
                Modulo = modulo,
                Accion = accion,
                Descripcion = descripcion,
                EntidadId = entidadId,
                Nivel = nivel,
                IpAddress = ipAddress,
                Fecha = DateTime.Now
            };

            _context.LogsAuditoria.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
