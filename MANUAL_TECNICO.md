# 🛠️ Manual Técnico - Facturapro

> **Arquitectura, Backend y Flujos de Datos**  
> **Versión:** 1.3.0  
> **Framework:** .NET 8.0 ASP.NET Core MVC  
> **Última actualización:** 03 Mayo 2026

---

## 🏗️ 1. Arquitectura del Sistema

Facturapro utiliza una arquitectura **Monolítica Modular** basada en el patrón **MVC (Model-View-Controller)** de ASP.NET Core. El sistema está diseñado para ser altamente extensible y cumplir con los estándares de seguridad requeridos para la facturación electrónica.

### Stack Tecnológico
- **Lenguaje:** C# 12
- **Framework Web:** ASP.NET Core 8.0 MVC
- **ORM:** Entity Framework Core 8.0
- **Base de Datos:** SQL Server (Producción) / InMemory (Pruebas)
- **Frontend:** Razor Pages, JavaScript (ES6+), jQuery, CSS Vanilla (Elite UI Custom)
- **Reportes y PDF:** QuestPDF (Generación de PDF), QRCoder (Generación de QR)

---

## 📂 2. Estructura de Directorios

```text
Facturapro/
├── Controllers/           # Lógica de negocio y manejo de peticiones HTTP
├── Models/                # Entidades de base de datos y ViewModels
│   ├── Entities/          # Modelos principales (Factura, Producto, Cliente)
│   └── DGII/              # Modelos de intercambio con la API de DGII
├── Services/              # Capa de servicios (Lógica pesada desacoplada)
│   ├── DGII/              # Servicios específicos de facturación electrónica
│   ├── PDF/               # Generación de representaciones impresas
│   └── Inventario/        # Lógica de stock y movimientos (Kalder)
├── Data/                  # Contexto de base de datos (ApplicationDbContext)
├── Views/                 # Vistas Razor (HTML + C#)
├── wwwroot/               # Archivos estáticos (CSS, JS, Imágenes, Certificados)
└── Program.cs             # Configuración del pipeline y DI
```

---

## 🗄️ 3. Modelo de Datos (Esquema Principal)

### Entidades Clave

1. **Factura:** Almacena la cabecera de la factura, NCF, estados DGII y totales.
2. **FacturaDetalle:** Líneas individuales de productos, cantidades e ITBIS.
3. **Producto:** Maestro de artículos, precios, stock y códigos de barras.
4. **Cliente/Proveedor:** Directorio de entidades con validación de RNC.
5. **ConfiguracionDGII:** Almacena el RNC del emisor, certificado PFX (base64/path) y modo de operación.

### Flujo de Estados de Factura
`Pendiente` → `Firmado` → `Enviado` → `Aceptado / Rechazado`

---

## ⚙️ 4. Servicios Críticos

### `FacturacionElectronicaService.cs`
Se encarga de la orquestación del proceso de e-CF:
- Generación del XML basado en los esquemas XSD de la DGII.
- Firma digital utilizando la librería `System.Security.Cryptography.Xml`.
- Manejo de la lógica de "Notas de Crédito" vinculando el e-NCF original.

### `DGIIService.cs`
Maneja la comunicación con los Endpoints de la DGII:
- Gestión de **Semilla** y **Token JWT** (Auth OAuth2).
- Envío de XML mediante `multipart/form-data`.
- Consulta de estado asíncrona mediante `TrackId`.

### `PdfService.cs`
Utiliza **QuestPDF** para generar la representación impresa:
- Motor de diseño fluido basado en código C#.
- Generación dinámica de QR con la URL de verificación de la DGII.
- Soporte para formatos A4 y Ticket (80mm).

---

## 🌐 5. Integración DGII (Detalles de Firma)

El proceso de firma sigue el estándar **XML-DSig**:
1. Se carga el certificado `.pfx` con clave privada.
2. Se calcula el `DigestValue` de los datos.
3. Se genera la firma RSA-SHA256.
4. Se inserta el nodo `<Signature>` dentro del XML del e-CF.

---

## 🎨 6. Frontend y Elite UI

El sistema utiliza un sistema de diseño personalizado llamado **Elite UI**, enfocado en la claridad y velocidad operativa.
- **Componentes:** Card-based layouts, tablas dinámicas con AJAX.
- **Side-loading:** El listado de facturas utiliza `GetFacturasJson` para una carga asíncrona, evitando bloqueos en la UI.
- **Responsividad:** Adaptado para tablets y desktops mediante Grid Layouts.

---

## 🚀 7. Despliegue y Mantenimiento

### Requisitos de Servidor
- IIS 10+ o Kestrel (Linux/Windows)
- .NET 8 Runtime instalado
- Acceso a Internet (Puerto 443 abierto para API DGII)

### Variables de Entorno (AppSettings)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=Facturapro;..."
  },
  "DGII": {
    "Ambiente": "TesteCF",
    "UrlApi": "https://ecf.dgii.gov.do/testecf/"
  }
}
```

### Comandos de Migración
```bash
dotnet ef migrations add NombreMigracion
dotnet ef database update
```

---

## 🛡️ 8. Seguridad y RBAC

El sistema utiliza **ASP.NET Core Identity**:
- **Roles:** `Admin`, `Gerente`, `Vendedor`, `Cajero`.
- **Policies:** Acceso restringido a configuración DGII solo para `Admin`.
- **Auditoría:** Los campos `UsuarioId` y `FechaCreacion` están presentes en todas las tablas transaccionales.

---

**© 2026 Facturapro Engineering Team**
