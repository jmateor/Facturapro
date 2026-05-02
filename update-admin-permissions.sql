-- Actualizar todos los permisos del usuario admin josemateo3148@gmail.com
UPDATE AspNetUsers
SET
    PuedeFacturar = 1,
    PuedeVerReportes = 1,
    PuedeGestionarInventario = 1,
    PuedeConfigurarSistema = 1,
    PuedeAnularFacturas = 1,
    PuedeVerCostos = 1,
    PuedeGestionarClientes = 1,
    PuedeGestionarUsuarios = 1
WHERE Email = 'josemateo3148@gmail.com';

-- Verificar el resultado
SELECT Email, Nombre, Rol,
       PuedeFacturar, PuedeVerReportes, PuedeGestionarInventario,
       PuedeConfigurarSistema, PuedeAnularFacturas, PuedeVerCostos,
       PuedeGestionarClientes, PuedeGestionarUsuarios
FROM AspNetUsers
WHERE Email = 'josemateo3148@gmail.com';
