# 📘 Manual de Usuario - Facturapro

> **Sistema de Facturación Electrónica para República Dominicana**  
> **Versión:** 1.0.0-beta  
> **Fecha:** Abril 2026

---

## 📑 Tabla de Contenidos

1. [Introducción](#introducción)
2. [Primeros Pasos](#primeros-pasos)
3. [Configuración Inicial](#configuración-inicial)
4. [Módulo de Facturación](#módulo-de-facturación)
5. [Módulo de Clientes](#módulo-de-clientes)
6. [Módulo de Productos](#módulo-de-productos)
7. [Módulo de Inventario](#módulo-de-inventario)
8. [Punto de Venta (POS)](#punto-de-venta-pos)
9. [Facturación Electrónica DGII](#facturación-electrónica-dgii)
10. [Reportes](#reportes)
11. [Solución de Problemas](#solución-de-problemas)

---

## 🎯 Introducción

Facturapro es un sistema de facturación diseñado para cumplir con los requisitos de la DGII (Dirección General de Impuestos Internos) de República Dominicana en materia de facturación electrónica.

### Características Principales

- ✅ Facturación electrónica certificada (e-CF)
- ✅ Punto de venta con escáner de códigos de barras
- ✅ Control de inventario completo
- ✅ Gestión de clientes y proveedores
- ✅ Reportes de ventas y compras
- ✅ Compatible con impresoras térmicas

---

## 🚀 Primeros Pasos

### Requisitos del Sistema

- Navegador web moderno (Chrome, Firefox, Edge)
- Conexión a Internet (para envío a DGII)
- Resolución mínima: 1366x768

### Acceso al Sistema

1. Abra su navegador web
2. Ingrese la dirección del sistema (ej: `http://localhost:5000`)
3. El sistema cargará el dashboard principal

> **Nota:** Actualmente el sistema no requiere login. Esta función será implementada próximamente.

---

## ⚙️ Configuración Inicial

### 1. Configurar Datos de la Empresa

Antes de emitir facturas, debe configurar los datos de su empresa:

1. Vaya a **Configuración** → **DGII** en el menú lateral
2. Complete los campos obligatorios:
   - **RNC Emisor:** Su número de RNC (9-11 dígitos)
   - **Razón Social:** Nombre legal de la empresa
   - **Dirección:** Dirección fiscal completa
   - **Municipio** y **Provincia**

3. Haga clic en **Guardar Configuración**

### 2. Subir Certificado Digital

Para emitir facturas electrónicas válidas, necesita un certificado digital:

1. En la misma pantalla de DGII, busque la sección **Certificado Digital**
2. Haga clic en **Seleccionar archivo** y elija su certificado (.PFX o .P12)
3. Ingrese la contraseña del certificado
4. Haga clic en **Subir Certificado**

> **⚠️ Importante:** El certificado debe ser emitido por una entidad certificadora autorizada por INDOTEL.

### 3. Seleccionar Modo de Operación

- **Modo Pruebas:** Para pruebas y capacitación. Los comprobantes NO tienen validez fiscal.
- **Modo Producción:** Comprobantes reales con validez ante la DGII.

Para cambiar de modo, use el switch en la sección **Modo de Operación**.

### 4. Configurar Rangos de Numeración

Antes de facturar, debe configurar los rangos autorizados por la DGII:

1. Vaya a **Rangos de Numeración** en el menú
2. Haga clic en **Nuevo Rango**
3. Ingrese:
   - Tipo de e-CF (E31, E32, etc.)
   - Rango desde/hasta (ej: E31000000001 a E31000001000)
   - Fecha de vencimiento
4. Guarde el rango

---

## 🧾 Módulo de Facturación

### Crear una Nueva Factura

1. Vaya a **Facturas** → **Nueva Factura e-CF**
2. Seleccione:
   - Cliente (o cree uno nuevo)
   - Tipo de e-CF (E31-Crédito Fiscal, E32-Consumo, etc.)
   - Fecha de emisión y vencimiento
   - Tipo de pago (Contado, Crédito)
3. Haga clic en **Crear**

### Agregar Líneas a la Factura

1. En la pantalla de edición, ingrese:
   - Descripción del producto/servicio
   - Cantidad
   - Precio unitario
   - Indicador de facturación (con ITBIS, exento, etc.)
2. Haga clic en **Agregar Línea**
3. Repita para cada ítem

### Flujo de Facturación Electrónica

```
Pendiente → Firmado → Enviado DGII → Aceptado/Rechazado
```

| Estado | Descripción | Acción Requerida |
|--------|-------------|------------------|
| Pendiente | Factura creada, sin firmar | Firmar |
| Firmado | XML generado y firmado | Enviar a DGII |
| Enviado | Enviado a DGII, esperando respuesta | Consultar estado |
| En Proceso | DGII procesando | Esperar/Consultar |
| Aceptado | Factura aprobada por DGII | Ninguna |
| Rechazado | Error en la factura | Corregir y reenviar |

### Enviar Factura a la DGII

1. Desde el listado de facturas, busque la factura en estado "Firmado"
2. Haga clic en el botón **Enviar DGII**
3. Espere la respuesta del sistema
4. Si es exitoso, obtendrá un TrackId de seguimiento

### Consultar Estado de Factura

1. Para facturas en estado "Enviado" o "En Proceso"
2. Haga clic en el botón **Consultar**
3. El sistema actualizará el estado con la respuesta de la DGII

### Descargar XML

Para facturas firmadas o procesadas:
1. Haga clic en el botón **XML**
2. Se descargará el archivo XML firmado

---

## 👥 Módulo de Clientes

### Crear un Cliente

1. Vaya a **Clientes** → **Nuevo Cliente**
2. Complete:
   - Nombre/Razón Social (obligatorio)
   - RNC/Cédula (9 u 11 dígitos)
   - Dirección, teléfono, email
3. Guarde

### Buscar Cliente

- Use la barra de búsqueda en el listado
- Puede buscar por nombre, RNC o email

---

## 📦 Módulo de Productos

### Crear un Producto

1. Vaya a **Productos** → **Nuevo Producto**
2. Complete:
   - Código único
   - Nombre del producto
   - Descripción
   - Precio de venta
   - Stock inicial
   - Categoría
3. Guarde

### Generar Código de Barras

1. Al crear/editar un producto, puede:
   - Ingresar un código de barras existente
   - O dejar que el sistema genere uno automáticamente
2. El código de barras se muestra en formato de imagen

### Importar Productos

1. Vaya a **Productos** → **Importar**
2. Prepare un archivo Excel con las columnas: Código, Nombre, Precio, Stock
3. Seleccione el archivo y cargue

---

## 📊 Módulo de Inventario

### Registrar Entrada de Inventario

1. Vaya a **Movimientos** → **Nueva Entrada**
2. Seleccione el producto
3. Ingrese la cantidad entrante
4. Especifique el motivo (compra, devolución, ajuste)
5. Guarde

### Registrar Salida de Inventario

1. Vaya a **Movimientos** → **Nueva Salida**
2. Seleccione el producto
3. Ingrese la cantidad saliente
4. Especifique el motivo (venta, daño, pérdida)
5. Guarde

### Ajuste de Inventario

1. Vaya a **Movimientos** → **Ajuste**
2. Seleccione el producto
3. Ingrese la cantidad real en existencia
4. El sistema calculará la diferencia automáticamente

### Stock Bajo

Vaya a **Stock Bajo** para ver productos que necesitan reabastecimiento.

---

## 💰 Punto de Venta (POS)

El módulo POS permite realizar ventas rápidas con interfaz optimizada.

### Realizar una Venta

1. Vaya a **⚡ Punto de Venta (POS)**
2. Seleccione el cliente (puede ser "Cliente de Contado")
3. Busque productos:
   - Escribiendo el nombre en el buscador
   - O escaneando el código de barras
4. El producto se agrega automáticamente al carrito
5. Ajuste cantidades si es necesario
6. Seleccione tipo de NCF si aplica
7. Haga clic en **Procesar Venta**

### Carrito de Compras

- **Agregar:** Busque y seleccione el producto
- **Actualizar:** Modifique la cantidad directamente
- **Eliminar:** Haga clic en la X del producto
- **Limpiar:** Vacía todo el carrito

### Venta con Código de Barras

1. Enfoque el cursor en el campo de búsqueda
2. Escanee el código de barras con el lector
3. El producto se agrega automáticamente

---

## 🏛️ Facturación Electrónica DGII

### Tipos de Comprobantes Soportados

| Código | Tipo | Uso |
|--------|------|-----|
| E31 | Factura de Crédito Fiscal | Ventas con NCF a crédito |
| E32 | Factura de Consumo | Ventas al contado |
| E33 | Nota de Débito | Aumento en el valor de una factura |
| E34 | Nota de Crédito | Descuentos o devoluciones |
| E41 | Comprobante de Compras | Gastos menores a proveedores |
| E43 | Gastos Menores | Compras hasta RD$50,000 anuales |
| E44 | Regímenes Especiales | Sector agropecuario, zonas francas |
| E45 | Gubernamental | Facturas a entidades públicas |
| E46 | Exportaciones | Ventas al exterior |
| E47 | Pagos al Exterior | Servicios de no residentes |

### Montos Importantes

- **Facturas de Consumo (E32):** Hasta RD$250,000 no requieren ser recibidas por el comprador
- **Facturas ≥ RD$250,000:** Deben ser aceptadas por el comprador en el portal DGII

### Codificación de Productos

| Código | Descripción |
|--------|-------------|
| 1 | Productos con ITBIS (18%) |
| 2 | Servicios con ITBIS (18%) |
| 3 | Productos sin ITBIS |
| 4 | Servicios sin ITBIS |

### Estados del Comprobante

- El sistema consulta automáticamente el estado cada 5 minutos
- También puede consultar manualmente con el botón **Consultar**
- Guarde el TrackId para seguimiento con la DGII

---

## 📈 Reportes

### Disponibles Actualmente

- **Dashboard:** Vista general del negocio
- **Estadísticas:** Gráficos de ventas (en desarrollo)

### Próximamente

- Reporte de ventas por período
- Reporte de inventario
- Estado de resultados
- Cuadre de caja
- Reportes DGII (606, 607, 608)

---

## 🔧 Solución de Problemas

### Error: "No hay rangos de numeración disponibles"

**Solución:**
1. Vaya a **Rangos de Numeración**
2. Cree un nuevo rango autorizado por la DGII
3. El rango debe estar vigente y tener números disponibles

### Error: "No hay certificado configurado"

**Solución:**
1. Vaya a **Configuración** → **DGII**
2. En la sección Certificado Digital, suba su archivo .PFX
3. Ingrese la contraseña correcta
4. Verifique que el certificado no esté vencido

### Error al enviar a DGII: "Servicios no disponibles"

**Solución:**
1. Verifique su conexión a Internet
2. Intente más tarde (los servicios de la DGII pueden estar en mantenimiento)
3. Use el botón **Verificar Conexión** en Configuración DGII

### Factura rechazada por la DGII

**Pasos:**
1. Haga clic en la factura para ver el detalle
2. Revise el mensaje de error en el campo "Mensaje DGII"
3. Corrija el error (generalmente datos del cliente o totales incorrectos)
4. Vuelva a firmar y enviar

### Errores comunes y soluciones

| Error | Causa | Solución |
|-------|-------|----------|
| RNC inválido | Cliente sin RNC correcto | Verifique el RNC del cliente |
| Total incorrecto | Diferencia en cálculo de ITBIS | Revise las líneas de la factura |
| XML mal formado | Error en estructura | Contacte soporte técnico |
| Certificado expirado | Venció la vigencia | Renueve su certificado digital |

---

## 📞 Soporte Técnico

Para soporte técnico contacte a:

- **Email:** soporte@facturapro.do
- **Teléfono:** (809) 555-0000
- **Horario:** Lunes a Viernes, 8:00 AM - 6:00 PM

---

## 📋 Glosario

- **e-CF:** Comprobante Fiscal Electrónico
- **DGII:** Dirección General de Impuestos Internos
- **ITBIS:** Impuesto a la Transferencia de Bienes Industrializados y Servicios
- **NCF:** Número de Comprobante Fiscal
- **RNC:** Registro Nacional de Contribuyentes
- **TrackId:** Código de seguimiento de envío a DGII
- **XML:** Formato de archivo para comprobantes electrónicos

---

## 📄 Documentación Relacionada

- [PROJECT_STATUS.md](./PROJECT_STATUS.md) - Estado del desarrollo
- [DGII_IMPORTANTE.md](./DGII_IMPORTANTE.md) - Notas críticas de DGII
- Documentación oficial DGII: https://dgii.gov.do

---

**© 2026 Facturapro - Todos los derechos reservados**

> **Nota de actualización:** Este manual se actualiza automáticamente con cada nueva funcionalidad agregada al sistema.
