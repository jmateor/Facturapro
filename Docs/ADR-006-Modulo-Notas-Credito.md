# ADR-006: Módulo Dedicado de Notas de Crédito (E34)

## Status
**Aceptado** - Implementado el 03 de mayo de 2026

## Contexto

Anteriormente, la emisión de Notas de Crédito (E34) se realizaba de forma genérica, lo que dificultaba la trazabilidad fiscal exigida por la DGII. Se requería un módulo que facilitara la anulación total o parcial de facturas, asegurando que:
1. Se referencie correctamente el e-NCF original.
2. El stock de los productos se revierta automáticamente al inventario.
3. La interfaz sea intuitiva y rápida para el usuario (flujo de un solo clic).

## Decisión

Se implementó un módulo dedicado y un flujo de emisión asistida desde la consulta de facturas.

### Arquitectura del Módulo

- **Filtro Dinámico:** La vista de facturas ahora acepta un parámetro `tipoECF` para actuar como un módulo independiente de "Notas de Crédito" en el sidebar.
- **Acciones Rápidas:** Se integró un botón de "Emitir Nota de Crédito" en cada fila del listado de facturas.

### Automatización de Referencia Fiscal

Para cumplir con la DGII, el sistema ahora inyecta automáticamente el nodo `<InformacionReferencia>` en el XML:
- `NCFModificado`: Toma el e-NCF de la factura origen.
- `CodigoModificacion`: Por defecto `1` (Anulación total) o `2` (Corrección parcial).

### Integración con Inventario (Kalder)

Al procesar una Nota de Crédito (E34), el sistema dispara un evento de inventario que crea una "Entrada por Devolución" para cada ítem de la factura, manteniendo el stock sincronizado en tiempo real.

## Consecuencias

### Positivas
1. **Reducción de Errores:** Elimina la necesidad de escribir manualmente el NCF de referencia.
2. **Eficiencia Operativa:** El personal de caja puede anular facturas en segundos.
3. **Consistencia de Stock:** El inventario siempre refleja la realidad física tras una devolución.

### Negativas
1. **Rigidez:** Solo se pueden emitir notas de crédito sobre facturas que ya han sido firmadas o aceptadas por la DGII.

## Implementación Técnica

- **Controlador:** `FacturasController.EmitirNotaCredito` (POST) realiza la clonación y vinculación.
- **Vistas:** `Views/Facturas/Index.cshtml` actualizada con los botones de acción rápida y filtrado dinámico.
- **SideBar:** Nueva entrada "Notas de Crédito" con resaltado inteligente.

---
**Próximos Pasos:**
- Permitir la selección parcial de ítems para notas de crédito de corrección.
- Integrar firma automática masiva para notas de crédito.
