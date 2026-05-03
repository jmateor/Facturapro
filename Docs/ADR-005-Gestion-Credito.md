# ADR-005: Gestión de Ventas a Crédito y Cuentas por Cobrar

## Status
**Aceptado** - Implementado el 03 de mayo de 2026

## Contexto

El sistema Facturapro necesitaba evolucionar de un modelo de ventas puramente al contado a un modelo bimodal que soporte el crédito comercial, muy común en el mercado dominicano. Los requerimientos clave eran:

1. Permitir que una venta en el POS se marque como "A Crédito".
2. Registrar pagos parciales (abonos iniciales) al momento de la venta.
3. Mantener un balance actualizado de deudas por cliente.
4. Reflejar la condición de pago y el balance pendiente en los documentos fiscales (Representación Impresa).

## Decisión

Se decidió implementar un flujo integrado que conecta el Punto de Venta con el módulo de Clientes y la contabilidad fiscal de la DGII.

### Cambios en el Modelo de Datos

- **Factura:** Se utiliza el campo `TipoPago` (entero) donde `1 = Contado` y `2 = Crédito`. Se añadieron campos para `MontoPagado` y `BalancePendiente`.
- **Cliente:** Se utiliza para agrupar las deudas y gestionar los límites de crédito configurados.

### Flujo en el Punto de Venta (POS)

El modal de cobro fue actualizado para incluir un toggle de "Condición de Pago":
- Si es **Contado**: El monto pagado debe ser igual o mayor al total.
- Si es **Crédito**: Se permite que el monto pagado sea menor al total. La diferencia se asigna automáticamente al `BalancePendiente`.

### Representación Impresa (PDF)

Se modificó `PdfService.cs` para detectar la condición de pago:
- Si `TipoPago == 2`, se añade una sección de "RESUMEN DE CRÉDITO" que detalla el monto abonado y el saldo restante.
- Se incluye la leyenda legal correspondiente para facturas a crédito.

## Consecuencias

### Positivas
1. **Flexibilidad Comercial:** Permite cerrar ventas con clientes recurrentes que pagan periódicamente.
2. **Control Financiero:** Automatiza el seguimiento de deudas sin intervención manual.
3. **Cumplimiento Fiscal:** El XML generado para la DGII ahora incluye correctamente la tabla de formas de pago con el código de crédito.

### Negativas
1. **Complejidad en Devoluciones:** Las notas de crédito ahora deben considerar si el dinero se devuelve o si se descuenta del balance pendiente del cliente.

## Implementación Técnica

- **Backend:** `POSController.FinalizarVenta` ahora procesa la lógica de crédito.
- **Frontend:** Interfaz de usuario con validaciones para no exceder el límite de crédito del cliente.
- **Servicios:** Actualización del generador de PDF para mostrar balances.

---
**Próximos Pasos:**
- Implementar reporte de antigüedad de saldo.
- Agregar alertas de cobro automáticas vía WhatsApp.
