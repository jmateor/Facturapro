using Facturapro.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Facturapro.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // Helper para obtener el usuario actual
        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            return await _userManager.GetUserAsync(User);
        }

        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("Usuario {Email} inició sesión", model.Email);

                // Actualizar último acceso
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    user.UltimoAcceso = DateTime.Now;
                    await _userManager.UpdateAsync(user);
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Usuario {Email} bloqueado por intentos fallidos", model.Email);
                ModelState.AddModelError(string.Empty, "La cuenta está bloqueada. Intente más tarde.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
            return View(model);
        }

        // GET: /Account/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Rol = model.Rol ?? "Vendedor",
                Activo = true,
                FechaRegistro = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("Usuario {Email} registrado exitosamente", model.Email);
                await _userManager.AddToRoleAsync(user, user.Rol);
                TempData["SuccessMessage"] = "Usuario registrado exitosamente.";
                return Ok();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return BadRequest(model);
        }

        // POST: /Account/EditarUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditarUsuario(string id, string nombre, string apellido, string rol)
        {
            // Verificar que el usuario actual es administrador
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null || currentUser.Rol != "Admin")
            {
                return Forbid();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return BadRequest(new { message = "Usuario no encontrado" });
            }

            user.Nombre = nombre;
            user.Apellido = apellido;
            user.Rol = rol;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation("Usuario {Email} actualizado exitosamente", user.Email);
                TempData["SuccessMessage"] = "Usuario actualizado exitosamente.";
                return Ok();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return BadRequest(new { message = "Error al actualizar el usuario" });
        }

        // POST: /Account/CambiarContrasena
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CambiarContrasena(string id, string nuevaContrasena)
        {
            // Verificar que el usuario actual es administrador
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null || currentUser.Rol != "Admin")
            {
                return Forbid();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return BadRequest(new { message = "Usuario no encontrado" });
            }

            // Generar token para resetear contraseña
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, nuevaContrasena);

            if (result.Succeeded)
            {
                _logger.LogInformation("Contraseña de {Email} cambiada exitosamente", user.Email);
                return Ok(new { message = "Contraseña cambiada exitosamente" });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return BadRequest(new { message = "Error al cambiar la contraseña", errores = result.Errors.Select(e => e.Description) });
        }

        // POST: /Account/EliminarUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EliminarUsuario(string id)
        {
            // Verificar que el usuario actual es administrador
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null || currentUser.Rol != "Admin")
            {
                return Forbid();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return BadRequest(new { message = "Usuario no encontrado" });
            }

            // No permitir eliminar el último administrador
            if (user.Rol == "Admin")
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count <= 1)
                {
                    return BadRequest(new { message = "No se puede eliminar el último administrador" });
                }
            }

            // No permitir auto-eliminación
            if (user.Id == currentUser.Id)
            {
                return BadRequest(new { message = "No puedes eliminarte a ti mismo" });
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation("Usuario {Email} eliminado exitosamente", user.Email);
                TempData["SuccessMessage"] = "Usuario eliminado exitosamente.";
                return Ok();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return BadRequest(new { message = "Error al eliminar el usuario" });
        }

        // GET: /Account/ObtenerUsuario
        [HttpGet]
        public async Task<IActionResult> ObtenerUsuario(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                id = user.Id,
                nombre = user.Nombre,
                apellido = user.Apellido,
                email = user.Email,
                rol = user.Rol
            });
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Usuario cerró sesión");
            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: /Account/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ProfileViewModel
            {
                Email = user.Email,
                Nombre = user.Nombre,
                Apellido = user.Apellido,
                Rol = user.Rol,
                FechaRegistro = user.FechaRegistro,
                UltimoAcceso = user.UltimoAcceso
            };

            return View(model);
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Profile));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Contraseña actualizada correctamente.";
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    TempData["ErrorMessage"] = error.Description;
                }
            }

            return RedirectToAction(nameof(Profile));
        }
    }

    // ViewModels
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Recordarme")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "El apellido no puede exceder 100 caracteres")]
        [Display(Name = "Apellido")]
        public string? Apellido { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, ErrorMessage = "La contraseña debe tener al menos {2} caracteres", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Rol")]
        public string? Rol { get; set; } = "Vendedor";
    }

    public class ProfileViewModel
    {
        public string? Email { get; set; }
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? Rol { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime? UltimoAcceso { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nueva contraseña")]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
