using Facturapro.Data;
using Facturapro.Models;
using Facturapro.Models.DGII;
using Facturapro.Models.Entities;
using Facturapro.Services;
using Facturapro.Services.DGII;
using Facturapro.Services.PDF;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Session for POS Cart
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Add Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString))
        options.UseSqlServer(connectionString);
    else
        options.UseInMemoryDatabase("FacturaproDb");
});

// Add ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Configuración de contraseñas
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;

    // Configuración de usuario
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    // Configuración de bloqueo
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // Configuración de confirmación
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configuración de cookies de autenticación
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Registrar HttpClientFactory para servicios DGII
builder.Services.AddHttpClient("DGII", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

// Registrar servicios de Negocio y Utilidades
builder.Services.AddScoped<IBarcodeService, BarcodeService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<Facturapro.Services.Notifications.IWhatsAppService, Facturapro.Services.Notifications.WhatsAppService>();
builder.Services.AddScoped<Facturapro.Services.Intelligence.IOcrService, Facturapro.Services.Intelligence.OcrService>();

// Configuración de API DGII
builder.Services.AddScoped<ConfiguracionAPIDGII>(provider =>
{
    var context = provider.GetRequiredService<ApplicationDbContext>();
    var config = context.ConfiguracionEmpresas.FirstOrDefault();

    return new ConfiguracionAPIDGII
    {
        Ambiente = config?.ModoPruebas == true ? AmbienteDGII.Test : AmbienteDGII.Produccion,
        RncEmisor = config?.RNCEmisor ?? "000000000",
        RutaCertificadoP12 = config?.RutaCertificado ?? string.Empty,
        PasswordCertificado = config?.PasswordCertificado ?? string.Empty
    };
});

// Registrar servicios de API DGII y Fiscal
builder.Services.AddScoped<RangoNumeracionService>();
builder.Services.AddScoped<IDGIIService, DGIIService>();
builder.Services.AddScoped<IRncValidationService, RncValidationService>();
builder.Services.AddScoped<Facturapro.Services.DGII.IFiscalService, Facturapro.Services.DGII.FiscalService>();
builder.Services.AddScoped<IFacturacionElectronicaAPIService, FacturacionElectronicaAPIService>();

// Registrar servicios Externos (Moneda/HttpClient)
builder.Services.AddHttpClient<Facturapro.Services.Currency.IExchangeRateService, Facturapro.Services.Currency.ExchangeRateService>();
builder.Services.AddScoped<Facturapro.Services.Currency.IExchangeRateService, Facturapro.Services.Currency.ExchangeRateService>();

// Servicio en background para consultar estados DGII
builder.Services.AddHostedService<DGIIBackgroundService>();

// Registrar servicio de generación de PDFs
QuestPDF.Settings.License = LicenseType.Community;
builder.Services.AddScoped<IPdfService, PdfService>();

builder.Services.AddScoped<FacturacionElectronicaService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<FacturacionElectronicaService>>();
    var context = provider.GetRequiredService<ApplicationDbContext>();

    // Obtener configuración de empresa
    var config = context.ConfiguracionEmpresas.FirstOrDefault()
        ?? new ConfiguracionEmpresa
        {
            RNCEmisor = "000000000",
            RazonSocialEmisor = "SIN CONFIGURAR",
            DireccionEmisor = "SIN CONFIGURAR"
        };

    return new FacturacionElectronicaService(logger, config);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

// Configurar Localización Global (es-DO)
var supportedCultures = new[] { "es-DO", "es-ES", "en-US" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

// Crear base de datos automáticamente si no existe (solo desarrollo)
// y seed de usuario admin inicial
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        context.Database.EnsureCreated();

        // Verificar si las tablas de Identity existen
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetRoles'";
        var result = await command.ExecuteScalarAsync();
        await connection.CloseAsync();

        if (Convert.ToInt32(result) > 0)
        {
            // Seed roles
            string[] roles = { "Admin", "Vendedor", "Cajero", "Gerente" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed admin user
            var adminEmail = builder.Configuration["InitialSetup:AdminEmail"] ?? "admin@facturapro.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    Nombre = "Administrador",
                    Apellido = "Sistema",
                    Rol = "Admin",
                    Activo = true,
                    FechaRegistro = DateTime.Now,
                    EmailConfirmed = true
                };

                var adminPassword = builder.Configuration["InitialSetup:AdminPassword"] ?? "Admin123!";
                var userResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (userResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed super administrador
            var superAdminEmail = builder.Configuration["InitialSetup:SuperAdminEmail"] ?? "superadmin@facturapro.com";
            var superAdminUser = await userManager.FindByEmailAsync(superAdminEmail);
            if (superAdminUser == null)
            {
                superAdminUser = new ApplicationUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    Nombre = "Super",
                    Apellido = "Administrador",
                    Rol = "Admin",
                    Activo = true,
                    FechaRegistro = DateTime.Now,
                    EmailConfirmed = true,
                    PuedeFacturar = true,
                    PuedeVerReportes = true,
                    PuedeGestionarInventario = true,
                    PuedeConfigurarSistema = true,
                    PuedeAnularFacturas = true,
                    PuedeVerCostos = true,
                    PuedeGestionarClientes = true,
                    PuedeGestionarUsuarios = true
                };

                var superAdminPassword = builder.Configuration["InitialSetup:SuperAdminPassword"] ?? "SuperAdmin123!";
                var superAdminResult = await userManager.CreateAsync(superAdminUser, superAdminPassword);
                if (superAdminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdminUser, "Admin");
                }
            }
            else
            {
                // Actualizar permisos del super admin si ya existe
                bool needsUpdate = !superAdminUser.PuedeFacturar || !superAdminUser.PuedeVerReportes ||
                                   !superAdminUser.PuedeGestionarInventario || !superAdminUser.PuedeConfigurarSistema ||
                                   !superAdminUser.PuedeAnularFacturas || !superAdminUser.PuedeVerCostos ||
                                   !superAdminUser.PuedeGestionarClientes || !superAdminUser.PuedeGestionarUsuarios;

                if (needsUpdate)
                {
                    superAdminUser.PuedeFacturar = true;
                    superAdminUser.PuedeVerReportes = true;
                    superAdminUser.PuedeGestionarInventario = true;
                    superAdminUser.PuedeConfigurarSistema = true;
                    superAdminUser.PuedeAnularFacturas = true;
                    superAdminUser.PuedeVerCostos = true;
                    superAdminUser.PuedeGestionarClientes = true;
                    superAdminUser.PuedeGestionarUsuarios = true;

                    await userManager.UpdateAsync(superAdminUser);
                    Console.WriteLine("Permisos del super admin actualizados correctamente.");
                }
            }
        }
        else
        {
            Console.WriteLine("ADVERTENCIA: Las tablas de Identity no existen. Ejecute 'dotnet ef database update' o el script SQL create_identity_tables.sql");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"ADVERTENCIA: Error al inicializar la base de datos: {ex.Message}");
    Console.WriteLine("La aplicación continuará pero puede que algunas funciones no estén disponibles.");
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
