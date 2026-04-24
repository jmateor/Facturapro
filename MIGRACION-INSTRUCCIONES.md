# Instrucciones para Aplicar la Migración

## Problema
Las nuevas columnas `CodigoBarras` y `CodigoBarrasImagenUrl` fueron agregadas al modelo Producto, pero la base de datos SQL Server no tiene estas columnas, causando el error:

```
SqlException: El nombre de columna 'CodigoBarras' no es válido.
```

## Solución

Debes ejecutar el script SQL para agregar las columnas faltantes a la tabla `Productos`.

---

## Opción 1: Usar SQL Server Management Studio (SSMS) - RECOMENDADO

1. Abre **SQL Server Management Studio (SSMS)**
2. Conecta a tu servidor SQL Server
3. Expande **Bases de datos** → selecciona tu base de datos (ej: `FacturaproDb`)
4. Haz clic derecho en la base de datos → **Nueva Consulta**
5. Copia y pega el siguiente SQL:

```sql
-- Verificar si las columnas no existen antes de agregarlas
IF NOT EXISTS (SELECT * FROM sys.columns
               WHERE Name = 'CodigoBarras'
               AND Object_ID = Object_ID(N'Productos'))
BEGIN
    ALTER TABLE Productos ADD CodigoBarras NVARCHAR(50) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns
               WHERE Name = 'CodigoBarrasImagenUrl'
               AND Object_ID = Object_ID(N'Productos'))
BEGIN
    ALTER TABLE Productos ADD CodigoBarrasImagenUrl NVARCHAR(500) NULL;
END

-- Generar códigos de barra para productos existentes que no tengan uno
UPDATE Productos
SET CodigoBarras = Codigo + '-' + RIGHT('000000' + CAST(Id AS VARCHAR), 6)
WHERE CodigoBarras IS NULL OR CodigoBarras = '';

PRINT 'Migración completada exitosamente.';
```

6. Presiona **F5** o haz clic en **Ejecutar**

---

## Opción 2: Usar sqlcmd desde la línea de comandos

1. Abre **Command Prompt** o **PowerShell**
2. Ejecuta el siguiente comando (ajusta los parámetros según tu configuración):

```bash
sqlcmd -S localhost -d FacturaproDb -U sa -P tu_password -i "Migrations\AgregarCodigoBarras_20260420.sql"
```

O usa Windows Authentication:

```bash
sqlcmd -S localhost -d FacturaproDb -E -i "Migrations\AgregarCodigoBarras_20260420.sql"
```

Parámetros:
- `-S localhost` → Nombre del servidor SQL
- `-d FacturaproDb` → Nombre de la base de datos
- `-U sa` → Usuario
- `-P tu_password` → Contraseña
- `-E` → Usar Windows Authentication (en lugar de usuario/contraseña)

---

## Opción 3: Usar el archivo batch (aplicar-migracion.bat)

1. Abre el archivo `aplicar-migracion.bat` en un editor de texto
2. Modifica estas líneas con tus credenciales:

```batch
set SERVER=localhost
set DATABASE=FacturaproDb
set USER=sa
set PASSWORD=tu_password
```

3. Guarda el archivo
4. Haz doble clic en `aplicar-migracion.bat`

---

## Opción 4: Borrar y recrear la base de datos (SOLO para desarrollo)

⚠️ **ADVERTENCIA**: Esto eliminará TODOS los datos existentes. Úsalo solo en desarrollo.

1. Detén la aplicación
2. Borra el archivo de la base de datos (si es LocalDB) o ejecuta:

```sql
DROP DATABASE FacturaproDb;
```

3. Al reiniciar la aplicación con `EnsureCreated()`, EF creará la base de datos con todas las columnas nuevas.

---

## Verificación

Después de aplicar la migración, verifica que las columnas existen ejecutando:

```sql
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Productos'
AND COLUMN_NAME IN ('CodigoBarras', 'CodigoBarrasImagenUrl');
```

Debe retornar 2 filas con los nombres de las columnas.

---

## Archivos relacionados

- `Migrations/AgregarCodigoBarras_20260420.sql` - Script SQL de la migración
- `aplicar-migracion.bat` - Script batch para aplicar la migración
- `Data/ApplicationDbContext.cs` - Configuración de EF con las nuevas columnas

---

## Notas

- Las columnas nuevas son **nullable** (NULL permitido) para no afectar registros existentes
- El script genera automáticamente códigos de barra para productos existentes
- El formato del código de barra es: `{Codigo}-{Id:000000}` (ej: `PROD001-000042`)
