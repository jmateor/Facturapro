-- Migración: Agregar campos de código de barras a Productos
-- Fecha: 2026-04-20

-- Verificar si las columnas no existen antes de agregarlas
IF NOT EXISTS (SELECT * FROM sys.columns
               WHERE Name = 'CodigoBarras'
               AND Object_ID = Object_ID(N'Productos'))
BEGIN
    ALTER TABLE Productos ADD CodigoBarras NVARCHAR(50) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns
               WHERE Name = 'CodigoBarrasImagenUrl'
               AND Object_ID = Object_ID(N'Productos'))
BEGIN
    ALTER TABLE Productos ADD CodigoBarrasImagenUrl NVARCHAR(500) NULL;
END
GO

-- Generar códigos de barra para productos existentes que no tengan uno
UPDATE Productos
SET CodigoBarras = Codigo + '-' + RIGHT('000000' + CAST(Id AS VARCHAR), 6)
WHERE CodigoBarras IS NULL OR CodigoBarras = '';
GO

PRINT 'Migración completada: Campos de código de barras agregados exitosamente.';
