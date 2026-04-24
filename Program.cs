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
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Registrar HttpClientFactory para servicios DGII
builder.Services.AddHttpClient("DGII", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

// Registrar servicios de DGII
builder.Services.AddScoped<RangoNumeracionService>();

// Registrar servicio de códigos de barra
builder.Services.AddScoped<IBarcodeService, BarcodeService>();

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

// Registrar servicios de API DGII
builder.Services.AddScoped<IDGIIService, DGIIService>();
builder.Services.AddScoped<IFacturacionElectronicaAPIService, FacturacionElectronicaAPIService>();

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
            var adminEmail = "jmateor@gmail.com";
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

                // Contraseña desde variable de entorno o appsettings para producción
                var adminPassword = builder.Configuration["InitialAdminPassword"]
                    ?? Environment.GetEnvironmentVariable("FACTURAPRO_ADMIN_PASSWORD")
                    ?? "Jmateor11099"; // Solo para desarrollo local

                var userResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (userResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed super administrador - Usuario maestro para instalaciones del sistema
            var superAdminEmail = "josemateo3148@gmail.com";
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
                    EmailConfirmed = true
                };

                // Contraseña fija para instalaciones nuevas - cambiar en producción
                var superAdminPassword = "Jmateor11099@";

                var superAdminResult = await userManager.CreateAsync(superAdminUser, superAdminPassword);
                if (superAdminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdminUser, "Admin");
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
