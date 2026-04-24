-- Script para agregar las nuevas columnas a ConfiguracionEmpresas
-- Ejecutar en la base de datos FacturaproDB

USE FacturaproDB;
GO

-- Verificar si las columnas ya existen antes de agregarlas
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ConfiguracionEmpresas') AND name = 'Multimoneda')
BEGIN
    ALTER TABLE ConfiguracionEmpresas ADD Multimoneda BIT NOT NULL DEFAULT 0;
    PRINT 'Columna Multimoneda agregada exitosamente';
END
ELSE
BEGIN
    PRINT 'La columna Multimoneda ya existe';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ConfiguracionEmpresas') AND name = 'UsarCaja')
BEGIN
    ALTER TABLE ConfiguracionEmpresas ADD UsarCaja BIT NOT NULL DEFAULT 1;
    PRINT 'Columna UsarCaja agregada exitosamente';
END
ELSE
BEGIN
    PRINT 'La columna UsarCaja ya existe';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ConfiguracionEmpresas') AND name = 'CuentaIngresosEfectivo')
BEGIN
    ALTER TABLE ConfiguracionEmpresas ADD CuentaIngresosEfectivo NVARCHAR(100) NULL;
    PRINT 'Columna CuentaIngresosEfectivo agregada exitosamente';
END
ELSE
BEGIN
    PRINT 'La columna CuentaIngresosEfectivo ya existe';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ConfiguracionEmpresas') AND name = 'CuentaSalidasEfectivo')
BEGIN
    ALTER TABLE ConfiguracionEmpresas ADD CuentaSalidasEfectivo NVARCHAR(100) NULL;
    PRINT 'Columna CuentaSalidasEfectivo agregada exitosamente';
END
ELSE
BEGIN
    PRINT 'La columna CuentaSalidasEfectivo ya existe';
END

PRINT 'Script completado exitosamente';
GO
