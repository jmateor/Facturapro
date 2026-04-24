# ADR-001: Estado Actual del Proyecto - Módulo de Categorías

## Status
**Aceptado** - Implementado el 20 de abril de 2026

## Contexto

El proyecto Facturapro es un sistema de facturación electrónica desarrollado en ASP.NET Core 8.0, orientado al cumplimiento de los requisitos de la DGII (Dirección General de Impuestos Internos de República Dominicana).

Se requería implementar un módulo de categorías para organizar los productos de manera más estructurada, reemplazando el campo `Categoria` de tipo string en la entidad Producto por una relación formal con una entidad Categoria independiente.

## Decisión

Se decidió implementar una arquitectura de relación **uno-a-muchos** entre Categoria y Producto, donde:

- Una **Categoria** puede tener múltiples **Productos**
- Un **Producto** pertenece a cero o una **Categoria** (relación opcional)

### Estructura Implementada

#### Entidad Categoria
```csharp
public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } // Único, requerido
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public ICollection<Producto> Productos { get; set; }
}
```

#### Entidad Producto (Modificada)
```csharp
public class Producto
{
    public int Id { get; set; }
    public string Codigo { get; set; } // Único
    public string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public int? CategoriaId { get; set; } // FK opcional
    public Categoria? Categoria { get; set; } // Navigation property
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}
```

## Consecuencias

### Positivas

1. **Integridad de Datos**: Al usar una tabla independiente, se evitan errores de tipeo y se garantiza la consistencia de los nombres de categorías.

2. **Escalabilidad**: Facilita agregar propiedades adicionales a las categorías (iconos, colores, jerarquías) sin modificar la estructura de productos.

3. **Mantenibilidad**: Las categorías se gestionan desde su propio módulo CRUD completo, independiente de los productos.

4. **Consultas Eficientes**: Las búsquedas y filtros por categoría son más rápidas al usar claves foráneas indexadas.

5. **Relación Flexible**: Al ser opcional (`CategoriaId` nullable), los productos pueden existir sin categoría asignada.

### Negativas

1. **Complejidad Adicional**: Se requiere mantener un controlador y vistas adicionales para el módulo de categorías.

2. **Migración Necesaria**: Se debe ejecutar una migración de Entity Framework para actualizar el esquema de la base de datos.

3. **Cascada de Cambios**: Los productos existentes con categoría como string necesitan ser migrados manualmente a la nueva estructura.

## Implementación

### Archivos Creados

| Ruta | Descripción |
|------|-------------|
| `Models/Entities/Categoria.cs` | Entidad Categoria |
| `Controllers/CategoriasController.cs` | Controlador CRUD |
| `Views/Categorias/Index.cshtml` | Lista de categorías |
| `Views/Categorias/Create.cshtml` | Crear categoría |
| `Views/Categorias/Edit.cshtml` | Editar categoría |
| `Views/Categorias/Details.cshtml` | Detalles con productos |
| `Views/Categorias/Delete.cshtml` | Eliminar categoría |
| `Views/Productos/Details.cshtml` | Detalles del producto |
| `Views/Productos/Delete.cshtml` | Eliminar producto |

### Archivos Modificados

| Ruta | Cambios |
|------|---------|
| `Models/Entities/Producto.cs` | Reemplazado `Categoria` string por `CategoriaId` int? FK |
| `Data/ApplicationDbContext.cs` | Agregado DbSet<Categoria> y configuración de relación |
| `Controllers/ProductosController.cs` | Actualizado para usar CategoriaId, validaciones de código duplicado |
| `Views/Productos/Index.cshtml` | Filtro por CategoriaId (dropdown), acciones adicionales |
| `Views/Productos/Create.cshtml` | Dropdown de categorías, validaciones |
| `Views/Productos/Edit.cshtml` | Dropdown de categorías |
| `Views/Shared/_Layout.cshtml` | Agregado enlace al módulo Categorías en el sidebar |
| `Views/_ViewImports.cshtml` | Agregado `using Facturapro.Models.Entities` |

### Configuración de Base de Datos

```csharp
// Relación uno-a-muchos con comportamiento SetNull
modelBuilder.Entity<Producto>(entity =>
{
    entity.HasOne(e => e.Categoria)
          .WithMany(c => c.Productos)
          .HasForeignKey(e => e.CategoriaId)
          .OnDelete(DeleteBehavior.SetNull);
});

// Índice único en nombre de categoría
modelBuilder.Entity<Categoria>(entity =>
{
    entity.HasIndex(e => e.Nombre).IsUnique();
});
```

## Próximos Pasos

1. **Ejecutar Migración**:
   ```bash
   dotnet ef migrations add AddCategoriaTable
   dotnet ef database update
   ```

2. **Migrar Datos Existentes**: Crear script SQL para convertir categorías string existentes a registros en la tabla Categorias.

3. **Implementar Jerarquías**: Considerar agregar `CategoriaPadreId` para soportar subcategorías en el futuro.

4. **Tests**: Agregar pruebas unitarias para los controladores de Categorias y Productos.

## Referencias

- [ADR-0000](https://adr.github.io/) - Plantilla ADR
- [Entity Framework Core Relationships](https://docs.microsoft.com/en-us/ef/core/modeling/relationships)
- [ASP.NET Core MVC](https://docs.microsoft.com/en-us/aspnet/core/mvc/overview)
