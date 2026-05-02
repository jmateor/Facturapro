using Facturapro.Data;
using Facturapro.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Facturapro.Scratch
{
    public class FiscalTestScript
    {
        private readonly ApplicationDbContext _context;

        public FiscalTestScript(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task RunTest()
        {
            Console.WriteLine("--- INICIO DE TEST FISCAL DGII ---");

            // 1. Asegurar Cliente de Prueba
            var cliente = await _context.Clientes.FirstOrDefaultAsync();
            if (cliente == null)
            {
                cliente = new Cliente { Nombre = "Cliente de Prueba DGII", RNC = "131425712", Activo = true };
                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();
                Console.WriteLine("✅ Cliente de prueba creado.");
            }

            // 2. Asegurar Rango de Prueba E31
            var rango = await _context.RangoNumeraciones.FirstOrDefaultAsync(r => r.TipoECF == "31" && r.Activo);
            if (rango == null)
            {
                rango = new RangoNumeracion 
                { 
                    TipoECF = "31", 
                    RangoDesde = "E31000000001", 
                    RangoHasta = "E31000000100", 
                    NumeroActual = 0,
                    FechaVencimiento = DateTime.Now.AddYears(1),
                    Activo = true,
                    Estado = EstadoRango.Activo
                };
                _context.RangoNumeraciones.Add(rango);
                await _context.SaveChangesAsync();
                Console.WriteLine("✅ Rango E31 de prueba creado.");
            }

            // 3. Crear Factura de Prueba
            var siguienteNCF = $"E31{(rango.NumeroActual + 1).ToString("D10")}";
            
            var factura = new Factura
            {
                NumeroFactura = "TEST-" + DateTime.Now.Ticks.ToString().Substring(10),
                eNCF = siguienteNCF,
                ClienteId = cliente.Id,
                FechaEmision = DateTime.Now,
                Subtotal = 1000,
                MontoITBIS = 180,
                Total = 1180,
                TotalDOP = 1180,
                Estado = "Emitida",
                EstadoDGII = "Pendiente",
                Moneda = "DOP"
            };

            _context.Facturas.Add(factura);
            rango.NumeroActual++;
            await _context.SaveChangesAsync();

            Console.WriteLine($"🚀 Factura de prueba creada con éxito!");
            Console.WriteLine($"   NCF Asignado: {siguienteNCF}");
            Console.WriteLine($"   Cliente: {cliente.Nombre}");
            Console.WriteLine($"   Monto: {factura.Total} DOP");
            Console.WriteLine("--- FIN DE TEST FISCAL ---");
        }
    }
}
