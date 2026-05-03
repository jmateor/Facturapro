# Decisiones Arquitectónicas (ADR)

Este documento registra las decisiones arquitectónicas importantes tomadas en el proyecto Facturapro.

## Índice

1. [ADR-001: Formato CSV para Importación/Exportación](#adr-001-formato-csv-para-importaciónexportación)
2. [ADR-002: Generación de Códigos de Barra](#adr-002-generación-de-códigos-de-barra)
3. [ADR-003: Soporte de Lectora de Código de Barra en POS](#adr-003-soporte-de-lectora-de-código-de-barra-en-pos)
4. [ADR-004: Módulo Kalder para Gestión de Almacén](#adr-004-módulo-kalder-para-gestión-de-almacén)
5. [ADR-005: Gestión de Ventas a Crédito y Cuentas por Cobrar](#adr-005-gestión-de-ventas-a-crédito-y-cuentas-por-cobrar)
6. [ADR-006: Módulo Dedicado de Notas de Crédito (E34)](#adr-006-módulo-dedicado-de-notas-de-crédito-e34)

---

## ADR-001: Formato CSV para Importación/Exportación

**Estado:** Aceptado  
**Fecha:** 2026-04-20  
**Contexto:** El sistema necesitaba soporte para importar y exportar productos desde/hacia archivos externos. Inicialmente se consideró Excel (.xlsx).

**Decisión:** Usar formato CSV nativo en lugar de bibliotecas externas como EPPlus o ClosedXML.

**Motivación:**
- NuGet PackageSourceMapping impedía la instalación de paquetes externos (EPPlus y ClosedXML fallaron)
- CSV no requiere dependencias externas - se maneja con clases nativas de .NET (`StreamReader`, `StreamWriter`)
- Formato universalmente compatible y legible
- Menor complejidad y tamaño del proyecto
- Fácil de generar y parsear sin librerías de terceros

**Consecuencias:**
- ✅ Proyecto se mantiene sin dependencias pesadas de Excel
- ✅ Funcionalidad completamente operativa
- ✅ Reducción de tamaño de binarios
- ⚠️ Los usuarios deben usar formato CSV en lugar de Excel

**Implementación:**
- `ProductosController.Importar()` - Parseo CSV con `ParseCsvLine()` personalizado
- `ProductosController.Exportar()` - Generación CSV con `EscapeCsvField()`
- `ProductosController.DescargarPlantilla()` - Plantilla CSV de ejemplo

---

## ADR-002: Generación de Códigos de Barra

**Estado:** Aceptado  
**Fecha:** 2026-04-20  
**Contexto:** El usuario solicitó que cada producto tuviera un código de barra generado automáticamente.

**Decisión:** Implementar generación de códigos de barra Code 128 como SVG inline, sin dependencias de librerías de gráficos.

**Motivación:**
- Code 128 es un estándar universal de códigos de barra
- Generación SVG pura (sin imágenes raster) para escalabilidad e impresión
- No requiere librerías externas de generación de imágenes
- Fácil de imprimir desde el navegador
- Integración directa con el sistema existente

**Consecuencias:**
- ✅ Cada producto tiene código de barras único generado automáticamente
- ✅ Visualización e impresión desde la vista de detalles del producto
- ✅ Compatible con lectores de código de barra estándar
- ⚠️ Códigos generados son Code 128 (no EAN-13 que requiere registro GS1)

**Implementación:**
- Nuevo campo `Producto.CodigoBarras` en entidad
- `IBarcodeService` / `BarcodeService` - Servicio de generación
- Patrones Code 128 codificados en el servicio
- Generación automática al crear/importar productos

---

## ADR-003: Soporte de Lectora de Código de Barra en POS

**Estado:** Aceptado  
**Fecha:** 2026-04-20  
**Contexto:** Se requería permitir el uso de lectoras de código de barra en el Punto de Venta (POS).

**Decisión:** Implementar campo de entrada dedicado para códigos de barra con manejo de eventos JavaScript optimizado para escáneres USB.

**Motivación:**
- Las lectoras de código de barra USB actúan como teclado, enviando caracteres rápidamente
- Se requiere detectar la entrada completa del código sin interferir con la navegación normal
- Los escáneres suelen enviar caracteres seguidos de Enter/Return
- Enfoque "scan-and-go": escanear y agregar al carrito automáticamente

**Consecuencias:**
- ✅ Compatible con cualquier lectora de código de barra USB (HID Keyboard)
- ✅ Agregado automático al carrito al escanear
- ✅ Foco automático en el campo de escaneo para flujo continuo
- ✅ Fallback para entrada manual del código

**Implementación:**
- Campo dedicado `#scanBarcode` en la vista POS
- Event listeners: `input` con debounce y `keypress` para Enter
- Endpoint `POS/AgregarPorCodigoBarras` para búsqueda por código
- Detección global de teclado para autofoco en campo de escaneo

---

## ADR-004: Módulo Kalder para Gestión de Almacén

**Estado:** Aceptado  
**Fecha:** 2026-04-20  
**Contexto:** El usuario solicitó un módulo "Kalder" para gestión del almacén/warehouse.

**Decisión:** Crear un controlador y vistas independientes (`KalderController`) especializado en operaciones de inventario: entradas, salidas, ajustes, historial y reportes.

**Motivación:**
- Separación de responsabilidades: Kalder se enfoca en operaciones de almacén
- El controlador existente `MovimientosInventario` era más de consulta
- Necesidad de flujos de trabajo específicos: entrada de mercancía, salidas manuales, ajustes de inventario
- Dashboard específico para almacén con alertas de stock
- Reutilización del sistema de `MovimientoInventario` existente

**Consecuencias:**
- ✅ Módulo completo de gestión de almacén
- ✅ Dashboard con estadísticas y alertas en tiempo real
- ✅ Control de entradas con registro de proveedor y factura
- ✅ Control de salidas con validación de stock
- ✅ Ajustes de inventario con auditoría obligatoria
- ✅ Historial completo de movimientos
- ✅ Reportes por estado (bajo, agotado, sobre-stock)

**Implementación:**
- `KalderController` - Controlador principal del módulo
- Vistas: Index, Inventario, Entrada, Salida, Ajuste, Historial, ReporteStock
- Integración con entidad `MovimientoInventario` existente
- Agregado al menú lateral con estilo destacado

---

## ADR-005: Gestión de Ventas a Crédito y Cuentas por Cobrar

**Estado:** Aceptado  
**Fecha:** 2026-05-03  
**Contexto:** Se requería que el sistema permitiera realizar ventas sin cobro inmediato, manteniendo un registro de deudas por cliente.

**Decisión:** Integrar la condición de pago "A Crédito" en el POS y crear un sistema de seguimiento automático en la entidad `Cliente` y un módulo de `Cuentas por Cobrar`.

**Motivación:**
- Necesidad de soportar el flujo comercial dominicano de facturas a crédito.
- El POS debe permitir abonos iniciales (pagos parciales).
- Sincronización automática con el balance del cliente para evitar errores manuales.
- Visibilidad fiscal en PDF indicando balance pendiente.

**Consecuencias:**
- ✅ El POS ahora permite seleccionar "Crédito" como condición.
- ✅ Las facturas a crédito se guardan con balance pendiente.
- ✅ Los recibos de venta muestran "Monto Pagado" y "Balance Pendiente".
- ✅ Integración con el módulo de Cuentas por Cobrar para gestión de abonos posteriores.

**Implementación:**
- `Factura.TipoPago = 2` (Crédito) en base de datos.
- `POSController.FinalizarVenta()` - Lógica de abono y cálculo de balance.
- Vista `POS/Completada.cshtml` - Etiquetas dinámicas de condición de pago.

---

## ADR-006: Módulo Dedicado de Notas de Crédito (E34)

**Estado:** Aceptado  
**Fecha:** 2026-05-03  
**Contexto:** La emisión de Notas de Crédito era un proceso manual y propenso a errores de referencia fiscal.

**Decisión:** Crear un flujo de emisión simplificado "One-Click" que automatice la creación de documentos E34 referenciando facturas originales y revertiendo stock.

**Motivación:**
- Requerimiento fiscal de la DGII para anulación o corrección de facturas emitidas.
- Necesidad de asegurar la trazabilidad (nodo `<InformacionReferencia>` en XML).
- Mejorar la UX permitiendo emitir la nota directamente desde el listado de facturas.
- Automatizar la devolución de productos al inventario al anular la factura.

**Consecuencias:**
- ✅ Nuevo acceso dedicado en el sidebar para "Notas de Crédito".
- ✅ Botón de acción rápida en `Facturas/Index` para emisión inmediata.
- ✅ Autocompletado de datos del cliente e ítems desde la factura de origen.
- ✅ Garantía de cumplimiento fiscal para el e-NCF E34.

**Implementación:**
- `FacturasController.Index()` - Filtro dinámico por parámetro `tipoECF`.
- `FacturasController.EmitirNotaCredito()` - Acción para generar el documento vinculado.
- Sidebar dinámico con resaltado de sección activa.

---

## Notas Generales

### Convenciones seguidas:
- Usar inyección de dependencias para servicios (`IBarcodeService`)
- Mantener consistencia con el patrón MVC existente
- Usar el sistema de `TempData` para mensajes de éxito/error
- Seguir convenciones de nomenclatura del proyecto

### Consideraciones futuras:
- ✅ **Cumplimiento GS1 (EAN-13):** Implementado el 03 de mayo. El sistema ahora detecta códigos de 12/13 dígitos y los genera bajo el estándar global.
- Considerar paginación en reportes grandes de Kalder
- Posible integración con impresoras térmicas para códigos de barra
