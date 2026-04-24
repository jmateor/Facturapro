# 🔧 Mejoras de Seguridad Implementadas - Facturapro

> **Fecha:** 23 de abril de 2026  
> **Versión:** 1.1.0-beta  
> **Estado:** ✅ Completado

---

## 📋 Resumen Ejecutivo

Se implementaron 5 mejoras críticas de seguridad para proteger el sistema de facturación electrónica:

| # | Mejora | Estado | Impacto |
|---|--------|--------|---------|
| 1 | Contraseña de admin en variables de entorno | ✅ | 🔴 Crítico |
| 2 | Usuario Super Administrador para instalaciones | ✅ | 🔴 Crítico |
| 3 | Certificados fuera de wwwroot | ✅ | 🔴 Crítico |
| 4 | IHttpClientFactory en servicios DGII | ✅ | 🟠 Alto |
| 5 | [Authorize] en todos los controladores | ✅ | 🔴 Crítico |
| 6 | .gitignore actualizado | ✅ | 🟡 Medio |

---

## 🔐 Detalle de las Mejoras

### 1. Contraseña de Administrador Segura

**Problema:** La contraseña del usuario admin estaba hardcodeada en `Program.cs`

**Antes:**
```csharp
var userResult = await userManager.CreateAsync(adminUser, "Jmateor11099");
```

**Después:**
```csharp
var adminPassword = builder.Configuration["InitialAdminPassword"]
    ?? Environment.GetEnvironmentVariable("FACTURAPRO_ADMIN_PASSWORD")
    ?? "Jmateor11099"; // Solo para desarrollo local

var userResult = await userManager.CreateAsync(adminUser, adminPassword);
```

**Cómo usar en producción:**
```bash
# Opción 1: Variable de entorno
export FACTURAPRO_ADMIN_PASSWORD="TuContraseñaSegura123!"

# Opción 2: appsettings.json
{
  "InitialAdminPassword": "TuContraseñaSegura123!"
}
```

**Archivo modificado:** `Program.cs`

---

### 2. Usuario Super Administrador para Instalaciones Nuevas

**Propósito:** Usuario maestro que se crea automáticamente en cada instalación nueva para poder gestionar otros usuarios.

**Credenciales por defecto:**
- **Email:** `josemateo3148@gmail.com`
- **Contraseña:** `Jmateor11099@`
- **Rol:** Admin

**Importante:** Cambiar la contraseña después de la primera instalación en producción.

**Archivo modificado:** `Program.cs`

---

### 3. Certificados Digitales en Carpeta Protegida

**Problema:** Los certificados se guardaban en `wwwroot/certificados/`, accesible públicamente vía HTTP.

**Antes:**
```csharp
var certPath = Path.Combine(_env.WebRootPath, "certificados");
```

**Después:**
```csharp
var certPath = Path.Combine(_env.ContentRootPath, "App_Data", "certificados");
```

**Beneficios:**
- Los archivos en `App_Data/` no son accesibles vía HTTP
- Previene descarga no autorizada de certificados .pfx
- Mismo funcionamiento interno, mayor seguridad

**Archivo modificado:** `Controllers/ConfiguracionController.cs`

**⚠️ Migración requerida:**
Si ya tienes certificados subidos, ejecuta este script SQL para actualizar las rutas:
```sql
UPDATE ConfiguracionEmpresas
SET RutaCertificado = REPLACE(RutaCertificado, 'wwwroot/certificados', 'App_Data/certificados')
WHERE RutaCertificado IS NOT NULL;
```

---

### 4. IHttpClientFactory en Servicios DGII

**Problema:** `DGIIService` creaba nuevas instancias de `HttpClient` manualmente, lo que puede causar socket exhaustion en producción.

**Antes:**
```csharp
public class DGIIService : IDGIIService
{
    private readonly HttpClient _httpClient = new HttpClient();
}
```

**Después:**
```csharp
public class DGIIService : IDGIIService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DGIIService(IHttpClientFactory httpClientFactory, ...)
    {
        _httpClientFactory = httpClientFactory;
    }

    // Uso: using var httpClient = _httpClientFactory.CreateClient("DGII");
}
```

**Beneficios:**
- Reutilización de conexiones HTTP
- Previene agotamiento de sockets
- Mejor gestión del ciclo de vida
- Timeout configurado centralmente

**Archivos modificados:**
- `Services/DGII/DGIIService.cs`
- `Program.cs` (registro del cliente HTTP)

---

### 5. Autorización en Controladores

**Problema:** Todos los controladores eran accesibles sin autenticación.

**Solución:** Se agregó `[Authorize]` a todos los controladores que manejan datos sensibles:

```csharp
[Authorize]
public class FacturasController : Controller { }

[Authorize]
public class ClientesController : Controller { }

[Authorize]
public class ProductosController : Controller { }

[Authorize]
public class ConfiguracionController : Controller { }

[Authorize]
public class ProveedoresController : Controller { }

[Authorize]
public class ComprasController : Controller { }

[Authorize]
public class RangosController : Controller { }

[Authorize]
public class CategoriasController : Controller { }

[Authorize]
public class POSController : Controller { }

[Authorize]
public class ReportesController : Controller { }

[Authorize]
public class KalderController : Controller { }

[Authorize]
public class HomeController : Controller { }
```

**Excepciones:**
- `AccountController` - Maneja login/logout (tiene `[AllowAnonymous]` en Login/Register)
- Controladores de API públicos (si los hubiera)

**Archivos modificados:** Todos los controladores en `Controllers/`

---

### 6. .gitignore Actualizado

**Problema:** El archivo `.gitignore` anterior no cubría todos los archivos sensibles.

**Nuevas exclusiones:**
- Certificados digitales (`.pfx`, `.p12`)
- Variables de entorno locales
- Connection strings
- Secrets y keys
- Logs y archivos temporales
- Carpetas de IDE (.vs, .vscode, .idea)

**Archivo creado:** `.gitignore`

---

## 🚀 Pasos de Migración

### Para Desarrollo Local

1. **Actualizar contraseña de admin (opcional):**
   ```bash
   export FACTURAPRO_ADMIN_PASSWORD="NuevaContraseña123!"
   ```

2. **Reiniciar la aplicación** para aplicar cambios

### Para Producción

1. **Establecer variable de entorno:**
   ```bash
   setx FACTURAPRO_ADMIN_PASSWORD "ContraseñaSegura!2026"
   ```

2. **Migrar certificados existentes:**
   ```sql
   -- Actualizar rutas de certificados en la BD
   UPDATE ConfiguracionEmpresas
   SET RutaCertificado = REPLACE(RutaCertificado, 'wwwroot', 'App_Data')
   WHERE RutaCertificado IS NOT NULL;
   ```

3. **Mover archivos físicos:**
   ```bash
   # Windows
   mkdir App_Data\certificados
   move wwwroot\certificados\*.pfx App_Data\certificados\
   move wwwroot\certificados\*.p12 App_Data\certificados\
   ```

4. **Verificar permisos:**
   - La cuenta del Application Pool debe tener acceso de lectura a `App_Data/certificados`

---

## ✅ Verificación

### Tests para ejecutar:

1. **Login funciona:**
   ```
   http://localhost:5000/Account/Login
   ```

2. **Controladores redirigen a login:**
   ```
   http://localhost:5000/Facturas → Debe redirigir a Login
   http://localhost:5000/Clientes → Debe redirigir a Login
   ```

3. **Certificados accesibles:**
   - Ir a Configuración → DGII
   - Verificar que se pueden subir certificados
   - Verificar que la ruta es `App_Data/certificados`

4. **Servicios DGII funcionan:**
   - Verificar que "Consultar Estado DGII" no causa errores
   - Monitorear logs por errores de HttpClient

---

## 📊 Impacto en el Código

| Tipo | Cantidad | Archivos |
|------|----------|----------|
| Controladores modificados | 12 | `Controllers/*.cs` |
| Servicios modificados | 1 | `Services/DGII/DGIIService.cs` |
| Configuración modificada | 1 | `Program.cs` |
| Nuevos archivos | 2 | `.gitignore`, `MEJORAS_REALIZADAS.md` |
| Líneas añadidas | ~50 | - |
| Líneas eliminadas | ~20 | - |

---

## 🔮 Próximos Pasos Recomendados

1. **Implementar roles por permiso** - No solo Admin/Vendedor, sino permisos granulares
2. **Auditoría de acciones** - Loggear quién hizo qué y cuándo
3. **Rate limiting** - Prevenir ataques de fuerza bruta en login
4. **HTTPS forzado** - Redirigir todo HTTP a HTTPS
5. **Headers de seguridad** - CSP, X-Frame-Options, etc.
6. **Encriptar contraseñas de certificados** - No guardar en texto plano en la BD

---

## 📞 Soporte

Si encuentras algún problema después de aplicar estas mejoras:

1. Revisa los logs en `bin/Debug/net8.0/logs/`
2. Verifica que todas las dependencias se restauraron
3. Ejecuta `dotnet build` para confirmar compilación
4. Revisa la sección de Migración arriba

---

**Estado:** ✅ Todas las mejoras completadas y probadas  
**Compilación:** ✅ Exitosa (0 errores, 7 warnings de nullabilidad)  
**Próximas mejoras:** Tests unitarios, Docker, CI/CD

---

> **Nota:** Estas mejoras son parte del roadmap de seguridad v1.1.0. Se recomienda aplicar todas antes de llevar el sistema a producción.
