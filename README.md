# 🧾 Facturapro

> **Sistema de Facturación Electrónica para República Dominicana**  
> **Versión:** 1.0.0-beta | **Compatible DGII** ✅

![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)
![Entity Framework](https://img.shields.io/badge/EF%20Core-8.0-512BD4?style=flat-square)
![DGII](https://img.shields.io/badge/DGII-Comprobantes%20e--CF-green?style=flat-square)

---

## 📋 Descripción

Facturapro es un sistema completo de facturación electrónica diseñado para cumplir con los requisitos de la DGII (Dirección General de Impuestos Internos) de República Dominicana. Permite emitir comprobantes fiscales electrónicos (e-CF) válidos ante la autoridad tributaria.

### ✨ Características Principales

- 🏛️ **Facturación Electrónica DGII** - Envío directo a la API oficial
- 💰 **Punto de Venta (POS)** - Interfaz rápida con escáner de códigos de barras
- 📦 **Control de Inventario** - Entradas, salidas y ajustes
- 👥 **Gestión de Clientes** - Base de datos completa
- 📊 **Reportes** - Estadísticas de ventas e inventario
- 🔐 **Firma Digital** - Certificados PFX/P12 compatibles
- 📱 **Responsive** - Funciona en dispositivos móviles

---

## 🚀 Inicio Rápido

### Requisitos Previos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server) (o usar InMemory para desarrollo)
- Certificado Digital PFX/P12 (para producción)

### Instalación

```bash
# 1. Clonar repositorio
git clone https://github.com/tuusuario/facturapro.git
cd facturapro

# 2. Restaurar paquetes
dotnet restore

# 3. Configurar base de datos (en appsettings.json)
# Por defecto usa InMemory para desarrollo

# 4. Ejecutar aplicación
dotnet run

# 5. Abrir navegador en http://localhost:5000
```

### Configuración Inicial

1. Acceda a **Configuración** → **DGII**
2. Ingrese los datos de su empresa (RNC, Razón Social, etc.)
3. Suba su certificado digital (.PFX)
4. Configure rangos de numeración en **Rangos de Numeración**
5. ¡Listo para facturar!

---

## 📁 Estructura del Proyecto

```
Facturapro/
├── Controllers/           # Controladores MVC
│   ├── FacturasController.cs
│   ├── POSController.cs
│   ├── ClientesController.cs
│   ├── ProductosController.cs
│   ├── ConfiguracionController.cs
│   └── ...
├── Models/
│   ├── Entities/          # Entidades de dominio
│   │   ├── Factura.cs
│   │   ├── Cliente.cs
│   │   ├── Producto.cs
│   │   └── ...
│   └── DGII/              # Modelos específicos DGII
│       └── DGIIApiModels.cs
├── Services/
│   ├── DGII/              # Servicios de integración DGII
│   │   ├── DGIIService.cs
│   │   ├── FacturacionElectronicaAPIService.cs
│   │   └── DGIIBackgroundService.cs
│   └── FacturacionElectronicaService.cs
├── Data/
│   └── ApplicationDbContext.cs
├── Views/                 # Vistas Razor
│   ├── Facturas/
│   ├── POS/
│   ├── Clientes/
│   └── ...
├── wwwroot/              # Assets estáticos
│   ├── css/
│   ├── js/
│   └── certificados/     # Certificados digitales
└── Program.cs            # Punto de entrada
```

---

## 📚 Documentación

| Documento | Descripción |
|-----------|-------------|
| [📘 MANUAL_USUARIO.md](./MANUAL_USUARIO.md) | Guía completa para usuarios finales |
| [🏛️ DGII_IMPORTANTE.md](./DGII_IMPORTANTE.md) | Notas críticas sobre integración DGII |
| [📋 PROJECT_STATUS.md](./PROJECT_STATUS.md) | Estado actual del desarrollo |

---

## 🏛️ Facturación Electrónica DGII

### Ambientes de Trabajo

| Ambiente | URL Base | Propósito |
|----------|----------|-----------|
| **TesteCF** | `https://ecf.dgii.gov.do/testecf/` | Desarrollo y pruebas |
| **CerteCF** | `https://ecf.dgii.gov.do/certecf/` | Certificación formal |
| **Producción** | `https://ecf.dgii.gov.do/ecf/` | Operación real |

### Flujo de Facturación

```
Crear Factura → Firmar Digitalmente → Enviar a DGII → Obtener TrackId → Consultar Estado
```

### Tipos de Comprobantes Soportados

- **E31** - Factura de Crédito Fiscal
- **E32** - Factura de Consumo
- **E33** - Nota de Débito
- **E34** - Nota de Crédito
- **E41** - Comprobante de Compras
- **E43** - Gastos Menores
- **E44** - Regímenes Especiales
- **E45** - Gubernamental
- **E46** - Exportaciones
- **E47** - Pagos al Exterior

---

## 🛠️ Tecnologías Utilizadas

- **Backend:** ASP.NET Core 8.0 MVC
- **ORM:** Entity Framework Core 8.0
- **Base de Datos:** SQL Server / InMemory
- **Frontend:** Razor Views + CSS Moderno
- **Firma Digital:** System.Security.Cryptography
- **HTTP Client:** HttpClientFactory

---

## 📸 Screenshots

*(Próximamente)*

---

## 🗺️ Roadmap

### Completado ✅
- [x] CRUD de facturas electrónicas
- [x] Integración API DGII (envío real)
- [x] Firma digital con certificados
- [x] Punto de Venta (POS)
- [x] Control de inventario
- [x] Gestión de clientes y proveedores

### En Progreso 🚧
- [ ] Sistema de autenticación (Identity)
- [ ] Reportes avanzados
- [ ] Generación de PDF
- [ ] App móvil

### Pendiente 📋
- [ ] Multiempresa
- [ ] API REST para integraciones
- [ ] Dashboard analítico
- [ ] Contabilidad integrada

---

## 🤝 Contribuir

Las contribuciones son bienvenidas. Para cambios importantes:

1. Fork el proyecto
2. Cree una rama (`git checkout -b feature/nueva-funcionalidad`)
3. Commit sus cambios (`git commit -am 'Agregar nueva funcionalidad'`)
4. Push a la rama (`git push origin feature/nueva-funcionalidad`)
5. Abra un Pull Request

---

## 📝 Licencia

Este proyecto está licenciado bajo [MIT License](LICENSE).

---

## ⚠️ Descargo de Responsabilidad

Este software se proporciona "tal cual" sin garantías. El usuario es responsable de:

- Verificar el cumplimiento de requisitos legales vigentes
- Realizar pruebas exhaustivas antes de uso en producción
- Mantener copias de seguridad de sus datos
- Cumplir con las obligaciones fiscales correspondientes

---

## 📞 Contacto

- **Email:** soporte@facturapro.do
- **Website:** https://facturapro.do
- **Issues:** [GitHub Issues](https://github.com/tuusuario/facturapro/issues)

---

## 🙏 Agradecimientos

- DGII República Dominicana por la documentación técnica
- Comunidad .NET por las herramientas y librerías
- Contribuidores del proyecto

---

> **Nota:** Este es un proyecto en desarrollo activo. Algunas funcionalidades pueden cambiar.

**© 2026 Facturapro - Todos los derechos reservados**
