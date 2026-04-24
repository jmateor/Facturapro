@echo off
chcp 65001 >nul
echo ==========================================
echo   APLICAR MIGRACIÓN - FACTURAPRO
echo ==========================================
echo.
echo Este script agregará las columnas de código de barras
echo a la base de datos SQL Server.
echo.
echo Asegúrate de tener SQL Server Management Studio (SSMS)
echo o sqlcmd instalado, y modifica la cadena de conexión.
echo.
pause

REM Configura aquí tu cadena de conexión a SQL Server
set SERVER=localhost
set DATABASE=FacturaproDb
set USER=sa
set PASSWORD=tu_password

echo.
echo Ejecutando migración SQL...
echo.

sqlcmd -S %SERVER% -d %DATABASE% -U %USER% -P %PASSWORD% -i "Migrations\AgregarCodigoBarras_20260420.sql"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ==========================================
    echo   MIGRACIÓN COMPLETADA EXITOSAMENTE
    echo ==========================================
    echo.
    echo Las columnas CodigoBarras y CodigoBarrasImagenUrl
    echo han sido agregadas a la tabla Productos.
    echo.
) else (
    echo.
    echo ==========================================
    echo   ERROR AL EJECUTAR LA MIGRACIÓN
    echo ==========================================
    echo.
    echo Verifica:
    echo 1. Que SQL Server esté corriendo
    echo 2. Que las credenciales sean correctas
    echo 3. Que la base de datos exista
    echo.
    echo También puedes ejecutar el script manualmente:
    echo   Migrations\AgregarCodigoBarras_20260420.sql
    echo.
)

pause
