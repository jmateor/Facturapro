# ADR-003: Módulo Punto de Venta (POS)

## Status
**Aceptado** - Implementado el 20 de abril de 2026

## Contexto

El sistema necesitaba una interfaz rápida y sencilla para realizar ventas de manera ágil, similar a un punto de venta físico. El flujo de facturación existente requería muchos pasos para crear una factura simple.

## Decisión

Se implementó un módulo POS (Point of Sale) con las siguientes características:

### Características del POS

1. **Interfaz Dividida**
   - Panel izquierdo: Productos disponibles con búsqueda rápida
   - Panel derecho: Carrito de compras en tiempo real

2. **Flujo Rápido**
   - Click en producto para agregar al carrito
   - Ajuste de cantidades con +/-
   - Eliminación rápida de items
   - Cálculo automático de ITBIS (18%)

3. **Gestión de Carrito**
   - Almacenamiento en Session para persistencia
   - Validación de stock en tiempo real
   - Totales calculados automáticamente

4. **Integración con NCF**
   - Opción de seleccionar tipo de NCF (01, 02, 03, 14)
   - Opción "Sin NCF" para ventas no fiscales
   - Asignación automática de eNCF desde rangos activos

### Entidades y Flujo

```
┌─────────────────────────────────────────────────────────────┐
│                      FLUJO POS                               │
└─────────────────────────────────────────────────────────────┘

  Cliente selecciona productos
           ↓
    ┌──────────────┐
    │    Carrito   │ ← Almacenado en Session
    │   (Session)  │
    └──────┬───────┘
           ↓
  Click "Completar Venta"
           ↓
    ┌──────────────┐
    │    Factura   │ ← Creada en BD
    │   (Factura)  │
    └──────┬───────┘
           ↓
  ┌──────────────────┐
  │  Actualizaciones │
  │  - Stock         │
  │  - Movimientos   │
  │  - NCF (si aplica)│
  └──────────────────┘
```

## Estructura Implementada

### Controlador: POSController

| Acción | Descripción |
|--------|-------------|
| `Index` | Vista principal del POS |
| `AgregarAlCarrito` | Agrega producto al carrito (AJAX) |
| `ActualizarCantidad` | Modifica cantidad de item (AJAX) |
| `EliminarDelCarrito` | Elimina item del carrito (AJAX) |
| `LimpiarCarrito` | Vacía el carrito (AJAX) |
| `ProcesarVenta` | Crea la factura y actualiza stock |
| `Completada` | Vista de confirmación post-venta |

### Modelos

**CarritoItem** (Temporal en Session)
```csharp
public class CarritoItem
{
    public int ProductoId { get; set; }
    public string Codigo { get; set; }
    public string Nombre { get; set; }
    public decimal Precio { get; set; }
    public int Cantidad { get; set; }
    public decimal SubTotal => Precio * Cantidad;
    public decimal ITBIS => SubTotal * 0.18m;
    public decimal Total => SubTotal + ITBIS;
}
```

**POSVentaViewModel**
```csharp
public class POSVentaViewModel
{
    public int ClienteId { get; set; }
    public string? TipoNcf { get; set; }
    public string? Comentarios { get; set; }
}
```

### Vistas

| Vista | Función |
|-------|---------|
| `Index.cshtml` | Interfaz de venta con grid de productos y carrito |
| `Completada.cshtml` | Confirmación con resumen de la venta |

### Configuración de Session

Agregado a `Program.cs`:
```csharp
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

app.UseSession();
```

## Consecuencias

### Positivas

1. **Velocidad**: Las ventas se procesan en segundos
2. **Usabilidad**: Interfaz intuitiva tipo "touch"
3. **Flexibilidad**: Permite ventas con o sin NCF
4. **Integridad**: Actualiza stock y crea movimientos automáticamente
5. **Persistencia**: El carrito se mantiene durante la sesión

### Negativas

1. **Dependencia Session**: Si el servidor reinicia, se pierde el carrito
2. **Sin Offline**: Requiere conexión constante al servidor
3. **Complejidad**: Más código para mantener (Session + AJAX)

## Menú Actualizado

El menú ahora tiene una sección "Ventas" destacada:

```
⚡ Punto de Venta (POS)  ← Destacado con color especial
📄 Facturas
👥 Clientes
```

## Archivos Creados

| Archivo | Descripción |
|---------|-------------|
| `Controllers/POSController.cs` | Controlador del punto de venta |
| `Views/POS/Index.cshtml` | Interfaz principal del POS |
| `Views/POS/Completada.cshtml` | Vista de venta completada |

## Archivos Modificados

| Archivo | Cambios |
|---------|---------|
| `Program.cs` | Agregado servicio Session y middleware |
| `Views/Shared/_Layout.cshtml` | Menú reorganizado con sección Ventas |

## Próximos Pasos Sugeridos

1. **Impresión de Ticket**: Agregar vista para imprimir ticket térmico
2. **Descuentos**: Permitir descuentos porcentuales o fijos en el POS
3. **Múltiples Pagos**: Soportar pago mixto (efectivo + tarjeta)
4. **Código de Barras**: Integrar lector de códigos de barras
5. **Favoritos**: Marcar productos como favoritos para acceso rápido

## Referencias

- ADR-001: Módulo de Categorías
- ADR-002: Sistema de Inventario
- [ASP.NET Core Session](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/app-state)
