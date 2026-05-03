using Facturapro.Models;
using Facturapro.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Facturapro.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Factura> Facturas { get; set; }
        public DbSet<FacturaLinea> FacturaLineas { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<RangoNumeracion> RangoNumeraciones { get; set; }
        public DbSet<ConfiguracionEmpresa> ConfiguracionEmpresas { get; set; }
        public DbSet<ConfiguracionDispositivo> ConfiguracionDispositivos { get; set; }
        public DbSet<ConfiguracionIntegracion> ConfiguracionIntegraciones { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Compra> Compras { get; set; }
        public DbSet<CompraLinea> CompraLineas { get; set; }
        public DbSet<MovimientoInventario> MovimientosInventario { get; set; }
        public DbSet<FacturaAudit> FacturaAudits { get; set; }
        public DbSet<LogAuditoria> LogsAuditoria { get; set; }
        public DbSet<SesionCaja> SesionesCaja { get; set; }

        // Módulo de Sucursales y Almacenes
        public DbSet<Sucursal> Sucursales { get; set; }
        public DbSet<Almacen> Almacenes { get; set; }
        public DbSet<StockAlmacen> StocksAlmacen { get; set; }
        public DbSet<TransferenciaInventario> TransferenciasInventario { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de tablas Identity (usar nombres personalizados si se desea)
            // Por defecto, Identity usa AspNetUsers, AspNetRoles, etc.

            // Configuración de FacturaAudit
            modelBuilder.Entity<FacturaAudit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Accion).IsRequired().HasMaxLength(50);
                entity.Property(e => e.UsuarioId).IsRequired().HasMaxLength(450);
                entity.HasIndex(e => e.FacturaId);
                entity.HasIndex(e => e.Fecha);
            });

            // Configuración de Cliente
            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Email).HasMaxLength(150);
                entity.Property(e => e.NIF).HasMaxLength(20);
                entity.Property(e => e.RNC).HasMaxLength(11);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.RNC).IsUnique();
            });

            // Configuración de Factura
            modelBuilder.Entity<Factura>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NumeroFactura).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Estado).HasMaxLength(50);
                entity.Property(e => e.EstadoDGII).HasMaxLength(50);
                entity.Property(e => e.eNCF).HasMaxLength(13);
                entity.Property(e => e.TipoECF).HasMaxLength(2);
                entity.HasOne(e => e.Cliente)
                      .WithMany(c => c.Facturas)
                      .HasForeignKey(e => e.ClienteId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.Subtotal).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MontoITBIS).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalITBIS).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Total).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TasaCambio).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalDOP).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MontoITBISRetenido).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MontoISRRetenido).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PorcentajeITBIS).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MontoEfectivo).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MontoTarjeta).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MontoTransferencia).HasColumnType("decimal(18,2)");

                entity.HasIndex(e => e.NumeroFactura).IsUnique();
                entity.HasIndex(e => e.eNCF).IsUnique();
            });

            // Configuración de FacturaLinea
            modelBuilder.Entity<FacturaLinea>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Descripcion).IsRequired().HasMaxLength(500);
                entity.Property(e => e.NombreItem).HasMaxLength(80);
                entity.Property(e => e.UnidadMedida).HasMaxLength(20);
                entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MontoITBIS).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Subtotal).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Descuento).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MontoDescuento).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Factura)
                      .WithMany(f => f.Lineas)
                      .HasForeignKey(e => e.FacturaId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de RangoNumeracion
            modelBuilder.Entity<RangoNumeracion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TipoECF).IsRequired().HasMaxLength(2);
                entity.Property(e => e.RangoDesde).IsRequired().HasMaxLength(13);
                entity.Property(e => e.RangoHasta).IsRequired().HasMaxLength(13);
                entity.HasIndex(e => new { e.TipoECF, e.Estado });
            });

            // Configuración de ConfiguracionEmpresa
            modelBuilder.Entity<ConfiguracionEmpresa>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RNCEmisor).IsRequired().HasMaxLength(11);
                entity.Property(e => e.RazonSocialEmisor).IsRequired().HasMaxLength(150);
                entity.HasIndex(e => e.RNCEmisor).IsUnique();
            });

            // Configuración de Categoria
            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Nombre).IsUnique();
            });

            // Actualizar Configuración de Producto para incluir relación con Categoria y código de barras
            modelBuilder.Entity<Producto>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Codigo).HasMaxLength(50);
                entity.Property(e => e.CodigoBarras).HasMaxLength(50);
                entity.Property(e => e.CodigoBarrasImagenUrl).HasMaxLength(500);
                entity.Property(e => e.Ubicacion).HasMaxLength(100);
                entity.Property(e => e.NumeroLote).HasMaxLength(50);
                entity.Property(e => e.UnidadMedida).HasMaxLength(20);
                entity.HasIndex(e => e.Codigo).IsUnique();
                entity.HasIndex(e => e.CodigoBarras).IsUnique();
                entity.Property(e => e.Precio).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PrecioCompra).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Stock).HasColumnType("decimal(18,2)");
                entity.Property(e => e.StockMinimo).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Categoria)
                      .WithMany(c => c.Productos)
                      .HasForeignKey(e => e.CategoriaId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Proveedor)
                      .WithMany(p => p.Productos)
                      .HasForeignKey(e => e.ProveedorId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configuración de Proveedor
            modelBuilder.Entity<Proveedor>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Documento).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(150);
                entity.Property(e => e.Telefono).HasMaxLength(50);
                entity.Property(e => e.PersonaContacto).HasMaxLength(100);
                entity.HasIndex(e => e.Nombre).IsUnique();
            });

            // Configuración de Compra
            modelBuilder.Entity<Compra>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NumeroFactura).IsRequired().HasMaxLength(50);
                entity.HasOne(e => e.Proveedor)
                      .WithMany(p => p.Compras)
                      .HasForeignKey(e => e.ProveedorId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.NumeroFactura).IsUnique();
            });

            // Configuración de CompraLinea
            modelBuilder.Entity<CompraLinea>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Descripcion).IsRequired().HasMaxLength(200);
                entity.HasOne(e => e.Compra)
                      .WithMany(c => c.Lineas)
                      .HasForeignKey(e => e.CompraId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Producto)
                      .WithMany()
                      .HasForeignKey(e => e.ProductoId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de MovimientoInventario
            modelBuilder.Entity<MovimientoInventario>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Motivo).HasMaxLength(500);
                entity.Property(e => e.UsuarioRegistro).HasMaxLength(100);
                entity.HasOne(e => e.Producto)
                      .WithMany()
                      .HasForeignKey(e => e.ProductoId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Compra)
                      .WithMany()
                      .HasForeignKey(e => e.CompraId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Factura)
                      .WithMany()
                      .HasForeignKey(e => e.FacturaId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(e => new { e.ProductoId, e.FechaMovimiento });
            });
            
            // Configuración de LogAuditoria
            modelBuilder.Entity<LogAuditoria>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Usuario).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Modulo).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Accion).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Descripcion).IsRequired();
                entity.HasIndex(e => e.Fecha);
                entity.HasIndex(e => e.Usuario);
                entity.HasIndex(e => e.Modulo);
            });

            // Configuración de ConfiguracionDispositivo
            modelBuilder.Entity<ConfiguracionDispositivo>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<ConfiguracionIntegracion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TasaUSD).HasColumnType("decimal(18,2)");
            });

            // Configuración Módulo Sucursales
            modelBuilder.Entity<Sucursal>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            });

            modelBuilder.Entity<Almacen>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.HasOne(d => d.Sucursal)
                    .WithMany(p => p.Almacenes)
                    .HasForeignKey(d => d.SucursalId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StockAlmacen>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Cantidad).HasColumnType("decimal(18,2)");
                entity.HasOne(d => d.Producto)
                    .WithMany()
                    .HasForeignKey(d => d.ProductoId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(d => d.Almacen)
                    .WithMany(p => p.Stocks)
                    .HasForeignKey(d => d.AlmacenId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.ProductoId, e.AlmacenId }).IsUnique();
            });

            modelBuilder.Entity<TransferenciaInventario>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Cantidad).HasColumnType("decimal(18,2)");
                entity.HasOne(d => d.AlmacenOrigen)
                    .WithMany()
                    .HasForeignKey(d => d.AlmacenOrigenId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(d => d.AlmacenDestino)
                    .WithMany()
                    .HasForeignKey(d => d.AlmacenDestinoId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(d => d.Producto)
                    .WithMany()
                    .HasForeignKey(d => d.ProductoId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
