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
7. [Módulo de Proveedores](#módulo-de-proveedores)
8. [Módulo de Compras](#módulo-de-compras)
9. [Módulo de Inventario (Kalder)](#módulo-de-inventario-kalder)
10. [Punto de Venta (POS)](#punto-de-venta-pos)
11. [Módulo de Reportes y DGII](#módulo-de-reportes-y-dgii)
12. [Gestión de Usuarios y Roles](#gestión-de-usuarios-y-roles)
13. [Solución de Problemas](#solución-de-problemas)
14. [Preguntas Frecuentes](#preguntas-frecuentes)

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
2. Ingrese la dirección del sistema (ej: `http://localhost:5000` o `https://facturapro.suempresa.com`)
3. Será redirigido a la página de Login
4. Ingrese sus credenciales:
   - **Email:** su correo electrónico registrado
   - **Contraseña:** su contraseña personal
5. Haga clic en **Iniciar Sesión**

> 🔒 **Nota:** El sistema requiere autenticación y su navegación estará adaptada según su nivel de acceso. Los roles disponibles son: Admin, Gerente, Vendedor y Cajero.

### Credenciales de Administrador (Instalación Nueva)

Si es la primera vez que instala el sistema, use las credenciales del super administrador:

- **Email:** `josemateo3148@gmail.com`
- **Contraseña:** `Jmateor11099@`

> ⚠️ **Importante:** Cambie esta contraseña inmediatamente después del primer acceso por seguridad.

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

### Flujo Completo de Emisión de e-CF

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   CREAR     │ ──► │   EDITAR    │ ──► │   FIRMAR    │ ──► │  ENVIAR     │
│   FACTURA   │     │ + LÍNEAS    │     │   DIGITAL   │     │   A DGII    │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
       │                                                           │
       ▼                                                           ▼
┌─────────────┐                                         ┌─────────────┐
│  DESCARGAR  │ ◄────────────────────────────────────── │ CONSULTAR   │
│  PDF / XML  │                                         │   ESTADO    │
└─────────────┘                                         └─────────────┘
```

### Paso 1: Crear Factura

1. Vaya a **Facturas** → **Nueva Factura e-CF**
2. Complete el formulario:

| Campo | Descripción |
|-------|-------------|
| **Tipo de Comprobante** | E31 (Crédito Fiscal), E32 (Consumo), E33 (Nota Débito), E34 (Nota Crédito) |
| **Cliente** | Seleccione de la lista o cree uno nuevo |
| **Fecha de Emisión** | Fecha actual (por defecto) |
| **Fecha de Vencimiento** | 30 días después (por defecto) |
| **Tipo de Pago** | Contado, Crédito, Mixto |
| **ITBIS (%)** | 18% por defecto |
| **Moneda** | DOP (Pesos Dominicanos) o USD (Dólares). Si selecciona USD, se le pedirá la **Tasa de Cambio**. |
| **Notas** | Comentarios adicionales (opcional) |

3. Haga clic en **Crear Factura e-CF**

> ✅ El sistema asignará automáticamente un número e-CF del rango disponible.

### Paso 2: Agregar Líneas

Después de crear la factura, será redirigido a la pantalla de **Editar Factura**:

1. Haga clic en **Agregar Línea**
2. Complete el modal:

| Campo | Descripción |
|-------|-------------|
| **Descripción** | Nombre del producto o servicio |
| **Nombre del Ítem** | Nombre específico (opcional) |
| **Cantidad** | Número de unidades |
| **Precio Unitario** | Precio por unidad |
| **ITBIS** | Gravado (18%), Exento, Sin derecho a crédito |

3. Haga clic en **Agregar Línea**
4. Repita para todos los productos

> 💡 **Tip:** Puede eliminar líneas haciendo clic en el botón 🗑️ junto a cada una.

### Paso 3: Firmar Digitalmente

**Requisitos previos:**
- ✅ Certificado digital configurado en `Configuración > DGII`
- ✅ Al menos una línea agregada
- ✅ Cliente válido con RNC/Cédula

1. Verifique que todos los datos sean correctos
2. Haga clic en **Firmar Digitalmente**
3. El sistema generará:
   - XML del e-CF según formato DGII
   - Firma digital con su certificado PFX
   - Código QR de verificación
4. El estado cambiará a **Firmado**

> ⏱️ **Tiempo estimado:** 5-10 segundos

### Paso 4: Enviar a la DGII

1. Con la factura en estado **Firmado**, haga clic en **Enviar a la DGII**
2. El sistema realizará:
   - Autenticación con la API de la DGII
   - Envío del comprobante firmado
   - Recepción del TrackId de seguimiento
3. El estado cambiará a **En Proceso**

> ⏱️ **Tiempo estimado:** 10-30 segundos

### Paso 5: Consultar Estado

1. Haga clic en **Consultar Estado**
2. El sistema verificará en la DGII y mostrará:
   - **Aceptado:** El comprobante fue recibido correctamente ✅
   - **Rechazado:** Hay errores que deben corregirse ❌
   - **En Proceso:** Aún se está validando ⏳

### Paso 6: Descargar Documentos

#### Descargar PDF (Representación Impresa)

1. Haga clic en **Descargar PDF**
2. El PDF incluye:
   - Datos del emisor y comprador
   - Detalle de productos/servicios
   - Totales desglosados (Subtotal, ITBIS, Total)
   - Código QR de verificación DGII
   - Leyenda legal requerida

#### Descargar XML

1. Haga clic en **Descargar XML**
2. Obtendrá el archivo XML firmado digitalmente
3. Útil para:
   - Contabilidad y auditorías
   - Respaldo legal
   - Envío a clientes corporativos

### Compartir Factura

Desde la vista de **Detalles** puede compartir la factura:

#### Enviar por Email
1. Haga clic en **Enviar por Email**
2. Se abrirá su cliente de correo predeterminado
3. El asunto y cuerpo se generan automáticamente con los datos de la factura

#### Enviar por WhatsApp
1. Haga clic en **Enviar por WhatsApp**
2. Se abrirá WhatsApp Web con el mensaje prellenado
3. El mensaje incluye: número de factura, fecha y monto total

### Estados de una Factura

| Estado | Descripción | Acciones Disponibles |
|--------|-------------|---------------------|
| **Pendiente** | Creada, sin líneas | Editar, Agregar Líneas |
| **Firmado** | XML firmado digitalmente | Enviar a DGII, Descargar XML |
| **Enviado** | En proceso de validación | Consultar Estado |
| **En Proceso** | Validando en DGII | Consultar Estado |
| **Aceptado** | Aprobado por DGII | Descargar PDF/XML, Compartir |
| **Rechazado** | Errores detectados | Corregir, Reenviar |

### Notas de Crédito y Débito

#### Cuándo Usar

| Tipo | Cuándo Usar | Ejemplo |
|------|-------------|---------|
| **Nota de Crédito (E34)** | Devoluciones, descuentos posteriores, corrección que disminuye valor | Cliente devuelve producto defectuoso |
| **Nota de Débito (E33)** | Recargos, intereses, corrección que aumenta valor | Cliente debe pagar interés por mora |

#### Cómo Crear

1. Cree una factura normal
2. Seleccione el tipo **E33** o **E34**
3. En las líneas, referencie la factura original en la descripción
4. Use montos negativos para notas de crédito

---

## 👥 Módulo de Clientes

### Ver Listado de Clientes

**Ruta:** `Clientes`

La tabla muestra:
- Nombre / Razón Social
- RNC / Cédula
- Teléfono y Email
- Ciudad y Provincia
- Estado (Activo/Inactivo)

#### Filtros Disponibles
- Buscador por nombre, RNC o email
- Estado (Activo/Inactivo)
- Ordenamiento por columna

### Crear un Cliente

1. Vaya a **Clientes** → **Nuevo Cliente**
2. Complete los datos:

#### Información Básica

| Campo | Obligatorio | Descripción |
|-------|-------------|-------------|
| **Nombre** | Sí | Nombre completo o razón social |
| **Tipo de Documento** | Sí | RNC, Cédula, Pasaporte, Sin ID |
| **Documento** | Depende | Número de identificación (9-11 dígitos para RNC) |
| **Email** | No | Correo electrónico para envío de facturas |
| **Teléfono** | No | Número de contacto |

#### Dirección

| Campo | Obligatorio | Descripción |
|-------|-------------|-------------|
| **Dirección** | No | Calle, número, sector |
| **Ciudad** | No | Municipio o ciudad |
| **Provincia** | No | Provincia del país |

#### Configuración de Crédito

| Campo | Obligatorio | Descripción |
|-------|-------------|-------------|
| **Límite de Crédito** | No | Monto máximo a vender a crédito |
| **Días de Crédito** | No | Plazo máximo en días |

3. Haga clic en **Guardar**

### Editar Cliente

1. Haga clic en el cliente que desea editar
2. Modifique los campos necesarios
3. Haga clic en **Guardar**

### Activar/Inactivar Cliente

1. Abra el detalle del cliente
2. Cambie el interruptor **Activo**
3. Los clientes inactivos no aparecerán en ventas nuevas

### Validar RNC ante DGII

> 💡 **Función disponible:** Al crear un cliente con RNC, el sistema puede validarlo automáticamente ante la DGII.

1. Ingrese el RNC del cliente
2. El sistema verificará si está registrado como contribuyente
3. Si es válido, podrá autocompletar nombre y dirección

---

## 📦 Módulo de Productos

### Ver Listado de Productos

**Ruta:** `Productos`

La tabla muestra:
- Código de barras
- Nombre del producto
- Categoría
- Precio de venta
- Stock actual
- Estado (Activo/Inactivo)

#### Filtros Disponibles
- Buscador por nombre o código de barras
- Categoría
- Proveedor
- Estado (Activo/Inactivo)
- Stock bajo

### Crear un Producto

1. Vaya a **Productos** → **Nuevo Producto**
2. Complete la información:

#### Información Básica

| Campo | Obligatorio | Descripción |
|-------|-------------|-------------|
| **Código de Barras** | Sí | Código EAN/UPC único |
| **Nombre** | Sí | Nombre del producto |
| **Descripción** | No | Detalles adicionales |
| **Categoría** | Sí | Clasificación del producto |
| **Unidad de Medida** | Sí | Unidad, Docena, Kg, Litro, etc. |

#### Precios

| Campo | Obligatorio | Descripción |
|-------|-------------|-------------|
| **Precio de Costo** | Sí | Precio de compra al proveedor |
| **Precio de Venta** | Sí | Precio al público |
| **ITBIS (%)** | Sí | 18%, Exento (0%), etc. |

#### Inventario

| Campo | Obligatorio | Descripción |
|-------|-------------|-------------|
| **Stock Actual** | Sí | Cantidad disponible |
| **Stock Mínimo** | No | Alerta cuando llegar a este nivel |
| **Stock Máximo** | No | Límite recomendado de inventario |

3. Haga clic en **Guardar**

### Editar Producto

1. Haga clic en el producto que desea editar
2. Modifique los campos necesarios
3. Haga clic en **Guardar**

> ⚠️ **Precaución:** Cambiar el código de barras puede afectar ventas registradas.

### Importar Productos desde CSV

1. Vaya a **Productos** → **Importar**
2. Descargue la **plantilla de ejemplo**
3. Complete los datos en Excel
4. Guarde como **CSV (UTF-8)**
5. Seleccione el archivo y haga clic en **Importar**

#### Formato CSV Esperado

```csv
CodigoBarras,Nombre,Descripcion,Categoria,PrecioCosto,PrecioVenta,StockMinimo,ITBIS
"7012345678901","Producto A","Descripción del producto","Categoría1",100.00,150.00,10,"18"
"7012345678902","Producto B","Otro producto","Categoría1",200.00,300.00,5,"18"
```

### Exportar Productos

1. Vaya a **Productos** → **Exportar**
2. Aplique filtros si lo desea (categoría, estado, etc.)
3. Haga clic en **Exportar CSV**
4. El archivo se descargará automáticamente

> ✅ Los archivos se exportan con codificación UTF-8 BOM para compatibilidad total con Excel.

### Generar Código de Barras

Si el producto no tiene código de barras:

1. El sistema puede generar uno automáticamente
2. También puede usar códigos internos
3. El código de barras se muestra en la vista de detalles

---

## 📦 Módulo de Proveedores

### Ver Listado de Proveedores

**Ruta:** `Proveedores`

Funciona de manera similar al módulo de clientes.

### Crear Proveedor

1. Vaya a **Proveedores** → **Nuevo Proveedor**
2. Complete:

| Campo | Obligatorio | Descripción |
|-------|-------------|-------------|
| **Nombre** | Sí | Nombre completo o razón social |
| **Tipo de Documento** | Sí | RNC, Cédula, Pasaporte |
| **Documento** | Sí | Número de identificación |
| **Dirección** | No | Dirección completa |
| **Teléfono** | No | Número de contacto |
| **Email** | No | Correo electrónico |
| **Contacto** | No | Persona de contacto |
| **Condiciones de Pago** | No | Días de crédito, descuentos |

3. Haga clic en **Guardar**

---

## 🛒 Módulo de Compras

### Registrar Nueva Compra

1. Vaya a **Compras** → **Nueva Compra**
2. Seleccione el **Proveedor**
3. Complete los datos del comprobante:

| Campo | Descripción |
|-------|-------------|
| **Tipo de Comprobante** | NCF, e-CF, Ticket, Sin comprobante |
| **Número de Comprobante** | Número del documento recibido |
| **Fecha de Recepción** | Fecha cuando recibió la mercancía |

4. Agregue productos:
   - Busque por nombre o código
   - Ingrese la cantidad recibida
   - Verifique el precio unitario
   - Seleccione el ITBIS correspondiente

5. Revise los totales
6. Haga clic en **Guardar Compra**

### Efectos de Registrar una Compra

- ✅ Aumenta el stock de los productos
- ✅ Registra el costo actualizado (promedio ponderado)
- ✅ Crea registro en historial de compras
- ✅ Genera movimiento de inventario

### Ver Compras

**Ruta:** `Compras`

Filtros disponibles:
- Por proveedor
- Por fecha (desde/hasta)
- Por tipo de comprobante
- Por estado

---

## 📦 Módulo de Inventario (Kalder)

### Ver Inventario Actual

**Ruta:** `Kalder > Inventario`

La tabla muestra:
- Producto
- Stock Actual
- Stock Mínimo
- Stock Máximo
- Estado (Normal, Bajo, Agotado)

#### Filtros Disponibles
- Por categoría
- Por proveedor
- Solo stock bajo
- Buscador por nombre

### Ajustes de Inventario

1. Haga clic en **Nuevo Ajuste**
2. Seleccione el producto
3. Indique:
   - **Tipo de ajuste:** Entrada, Salida, Corrección
   - **Cantidad:** Número de unidades
   - **Motivo:** Compra, Venta, Devolución, Daño, Pérdida, Otro
4. Agregue observaciones (opcional)
5. Haga clic en **Guardar**

### Movimientos de Inventario

**Ruta:** `Kalder > Movimientos`

Historial completo de todos los movimientos:
- Compras (entrada)
- Ventas (salida)
- Ajustes manuales
- Transferencias

#### Filtros de Movimientos
- Por producto
- Por tipo de movimiento
- Por fecha
- Por usuario que registró

### Historial de Inventario

**Ruta:** `Kalder > Historial`

Visualice la evolución del inventario:
- Gráficos de stock por período
- Productos con mayor rotación
- Productos sin movimiento
- Valorización del inventario

---

## 💰 Punto de Venta (POS)

El módulo POS permite realizar ventas rápidas con interfaz optimizada para cobro en mostrador.

### Abrir Caja

1. Vaya a **POS > Nueva Venta**
2. El sistema registrará la apertura de caja
3. Ingrese el monto inicial en caja (opcional)

### Realizar una Venta

1. **Seleccionar Cliente (Opcional):**
   - Puede usar "Cliente de Contado" para ventas sin registro
   - O seleccione un cliente registrado para facturar

2. **Agregar Productos:**
   - Escriba el nombre en el buscador, o
   - Escanee el código de barras con el lector, o
   - Seleccione de la cuadrícula de productos

3. **Modificar Cantidades:**
   - Use los botones + / - en el carrito
   - O escriba la cantidad directamente

4. **Aplicar Descuentos:**
   - Descuento por línea (si está habilitado en configuración)
   - Descuento global al total de la venta

5. **Seleccionar Tipo de Pago:**
   - Efectivo
   - Tarjeta de Crédito/Débito
   - Mixto (efectivo + tarjeta)

6. **Moneda y Tasa de Cambio (Bimonetario):**
   - El POS soporta ventas en DOP y USD.
   - Puede cambiar la moneda desde el panel de pago.
   - El sistema calculará el total en DOP automáticamente para fines fiscales según la tasa del día.

7. **Completar Venta:**
   - Haga clic en **Cobrar**
   - El sistema calculará el cambio automáticamente
   - Imprima el comprobante térmico o formato completo (Elite UI)

### Carrito de Compras

| Acción | Descripción |
|--------|-------------|
| **Agregar** | Busque y seleccione el producto |
| **Actualizar** | Modifique la cantidad en el carrito |
| **Eliminar** | Haga clic en la 🗑️ del producto |
| **Limpiar** | Vacía todo el carrito y reinicia |

### Venta con Código de Barras

1. Enfoque el cursor en el campo de búsqueda (se enfoca automáticamente)
2. Escanee el código de barras con el lector
3. El producto se agrega automáticamente al carrito
4. Repita para todos los productos
5. Presione Enter o haga clic en **Cobrar**

### Cerrar Caja

1. Vaya a **POS > Cierre de Caja**
2. El sistema mostrará:
   - Ventas del período
   - Total en efectivo
   - Total en tarjetas
   - Monto inicial
   - Saldo esperado
3. Ingrese el monto real contado (para detectar diferencias)
4. Confirme el cierre
5. Imprima el reporte de cierre

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

## 📈 Módulo de Reportes y DGII

### Formatos de Envío DGII (606)

**Ruta:** `Reportes > Formato 606`

Facturapro automatiza la generación del formato 606 exigido por la DGII:
1. Seleccione el período (Mes y Año).
2. El sistema validará todas las compras, NCF válidos, retenciones de ITBIS e ISR.
3. Se mostrará un resumen pre-validado en pantalla para su revisión.
4. Haga clic en **Generar Archivo TXT**.
5. Obtendrá el archivo compatible y listo para subir a la Oficina Virtual de la DGII.

### Dashboard Principal

**Ruta:** `Reportes > Dashboard`

Visualice en tiempo real:

- 📊 Ventas del día, mes y año
- 📈 Gráficos de tendencias de ventas
- 🏆 Productos más vendidos
- 👥 Clientes frecuentes
- ⚠️ Alertas de stock bajo

### Reporte General de Ventas

**Ruta:** `Reportes > Ventas`

1. Seleccione el período (desde/hasta)
2. Filtre por:
   - Tipo de comprobante (E31, E32, etc.)
   - Cliente específico
   - Estado DGII
3. Haga clic en **Generar Reporte**
4. Exporte a PDF o Excel

### Productos Más Vendidos

**Ruta:** `Reportes > Productos Más Vendidos`

Muestra:

- Ranking de productos por cantidad vendida
- Ingresos generados por producto
- Período seleccionado
- Gráfico de barras

### Ventas por Categoría

**Ruta:** `Reportes > Ventas por Categoría`

Visualice:

- Distribución de ventas por categoría
- Porcentaje de participación de cada categoría
- Gráfico circular o de barras

### Ventas por Ciudad

**Ruta:** `Reportes > Ventas por Ciudad`

Analice:

- Distribución geográfica de ventas
- Ciudades con mayor volumen
- Oportunidades de expansión

### Ventas por Año

**Ruta:** `Reportes > Ventas por Año`

Compare:

- Ventas anuales históricas
- Crecimiento año tras año
- Tendencias estacionales

### Reporte de Devoluciones

**Ruta:** `Reportes > Devoluciones`

Detalle de:

- Notas de crédito emitidas
- Motivos de devolución
- Productos devueltos
- Montos totales

### Exportar Reportes a PDF

Todos los reportes pueden exportarse a PDF:

1. Genere el reporte con los filtros deseados
2. Haga clic en **Exportar PDF**
3. El documento incluirá:
   - Encabezado con datos de la empresa
   - Período del reporte
   - Tabla detallada de datos
   - Totales generales
   - Totales generales
   - Fecha de generación

---

## 👥 Gestión de Usuarios y Roles

Facturapro cuenta con un sistema de Control de Acceso Basado en Roles (RBAC) que asegura que cada empleado solo vea y modifique lo que le corresponde.

### Jerarquía de Roles

| Rol | Nivel de Acceso | Menús Visibles |
|-----|-----------------|----------------|
| **Cajero** | Básico | POS, Facturas, Clientes. Ideal para personal de mostrador. |
| **Vendedor** | Intermedio | POS, Facturas, Clientes, Productos, Categorías. |
| **Gerente** | Avanzado | Todo lo anterior + Compras, Inventario (Kalder), Proveedores y Reportes. |
| **Super Admin** | Total | Acceso absoluto incluyendo Configuración General, Usuarios y Firma Digital. |

### Crear un Usuario

**Ruta:** `Configuración > Usuarios` (Solo Super Admin)

1. Haga clic en **Nuevo Usuario**.
2. Complete el Nombre, Correo Electrónico (será su usuario de acceso) y Teléfono.
3. Asigne una **Contraseña Inicial**.
4. Seleccione el **Rol** del empleado desde el menú desplegable.
5. El sistema ajustará automáticamente el menú lateral (Sidebar) cuando este usuario inicie sesión.

---

## 🔧 Solución de Problemas

### Error: "No hay rangos de numeración disponibles"

**Solución:**
1. Vaya a **Configuración** → **Rangos de Numeración**
2. Haga clic en **Nuevo Rango**
3. Complete:
   - Tipo de e-CF (E31, E32, etc.)
   - Rango desde (ej: E31000000001)
   - Rango hasta (ej: E31000001000)
   - Fecha de vencimiento
4. Guarde el rango

### Error: "No hay certificado configurado"

**Solución:**
1. Vaya a **Configuración** → **DGII**
2. En la sección Certificado Digital:
   - Haga clic en **Seleccionar archivo**
   - Elija su archivo .PFX o .P12
   - Ingrese la contraseña
3. Haga clic en **Guardar Certificado**
4. Verifique que el certificado no esté vencido

### Error al enviar a DGII: "Servicios no disponibles"

**Solución:**
1. Verifique su conexión a Internet
2. Visite el portal de estatus DGII: https://statusecf.dgii.gov.do
3. Intente más tarde (los servicios pueden estar en mantenimiento)
4. Use el botón **Verificar Conexión** en Configuración DGII

### Error: "La factura debe tener al menos una línea para firmar"

**Solución:**
1. Abra la factura en modo edición
2. Haga clic en **Agregar Línea**
3. Complete al menos un producto o servicio
4. Guarde e intente firmar nuevamente

### Factura rechazada por la DGII

**Pasos:**
1. Vaya a **Facturas** → **Detalles** de la factura rechazada
2. Revise el **Mensaje DGII** que indica el motivo del rechazo
3. Los errores comunes son:
   - RNC del cliente inválido
   - Totales incorrectos
   - Fecha de vencimiento incorrecta
   - Error en estructura del XML
4. Si el error es de datos, corrija y vuelva a enviar
5. Si el error es técnico, contacte soporte

### Error: "Certificado expirado o inválido"

**Solución:**
1. Verifique la fecha de vencimiento del certificado
2. Si venció, renueve el certificado con la entidad emisora
3. Si es inválido, verifique:
   - La contraseña es correcta
   - El archivo no está corrupto
   - El certificado es emitido por una entidad autorizada por INDOTEL

### Errores comunes y soluciones

| Error | Causa Probable | Solución |
|-------|---------------|----------|
| RNC inválido | Cliente sin RNC o formato incorrecto | Verifique el RNC (9 o 11 dígitos) |
| Total incorrecto | Diferencia en cálculo de ITBIS | Revise las líneas y recalcule totales |
| XML mal formado | Error en estructura de datos | Contacte soporte técnico |
| Certificado expirado | Venció la vigencia | Renueve su certificado digital |
| Sin stock | Producto sin existencia | Permita stock negativo o haga compra |
| Cliente no encontrado | Cliente inactivo o eliminado | Active o cree un nuevo cliente |

### Problemas de Impresión

| Problema | Solución |
|----------|----------|
| PDF no se descarga | Verifique permisos del navegador |
| QR no se ve | Actualice la página e intente de nuevo |
| Impresora térmica no responde | Verifique conexión USB/red y drivers |

---

## ❓ Preguntas Frecuentes

### Facturación Electrónica

#### ¿Cuánto tiempo tarda la DGII en aceptar un comprobante?

- **Facturas de Consumo (E32):** Generalmente 5-30 segundos
- **Facturas de Crédito Fiscal (E31):** Puede tardar hasta 24 horas en proceso de certificación
- **Notas de Crédito/Débito:** Similar a facturas de consumo

#### ¿Qué hago si un comprobante es rechazado?

1. Revise el mensaje de error en el campo "Mensaje DGII"
2. Los errores comunes son:
   - RNC del cliente inválido → Verifique el RNC en la DGII
   - Totales incorrectos → Revise cálculos de ITBIS
   - Fecha incorrecta → Verifique formato de fecha
3. Corrija el error en los datos
4. Vuelva a firmar y enviar

#### ¿Puedo anular un comprobante aceptado por la DGII?

No se puede anular directamente. Debe emitir una **Nota de Crédito (E34)** por el monto total de la factura original.

#### ¿Los comprobantes de consumo necesitan RNC del cliente?

No. Para consumidores finales puede usar:
- RNC genérico: `000000000`
- Cédula del cliente si la tiene
- Dejar en blanco si el sistema lo permite

#### ¿Qué pasa si se agota el rango de numeración?

1. Vaya a **Configuración > Rangos de Numeración**
2. Solicite un nuevo rango a la DGII
3. Registre el nuevo rango en el sistema
4. El sistema usará automáticamente el nuevo rango disponible

### Inventario

#### ¿Qué pasa si intento vender sin stock?

Depende de la configuración en `Configuración > Ventas`:
- Si **Permitir Stock Negativo** está habilitado: La venta se realiza
- Si está deshabilitado: El sistema bloqueará la venta y mostrará un error

#### ¿Cómo corrijo un error de inventario?

Use el módulo de Ajustes de Inventario (Kalder):
1. Vaya a `Kalder > Nuevo Ajuste`
2. Seleccione el producto
3. Indique el tipo de ajuste (Corrección)
4. Ingrese la cantidad correcta
5. Especifique el motivo

### Usuarios y Permisos

#### ¿Puedo tener múltiples administradores?

Sí, pero se recomienda tener solo 1-2 administradores principales por seguridad.

#### ¿Qué pasa si un usuario olvida su contraseña?

El administrador puede restablecer la contraseña:
1. Vaya a `Configuración > Usuarios`
2. Busque el usuario
3. Haga clic en **Cambiar Contraseña**
4. Ingrese la nueva contraseña temporal
5. El usuario debe cambiarla al iniciar sesión

#### ¿Puedo eliminar un usuario?

Solo los administradores pueden eliminar usuarios, con estas restricciones:
- No se puede eliminar el último administrador
- No se puede auto-eliminar
- No se pueden eliminar usuarios con transacciones asociadas

### Configuración

#### ¿Cada cuánto debo renovar el certificado digital?

Los certificados digitales suelen tener vigencia de **1 a 2 años**. Renueve al menos 30 días antes del vencimiento para evitar interrupciones.

#### ¿Puedo cambiar de ambiente (Test a Producción)?

Sí, pero necesita:
- Certificado digital diferente para cada ambiente
- Rangos de numeración separados
- La configuración se cambia en `Configuración > DGII`

#### ¿Qué datos son obligatorios para emitir e-CF?

- RNC Emisor configurado
- Certificado digital válido
- Rangos de numeración activos
- Cliente con identificación válida
- Al menos una línea de detalle

---

## 📞 Soporte Técnico

### Antes de Contactar Soporte

1. ✅ Verifique su conexión a internet
2. ✅ Confirme que el certificado no haya vencido
3. ✅ Revise los mensajes de error completos
4. ✅ Intente en otro navegador (Chrome, Firefox, Edge)
5. ✅ Consulte este manual en la sección "Solución de Problemas"

### Información para Soporte

Cuando contacte soporte, proporcione la siguiente información:

- 📋 Número de comprobante o factura afectada
- 📸 Captura de pantalla del error completo
- 🕐 Hora exacta del incidente
- 🔍 Navegador y versión utilizados
- 👤 Usuario que experimenta el problema

### Canales de Soporte

| Canal | Contacto | Horario |
|-------|----------|---------|
| **Email** | soporte@facturapro.do | 24/7 |
| **Teléfono** | (809) 555-0000 | Lun-Vie, 8AM-6PM |
| **WhatsApp** | (809) 555-0001 | Lun-Vie, 8AM-6PM |

### Tiempos de Respuesta

| Prioridad | Tiempo de Respuesta |
|-----------|---------------------|
| **Crítica** (Sistema caído) | 2-4 horas |
| **Alta** (Funcionalidad bloqueada) | 8-12 horas |
| **Media** (Error menor) | 24-48 horas |
| **Baja** (Consulta general) | 3-5 días |

---

## 📋 Glosario

| Término | Significado |
|---------|-------------|
| **e-CF** | Comprobante Fiscal Electrónico |
| **DGII** | Dirección General de Impuestos Internos |
| **ITBIS** | Impuesto a la Transferencia de Bienes Industrializados y Servicios (18% en RD) |
| **NCF** | Número de Comprobante Fiscal |
| **RNC** | Registro Nacional de Contribuyentes |
| **Cédula** | Documento de identidad personal (11 dígitos) |
| **TrackId** | Código de seguimiento de envío a DGII |
| **XML** | Lenguaje de Marcado Extensible - formato de comprobantes |
| **PDF** | Formato de Documento Portátil - representación impresa |
| **PFX/P12** | Formato de certificado digital con clave privada |
| **QR** | Código de Respuesta Rápida - verificación de autenticidad |
| **e-CF FC** | Factura de Consumo (monto menor a RD$250,000) |
| **TesteCF** | Ambiente de pruebas de la DGII |
| **CerteCF** | Ambiente de certificación formal |
| **INDOTEL** | Instituto Dominicano de las Telecomunicaciones - emite certificados |

---

## 📄 Documentación Relacionada

| Documento | Descripción |
|-----------|-------------|
| [CLAUDE.md](./CLAUDE.md) | Memoria del proyecto y decisiones arquitectónicas |
| [README.md](./README.md) | Información general del proyecto |
| **Portal DGII** | https://www.dgii.gov.do |
| **Verificación e-CF** | https://ecf.dgii.gov.do |
| **Estatus Servicios** | https://statusecf.dgii.gov.do |

---

## 📝 Historial de Versiones del Manual

| Versión | Fecha | Cambios |
|---------|-------|---------|
| 1.0 | Abril 2026 | Versión inicial con módulos básicos |
| 1.1 | Abril 2026 | Actualizado con flujo completo de e-CF, módulo Kalder, reportes y POS |
| 1.2 | Mayo 2026 | Agregado diseño Elite UI, soporte Bimonetario (USD/DOP), sistema de Roles (RBAC), formato 606 y optimización de seguridad. |

---

**© 2026 Facturapro - Sistema de Facturación Electrónica**

> 💡 **Consejo:** Guarde este manual como marcador en su navegador para consulta rápida. Para imprimir, use la opción de impresión de su navegador (Ctrl+P).
