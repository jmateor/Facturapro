-- Script para agregar columnas extendidas a la tabla Productos
-- Ejecutar en SQL Server Management Studio o Azure Data Studio

USE [Facturapro]; -- Cambiar al nombre correcto de tu base de datos
GO

-- Verificar si las columnas ya existen antes de agregarlas
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[Productos]') AND name = 'PrecioCompra')
BEGIN
    ALTER TABLE [dbo].[Productos] ADD [PrecioCompra] decimal(18,2) NOT NULL DEFAULT 0;
    PRINT 'Columna PrecioCompra agregada';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[Productos]') AND name = 'TipoProducto')
BEGIN
    ALTER TABLE [dbo].[Productos] ADD [TipoProducto] int NOT NULL DEFAULT 1;
    PRINT 'Columna TipoProducto agregada';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[Productos]') AND name = 'StockMinimo')
BEGIN
    ALTER TABLE [dbo].[Productos] ADD [StockMinimo] int NOT NULL DEFAULT 0;
    PRINT 'Columna StockMinimo agregada';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[Productos]') AND name = 'Ubicacion')
BEGIN
    ALTER TABLE [dbo].[Productos] ADD [Ubicacion] nvarchar(100) NULL;
    PRINT 'Columna Ubicacion agregada';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[Productos]') AND name = 'ProveedorId')
BEGIN
    ALTER TABLE [dbo].[Productos] ADD [ProveedorId] int NULL;
    PRINT 'Columna ProveedorId agregada';

    -- Crear índice para ProveedorId
    CREATE INDEX [IX_Productos_ProveedorId] ON [dbo].[Productos]([ProveedorId]);
    PRINT 'Índice IX_Productos_ProveedorId creado';

    -- Agregar foreign key
    ALTER TABLE [dbo].[Productos]
    ADD CONSTRAINT [FK_Productos_Proveedores_ProveedorId]
    FOREIGN KEY ([ProveedorId]) REFERENCES [dbo].[Proveedores]([Id])
    ON DELETE SET NULL;
    PRINT 'Foreign key FK_Productos_Proveedores_ProveedorId creada';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[Productos]') AND name = 'ControlaStock')
BEGIN
    ALTER TABLE [dbo].[Productos] ADD [ControlaStock] bit NOT NULL DEFAULT 1;
    PRINT 'Columna ControlaStock agregada';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[Productos]') AND name = 'FechaVencimiento')
BEGIN
    ALTER TABLE [dbo].[Productos] ADD [FechaVencimiento] datetime2 NULL;
    PRINT 'Columna FechaVencimiento agregada';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[Productos]') AND name = 'NumeroLote')
BEGIN
    ALTER TABLE [dbo].[Productos] ADD [NumeroLote] nvarchar(50) NULL;
    PRINT 'Columna NumeroLote agregada';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[Productos]') AND name = 'PesoPorUnidad')
BEGIN
    ALTER TABLE [dbo].[Productos] ADD [PesoPorUnidad] decimal(18,2) NULL;
    PRINT 'Columna PesoPorUnidad agregada';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[Productos]') AND name = 'UnidadMedida')
BEGIN
    ALTER TABLE [dbo].[Productos] ADD [UnidadMedida] nvarchar(20) NOT NULL DEFAULT N'Unidad';
    PRINT 'Columna UnidadMedida agregada';
END
GO

-- Registrar la migración en la tabla de historial
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260427000002_AddProductoExtended')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260427000002_AddProductoExtended', N'8.0.0');
    PRINT 'Migración registrada en __EFMigrationsHistory';
END
GO

PRINT 'Script completado exitosamente';
GO
