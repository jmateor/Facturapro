# ADR-002: Sistema de Inventario Completo y Dashboard Funcional

## Status
**Aceptado** - Implementado el 20 de abril de 2026

## Contexto

Tras implementar el módulo de Categorías, se requería completar el sistema de inventario con funcionalidades de gestión de proveedores, compras y control de stock. Además, el Dashboard estaba mostrando datos mockup (ficticios) y necesitaba conectarse a la base de datos para mostrar estadísticas reales del negocio.

## Decisiones

### 1. Sistema de Inventario Completo

Se implementaron las siguientes entidades y relaciones:

#### Entidades Creadas

**Proveedor**
- Gestión completa de proveedores con datos de contacto
- Relación uno-a-muchos con Compras

**Compra y CompraLinea**
- Registro de compras a proveedores
- Estados: Pendiente → Recibida/Cancelada
- Cálculo automático de subtotales, ITBIS (18%), descuentos y total
- Al recibir una compra, se actualiza automáticamente el stock de productos

**MovimientoInventario**
- Historial completo de todos los cambios de stock
- Tipos de movimiento: EntradaCompra, EntradaDevolucion, EntradaAjuste, SalidaVenta, SalidaDano, SalidaAjuste, SalidaConsumo
- Registro de stock anterior y nuevo para trazabilidad

#### Flujo de Inventario

```
Compra (Pendiente) → Recibir → Actualiza Stock → Registra Movimiento
         ↓
    Cancelar → Revierte stock (si ya fue recibida)
```

### 2. Dashboard Funcional

Se eliminaron los datos mockup y se implementó un Dashboard que consulta datos reales:

#### Estadísticas Implementadas

| Métrica | Descripción |
|---------|-------------|
| Ingresos del Mes | Suma de facturas del mes actual (no canceladas) |
| Facturas del Mes | Conteo de facturas emitidas este mes |
| Facturas Pendientes | Facturas en estado Pendiente o Emitida |
| Clientes Registrados | Total de clientes en el sistema |
| Clientes con Facturas | Clientes que tienen al menos una factura |
| Productos Activos | Productos marcados como activos |
| Stock Bajo | Productos con stock <= 10 unidades |
| Facturas Recientes | Últimas 10 facturas ordenadas por fecha |

### 3. Reorganización del Menú

El menú lateral se reorganizó en secciones lógicas:

**Principal**
- Dashboard

**Ventas**
- Clientes
- Facturas

**Catálogo**
- Productos
- Categorías

**Inventario**
- Movimientos (Historial completo)
- Proveedores
- Compras
- Stock Bajo (Alertas)

**DGII - Facturación Electrónica**
- Rangos de Numeración
- Configuración DGII

**Reportes**
- Estadísticas

## Archivos Creados/Modificados

### Nuevos Controladores
| Controlador | Descripción |
|-------------|-------------|
| `ProveedoresController` | CRUD de proveedores con validación de nombre único |
| `ComprasController` | Gestión de compras con recepción y cancelación |
| `MovimientosInventarioController` | Entradas, salidas, ajustes y alertas de stock |

### Nuevas Vistas
| Vista | Funcionalidad |
|-------|---------------|
| `Proveedores/*` | CRUD completo de proveedores |
| `Compras/*` | Crear compras, ver detalles, recibir/cancelar |
| `MovimientosInventario/*` | Historial, entradas manuales, salidas, ajustes, stock bajo |

### Archivos Modificados
| Archivo | Cambios |
|---------|---------|
| `HomeController.cs` | Ahora consulta datos reales de la BD |
| `Views/Home/Index.cshtml` | Dashboard funcional con estadísticas reales |
| `Views/Shared/_Layout.cshtml` | Menú reorganizado en secciones |
| `Views/Shared/_ValidationScriptsPartial.cshtml` | Creado para validaciones de formularios |

## Consecuencias

### Positivas

1. **Inventario Controlado**: Todo cambio de stock queda registrado en el historial
2. **Trazabilidad**: Se puede rastrear el origen de cada entrada/salida de productos
3. **Alertas Proactivas**: El sistema avisa cuando hay productos con stock bajo
4. **Dashboard Real**: El usuario ve información actualizada de su negocio al iniciar sesión
5. **Flujo de Compras**: Las compras a proveedores actualizan el inventario automáticamente

### Negativas

1. **Complejidad**: El sistema ahora tiene más módulos interconectados
2. **Dependencias**: Las compras dependen de proveedores y productos
3. **Validaciones**: Se requieren más validaciones para mantener la integridad del inventario

## Flujos de Trabajo Implementados

### Flujo de Compra
1. Crear compra con productos y cantidades
2. Compra queda en estado "Pendiente"
3. Al recibir: Se actualiza stock de cada producto + Se crean movimientos de inventario
4. Al cancelar: Si ya fue recibida, revierte el stock

### Flujo de Ajuste de Inventario
1. Ir a Inventario > Ajuste
2. Seleccionar producto
3. Ingresar stock físico real
4. El sistema calcula la diferencia y crea un movimiento de ajuste (entrada o salida)

### Flujo de Entrada/Salida Manual
1. Ir a Inventario > Movimientos
2. Seleccionar Entrada Manual o Salida Manual
3. Seleccionar producto y cantidad
4. Especificar motivo
5. El sistema actualiza el stock y registra el movimiento

## Próximos Pasos Sugeridos

1. **Auditoría**: Agregar quién realizó cada movimiento de inventario (usuario logueado)
2. **Reportes de Inventario**: Reportes de rotación, productos más vendidos, etc.
3. **Alertas por Email**: Notificar cuando el stock esté bajo
4. **Kardex**: Reporte detallado de entradas y salidas por producto
5. **Precios de Compra**: Historial de precios de compra por producto y proveedor

## Referencias

- ADR-001: Módulo de Categorías
- [Entity Framework Core Transactions](https://docs.microsoft.com/en-us/ef/core/saving/transactions)
- [ASP.NET Core Dependency Injection](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection)
