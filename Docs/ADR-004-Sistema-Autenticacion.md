# ADR-004: Sistema de Autenticación y Autorización

## Status
**Aceptado** - Implementado el 21 de abril de 2026

## Contexto

El sistema Facturapro maneja información sensible de facturación electrónica, datos de clientes y finanzas. Era necesario implementar un sistema de autenticación robusto que permitiera:

- Controlar el acceso al sistema
- Diferenciar permisos por roles (Admin, Vendedor, Cajero, Gerente)
- Mantener registro de actividad de usuarios
- Permitir a cada usuario gestionar su perfil y contraseña

## Decisión

Se implementó un sistema de autenticación completo utilizando **ASP.NET Core Identity** con las siguientes características:

### Arquitectura de Autenticación

```
┌─────────────────────────────────────────────────────────────┐
│                  SISTEMA DE AUTENTICACIÓN                    │
└─────────────────────────────────────────────────────────────┘

  ┌──────────────┐      ┌──────────────┐      ┌──────────────┐
  │   Usuario    │──────▶│   Login      │──────▶│   Cookie     │
  │              │      │   (Identity) │      │   Auth       │
  └──────────────┘      └──────────────┘      └──────┬───────┘
                                                    │
                         ┌──────────────────────────┘
                         ▼
              ┌──────────────────────┐
              │  AccountController  │
              │  - Login/Logout      │
              │  - Register          │
              │  - Profile           │
              │  - ChangePassword    │
              └──────────────────────┘
```

### Modelo de Usuario Personalizado

Se creó `ApplicationUser` extendiendo `IdentityUser` para incluir campos específicos del negocio:

```csharp
public class ApplicationUser : IdentityUser
{
    public string Nombre { get; set; } = string.Empty;
    public string? Apellido { get; set; }
    public string Rol { get; set; } = "Vendedor";
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public DateTime? UltimoAcceso { get; set; }
}
```

### Roles del Sistema

| Rol | Descripción | Permisos |
|-----|-------------|----------|
| **Admin** | Administrador del sistema | Acceso total |
| **Gerente** | Gerente de operaciones | Reportes, configuración, anulaciones |
| **Vendedor** | Personal de ventas | POS, facturas, clientes, productos (lectura) |
| **Cajero** | Personal de caja | POS, facturas, consultas |

### Configuración de Seguridad

**Política de Contraseñas:**
```csharp
options.Password.RequireDigit = true;
options.Password.RequireLowercase = true;
options.Password.RequireUppercase = true;
options.Password.RequireNonAlphanumeric = false;
options.Password.RequiredLength = 8;
```

**Bloqueo de Cuenta:**
```csharp
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.AllowedForNewUsers = true;
```

**Cookies de Autenticación:**
```csharp
options.LoginPath = "/Account/Login";
options.LogoutPath = "/Account/Logout";
options.AccessDeniedPath = "/Account/AccessDenied";
options.ExpireTimeSpan = TimeSpan.FromHours(2);
```

## Componentes Implementados

### Controlador: AccountController

| Acción | Descripción | Autorización |
|--------|-------------|--------------|
| `Login` | Vista y proceso de inicio de sesión | AllowAnonymous |
| `Register` | Vista y proceso de registro | AllowAnonymous |
| `Logout` | Cierra sesión del usuario | Requiere autenticación |
| `Profile` | Muestra información del perfil | Requiere autenticación |
| `ChangePassword` | Cambia contraseña del usuario | Requiere autenticación |
| `AccessDenied` | Vista de acceso denegado | - |

### ViewModels

| ViewModel | Propósito |
|-----------|-----------|
| `LoginViewModel` | Email, Password, RememberMe |
| `RegisterViewModel` | Nombre, Apellido, Email, Rol, Password, ConfirmPassword |
| `ProfileViewModel` | Información del perfil de usuario |
| `ChangePasswordViewModel` | Cambio de contraseña |

### Vistas

| Vista | Función |
|-------|---------|
| `Login.cshtml` | Formulario de inicio de sesión con diseño moderno |
| `Register.cshtml` | Formulario de registro con selección de rol |
| `Profile.cshtml` | Información del usuario y cambio de contraseña |
| `AccessDenied.cshtml` | Página de error 403 personalizada |

### Actualización del Layout

El menú de usuario en `_Layout.cshtml` ahora muestra:

- **Usuario autenticado:**
  - Avatar con inicial del email
  - Nombre del usuario (parte antes del @)
  - Dropdown: "Mi Perfil", "Cerrar Sesión"

- **Usuario no autenticado:**
  - Botón "Iniciar Sesión"
  - Botón "Registrarse"

## Migraciones y Seed

### Migración IdentityTables

Crea las tablas estándar de ASP.NET Core Identity:
- `AspNetUsers` (extendida con campos personalizados)
- `AspNetRoles`
- `AspNetUserRoles`
- `AspNetUserClaims`
- `AspNetUserLogins`
- `AspNetUserTokens`
- `AspNetRoleClaims`

### Seed de Datos Inicial

Al iniciar la aplicación se crean automáticamente:

1. **Roles:** Admin, Vendedor, Cajero, Gerente
2. **Usuario Administrador:**
   - Email: `admin@facturapro.com`
   - Contraseña: `Admin123!`
   - Rol: Admin

```csharp
// En Program.cs
await roleManager.CreateAsync(new IdentityRole("Admin"));
await userManager.CreateAsync(adminUser, "Admin123!");
await userManager.AddToRoleAsync(adminUser, "Admin");
```

## Integración con Sistema Existente

### Cambios en DbContext

```csharp
// Antes
public class ApplicationDbContext : DbContext

// Después
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
```

### Middleware Configurado

```csharp
app.UseAuthentication();  // Antes de UseAuthorization
app.UseAuthorization();
```

## Consecuencias

### Positivas

1. **Seguridad Estándar:** Utiliza ASP.NET Core Identity (probadas, seguras)
2. **Gestión Completa:** Login, logout, registro, recuperación, roles
3. **Extensible:** ApplicationUser permite campos personalizados
4. **Auditoría:** Registro de último acceso y fechas de creación
5. **Bloqueo:** Protección contra ataques de fuerza bruta

### Negativas

1. **Complejidad Adicional:** Más tablas en la base de datos (8 tablas Identity)
2. **Dependencia:** Acoplamiento con ASP.NET Core Identity
3. **Migraciones:** Requiere ejecutar migraciones en producción

## Archivos Creados

| Archivo | Descripción |
|---------|-------------|
| `Models/ApplicationUser.cs` | Modelo de usuario extendido |
| `Controllers/AccountController.cs` | Controlador de autenticación |
| `Views/Account/Login.cshtml` | Vista de login |
| `Views/Account/Register.cshtml` | Vista de registro |
| `Views/Account/Profile.cshtml` | Vista de perfil |
| `Views/Account/AccessDenied.cshtml` | Vista de acceso denegado |
| `Migrations/*IdentityTables*` | Migración para tablas de Identity |

## Archivos Modificados

| Archivo | Cambios |
|---------|---------|
| `Program.cs` | Configuración Identity, servicios, seed de admin |
| `Data/ApplicationDbContext.cs` | Herencia de IdentityDbContext |
| `Views/Shared/_Layout.cshtml` | Menú de usuario dinámico |
| `wwwroot/css/components.css` | Estilos para auth-links y dropdown |

## Próximos Pasos Sugeridos

1. **Recuperar Contraseña:** Implementar flujo de recuperación por email
2. **Autenticación de Dos Factores:** Agregar 2FA con autenticador
3. **Permisos Granulares:** Claims para permisos específicos por módulo
4. **Sesiones Múltiples:** Permitir control de sesiones activas
5. **Login Externo:** Integrar Google, Microsoft OAuth

## Referencias

- ADR-003: Módulo Punto de Venta (POS)
- [ASP.NET Core Identity Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [IdentityDbContext](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.entityframeworkcore.identitydbcontext)
