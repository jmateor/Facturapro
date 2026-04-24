# Memoria del Proyecto - Facturapro

> **Propósito:** Este archivo sirve como memoria centralizada para los modelos de IA que trabajen en este proyecto. Documenta decisiones arquitectónicas, configuraciones, y cambios importantes que afectan múltiples componentes.

---

## Información del Proyecto

| Campo | Valor |
|-------|-------|
| **Nombre** | Facturapro |
| **Framework** | ASP.NET Core 8.0 |
| **Base de Datos** | SQL Server |
| **ORM** | Entity Framework Core 8.0 |
| **UI** | Bootstrap 5 + Razor Views |
| **Ubicación** | D:\Facturapro |

---

## Configuración Centralizada de Impresión

**Fecha:** 2026-04-23

### Ubicación de la Configuración
Todos los módulos que necesiten parámetros de impresión deben consultar:
- **Modelo:** `Models/Entities/ConfiguracionEmpresa.cs`
- **Tabla DB:** `ConfiguracionEmpresas`

### Campos de Configuración de Impresión

```csharp
// Tipo de ticket: thermal (58-80mm), half (140x216mm), letter (216x279mm)
public string TipoTicket { get; set; } = "thermal";

// Elementos visibles en el comprobante
public bool MostrarLogo { get; set; } = true;
public bool MostrarDescripcion { get; set; } = true;
public bool MostrarCodigoBarras { get; set; } = true;
public bool MostrarNCF { get; set; } = false;  // NCF = Número de Comprobante Fiscal
public bool MostrarImpuestos { get; set; } = true;

// Pie de página personalizado (máx 200 caracteres)
public string? PiePagina { get; set; }

// Tamaño de fuente del pie: small, medium, large
public string TamanoFuentePie { get; set; } = "medium";
```

### Cómo Consultar la Configuración

En cualquier controlador o servicio que genere comprobantes:

```csharp
var config = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();

// Usar configuración de impresión
if (config.MostrarLogo) { /* incluir logo */ }
if (config.MostrarDescripcion) { /* incluir descripción */ }
// etc...
```

### Vista de Configuración
- **Ruta:** `/Configuracion/Impresion`
- **Controller:** `ConfiguracionController.Impresion()`
- **View:** `Views/Configuracion/Impresion.cshtml`

---

## Configuración Centralizada de Ventas

**Fecha:** 2026-04-23

### Ubicación de la Configuración
Todos los módulos que necesiten parámetros de ventas deben consultar:
- **Modelo:** `Models/Entities/ConfiguracionEmpresa.cs`
- **Tabla DB:** `ConfiguracionEmpresas`

### Campos de Configuración de Ventas

```csharp
// Reglas de venta
public bool PermitirVentasCredito { get; set; } = true;      // Permitir pago diferido
public bool ImprimirAutomático { get; set; } = true;         // Imprimir al completar venta POS
public bool ClienteObligatorio { get; set; } = false;        // Requerir cliente en venta
public bool DescuentoCantidad { get; set; } = true;          // Descuento por volumen
public bool StockNegativo { get; set; } = false;             // Permitir ventas sin stock
public bool UsarValoresDefecto { get; set; } = true;         // Autocompletar campos

// Configuración de precios
public int DecimalesPrecios { get; set; } = 2;               // 0, 2, 3, 4 decimales
public string? RedondeoTotales { get; set; } = "none";       // none, 0.05, 0.10, 0.50, 1.00
public string Moneda { get; set; } = "DOP";                  // DOP, USD, EUR
public string SimboloMoneda { get; set; } = "prefix";        // prefix o suffix

// Configuración de facturación
public string TipoComprobanteDefecto { get; set; } = "E31";  // E31, E32, E41, E43, etc.
public string TipoIngresoPorDefecto { get; set; } = "01";    // 01-06 para DGII
public int TipoPagoPorDefecto { get; set; } = 1;             // 1=Contado, 2=Crédito, 3=Mixto

// Límites y alertas
public decimal MontoVentaMinima { get; set; } = 0;           // 0 = sin límite
public decimal MontoVentaMaxima { get; set; } = 0;           // 0 = sin límite
public decimal MontoCreditoMaximo { get; set; } = 0;         // Límite de crédito
public int DiasPlazoCredito { get; set; } = 30;              // Días de crédito por defecto
```

### Cómo Consultar la Configuración

En el POS o módulo de facturación:

```csharp
var config = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();

// Verificar si permite ventas a crédito
if (!config.PermitirVentasCredito && tipoPago == Credito) { /* mostrar error */ }

// Verificar stock negativo
if (producto.Stock < cantidad && !config.StockNegativo) { /* mostrar error */ }

// Aplicar redondeo
if (config.RedondeoTotales != "none") { /* aplicar redondeo */ }

// Usar tipo de comprobante por defecto
var tipoComprobante = config.TipoComprobanteDefecto; // "E31"
```

### Vista de Configuración
- **Ruta:** `/Configuracion/Ventas`
- **Controller:** `ConfiguracionController.Ventas()`
- **View:** `Views/Configuracion/Ventas.cshtml`

---

## Super Administrador por Defecto

**Fecha:** 2026-04-23

### Credenciales de Instalación
En `Program.cs` se crea automáticamente un usuario super administrador:

```csharp
// Email: josemateo3148@gmail.com
// Contraseña: Jmateor11099@
// Rol: Admin
```

### Propósito
Este usuario permite gestionar otros usuarios después de una instalación nueva. **Importante:** Cambiar la contraseña en producción.

---

## Control de Usuarios - Solo Administradores

**Fecha:** 2026-04-23

### Restricción de Permisos
Solo los usuarios con rol `Admin` pueden:
- Crear nuevos usuarios
- Editar usuarios (nombre, apellido, rol)
- Cambiar contraseñas de otros usuarios
- Eliminar usuarios

### Implementación en Controller
En `AccountController.cs`:

```csharp
[Authorize(Roles = "Admin")]
public async Task<IActionResult> EditarUsuario(string id, string nombre, string apellido, string rol)
{
    // Solo admins pueden editar
}

[Authorize(Roles = "Admin")]
public async Task<IActionResult> CambiarContrasena(string id, string nuevaContrasena)
{
    // Solo admins pueden cambiar contraseñas
}

[Authorize(Roles = "Admin")]
public async Task<IActionResult> EliminarUsuario(string id)
{
    // Solo admins pueden eliminar
}
```

### Implementación en View
En `Views/Configuracion/Usuarios.cshtml`:

```csharp
@inject UserManager<Facturapro.Models.ApplicationUser> UserManager
var currentUser = await UserManager.GetUserAsync(User);
var esAdmin = currentUser != null && currentUser.Rol == "Admin";
```

**UI Condicional:**
- Si `!esAdmin`: Mostrar alerta de "Acceso restringido"
- Si `esAdmin`: Mostrar botón "Nuevo Usuario" y acciones (editar, cambiar contraseña, eliminar)

### Validaciones Adicionales
- No permitir eliminar el último administrador
- No permitir auto-eliminación
- No permitir auto-cambio de contraseña (debe hacerse desde Perfil)

### Cómo Consultar
En cualquier módulo que necesite verificar permisos de admin:

```csharp
var currentUser = await _userManager.GetUserAsync(User);
var esAdmin = currentUser?.Rol == "Admin";

if (!esAdmin) {
    return Forbid(); // o mostrar mensaje de acceso restringido
}
```

---

## Exportación CSV con UTF-8 BOM

**Fecha:** 2026-04-23

### Problema Resuelto
Excel no reconocía caracteres especiales (ó, í, ñ) en archivos CSV exportados.

### Solución
En `ProductosController.Exportar()` y `DescargarPlantilla()`:

```csharp
// ANTES (incorrecto):
var bytes = Encoding.UTF8.GetBytes(csv.ToString());

// AHORA (correcto con BOM):
var utf8WithBom = new UTF8Encoding(true);  // true = incluir BOM
var bytes = utf8WithBom.GetBytes(csv.ToString());
```

### Por Qué
El BOM (Byte Order Mark) es una secuencia de 3 bytes (`EF BB BF`) que le indica a Excel que el archivo está codificado en UTF-8, permitiendo mostrar correctamente los caracteres especiales.

---

## Decisiones Arquitectónicas (ADRs)

### ADR-001: Configuración Centralizada
**Decisión:** Todos los parámetros del sistema se almacenan en `ConfiguracionEmpresas`, no en `appsettings.json`.

**Razón:** Permite que los usuarios administren la configuración desde la UI sin tocar archivos de configuración.

**Impacto:** Cualquier módulo nuevo debe consultar esta tabla para parámetros configurables.

### ADR-002: Migraciones EF Personalizadas
**Decisión:** Cuando las tablas ya existen, editar manualmente el archivo de migración para usar `AddColumn()` en lugar de `CreateTable()`.

**Razón:** Evita errores de "objeto ya existe" al aplicar migraciones en bases de datos existentes.

### ADR-003: Vistas de Configuración Consistentes
**Decisión:** Todas las vistas de configuración siguen el mismo patrón:
- Page header con ícono y descripción
- Cards numeradas con avatar coloreado
- Columna izquierda: formularios
- Columna derecha: resumen/estado/ayuda
- Alerts para mensajes de éxito/error

**Archivos de referencia:**
- `Views/Configuracion/Empresa.cshtml`
- `Views/Configuracion/DGII.cshtml`
- `Views/Configuracion/Impresion.cshtml`
- `Views/Configuracion/Ventas.cshtml`

---

## Estructura de Controladores

| Controlador | Ruta | Propósito |
|------------|------|-----------|
| `AccountController` | `/Account` | Login, Logout, Registro, Perfil |
| `ConfiguracionController` | `/Configuracion` | Configuración del sistema |
| `ProductosController` | `/Productos` | CRUD de productos, Importar/Exportar CSV |
| `ClientesController` | `/Clientes` | CRUD de clientes |
| `ProveedoresController` | `/Proveedores` | CRUD de proveedores |
| `ComprasController` | `/Compras` | Gestión de compras |
| `FacturasController` | `/Facturas` | Emisión de facturas electrónicas |
| `CategoriasController` | `/Categorias` | Categorías de productos |
| `RangosController` | `/Rangos` | Rangos de numeración DGII |
| `POSController` | `/POS` | Punto de venta |
| `KalderController` | `/Kalder` | Gestión de inventario Kalder |
| `ReportesController` | `/Reportes` | Reportes y dashboard |
| `HomeController` | `/` | Dashboard principal |

---

## Servicios DGII

### Archivos Clave
- `Services/DGII/DGIIService.cs` - Cliente de API DGII
- `Services/DGII/FacturacionElectronicaService.cs` - Generación de e-CF
- `Services/DGII/RangoNumeracionService.cs` - Gestión de rangos DGII

### Configuración DGII
La configuración para conectar con DGII está en `ConfiguracionEmpresa`:
- `RNCEmisor` - RNC de la empresa
- `RutaCertificado` / `PasswordCertificado` - Certificado digital
- `ModoPruebas` - true = TesteCF, false = eCF producción

---

## Convenciones de Código

### Naming
- Controladores: `XxxController.cs`
- Modelos: `Xxx.cs` en `Models/Entities/`
- ViewModels: `XxxViewModel.cs` en `Models/ViewModels/`
- Servicios: `XxxService.cs` en `Services/`

### Validación
- Usar atributos `System.ComponentModel.DataAnnotations`
- Mensajes de error en español
- Validar en controlador con `ModelState.IsValid`

### Base de Datos
- Tablas en plural: `Productos`, `Clientes`, `Facturas`
- Primary Key: `Id` (int identity)
- Foreign Keys: `XxxId` (convención EF Core)

---

## Comandos Útiles

```bash
# Build
dotnet build

# Run con HTTPS
dotnet run

# Crear migración
dotnet ef migrations add NombreMigracion --context ApplicationDbContext

# Aplicar migraciones
dotnet ef database update --context ApplicationDbContext

# Listar migraciones
dotnet ef migrations list --context ApplicationDbContext
```

---

## Archivos Importantes

| Archivo | Propósito |
|---------|-----------|
| `Program.cs` | Configuración de la aplicación, seed de usuarios |
| `appsettings.json` | Connection strings y configuración básica |
| `Data/ApplicationDbContext.cs` | Contexto de EF Core |
| `Models/Entities/ConfiguracionEmpresa.cs` | **Configuración centralizada del sistema** |
| `Migrations/` | Migraciones de base de datos |

---

## Historial de Cambios

| Fecha | Cambio | Archivo(s) |
|-------|--------|------------|
| 2026-04-25 | Módulo de Reportes reorganizado con exportación PDF | `Controllers/ReportesController.cs`, `Services/PDF/PdfService.cs` (GenerarReportePDF), `Views/Reportes/General.cshtml`, `Views/Reportes/ProductosMasVendidos.cshtml`, `Views/Reportes/VentasPorCategoria.cshtml`, `Views/Reportes/VentasPorCiudad.cshtml`, `Views/Reportes/VentasPorAnio.cshtml`, `Views/Reportes/Devoluciones.cshtml`, `Models/ViewModels/Reportes/ReporteGeneralViewModel.cs` |
| 2026-04-24 | Rediseño UX/UI módulo Kalder - Historial | `Views/Kalder/Historial.cshtml` |
| 2026-04-24 | Vista Details de Facturas con descarga PDF, WhatsApp y Email | `Views/Facturas/Details.cshtml` (nuevo) |
| 2026-04-24 | Rediseño UX/UI módulo Compras | `Views/Compras/Create.cshtml` |
| 2026-04-24 | Rediseño UX/UI módulo Proveedores (completado) | `Views/Proveedores/Create.cshtml`, `Views/Proveedores/Edit.cshtml`, `Views/Proveedores/Details.cshtml`, `Views/Proveedores/Index.cshtml` |
| 2026-04-24 | Rediseño UX/UI módulo Categorías | `Views/Categorias/Create.cshtml`, `Views/Categorias/Edit.cshtml`, `Views/Categorias/Details.cshtml`, `Views/Categorias/Index.cshtml` |
| 2026-04-24 | Rediseño UX/UI módulo Productos | `Views/Productos/Create.cshtml`, `Views/Productos/Edit.cshtml`, `Views/Productos/Details.cshtml`, `Views/Productos/Index.cshtml` |
| 2026-04-24 | Rediseño UX/UI módulo Clientes | `Views/Clientes/Create.cshtml`, `Views/Clientes/Edit.cshtml`, `Views/Clientes/Details.cshtml`, `Views/Clientes/Index.cshtml` |
| 2026-04-23 | Restricción usuarios: solo admin | `Controllers/AccountController.cs`, `Views/Configuracion/Usuarios.cshtml` |
| 2026-04-23 | Módulo de ventas renovado | `Views/Configuracion/Ventas.cshtml`, `Controllers/ConfiguracionController.cs`, `Models/Entities/ConfiguracionEmpresa.cs` |
| 2026-04-23 | Módulo de impresión renovado | `Views/Configuracion/Impresion.cshtml`, `Controllers/ConfiguracionController.cs`, `Models/Entities/ConfiguracionEmpresa.cs` |
| 2026-04-23 | CSV UTF-8 BOM para Excel | `Controllers/ProductosController.cs` |
| 2026-04-23 | Super administrador por defecto | `Program.cs` |
| 2026-04-23 | Campos de impresión en DB | `Migrations/20260423194507_AddImpresionConfig.cs` |

---

## Notas para el Modelo

1. **Siempre verificar** `ConfiguracionEmpresa` antes de agregar nuevos parámetros configurables
2. **No hardcodear** valores que puedan ser configurados por el usuario
3. **Mantener consistencia** con el patrón de diseño de las vistas de configuración
4. **Documentar aquí** cualquier cambio que afecte múltiples módulos o la arquitectura
