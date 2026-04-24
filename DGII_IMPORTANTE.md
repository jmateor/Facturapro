# 🏛️ NOTAS IMPORTANTES - Módulo DGII (Facturación Electrónica)

> **Documento crítico para implementación y operación**  
> **Última actualización:** 20 Abril 2026  
> **Versión DGII API:** 1.0 (Mayo 2023)

---

## ⚠️ ADVERTENCIAS CRÍTICAS

### 🔴 NO UTILIZAR EN PRODUCCIÓN SIN VALIDAR

1. **Certificado Digital**
   - El certificado debe contener la CLAVE PRIVADA
   - Debe estar en formato .PFX o .P12
   - Debe ser emitido por entidad certificadora autorizada por INDOTEL
   - **Verificación:** Si el certificado no tiene clave privada, la firma fallará silenciosamente

2. **Modo de Operación**
   - **TEST:** Los comprobantes NO tienen validez fiscal
   - **CERTIFICACIÓN:** Requerido antes de pasar a producción
   - **PRODUCCIÓN:** Los comprobantes son reales y vinculantes
   - **⚠️ Una vez en producción, las facturas NO se pueden eliminar**

3. **Rangos de Numeración**
   - Los rangos deben estar previamente autorizados por la DGII
   - No inventar rangos - usar solo los asignados oficialmente
   - Verificar vigencia (fecha de vencimiento del rango)

---

## 📋 FLUJO COMPLETO DE FACTURACIÓN ELECTRÓNICA

```
┌─────────────────────────────────────────────────────────────────┐
│  1. CONFIGURACIÓN INICIAL (Una vez)                             │
│     ├── Registrar datos empresa en DGII                        │
│     ├── Obtener certificado digital autorizado                 │
│     ├── Solicitar rangos de numeración                         │
│     └── Configurar sistema (certificado + rangos)              │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  2. EMISIÓN DE COMPROBANTE                                      │
│     ├── Crear factura en sistema                               │
│     ├── Asignar número e-CF automático                         │
│     └── Estado: PENDIENTE                                       │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  3. FIRMA DIGITAL                                               │
│     ├── Generar XML según esquema DGII                         │
│     ├── Firmar con certificado (XML-DSig)                      │
│     └── Estado: FIRMADO                                         │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  4. ENVÍO A DGII                                                │
│     ├── Obtener semilla de autenticación                       │
│     ├── Firmar semilla con certificado                         │
│     ├── Obtener token JWT (válido 1 hora)                      │
│     ├── Enviar XML firmado a API                               │
│     └── Estado: ENVIADO → EN PROCESO                           │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  5. VALIDACIÓN DGII                                             │
│     ├── DGII valida estructura XML                             │
│     ├── Verifica firma digital                                 │
│     ├── Verifica RNC emisor y comprador                        │
│     └── Respuesta: ACEPTADO / RECHAZADO                        │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  6. POST-PROCESAMIENTO                                          │
│     ├── Si ACEPTADO: Factura tiene validez fiscal              │
│     ├── Generar representación impresa (PDF)                   │
│     ├── Incluir código QR oficial                              │
│     └── Almacenar XML por 10 años (obligatorio)                │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 ESPECIFICACIONES TÉCNICAS DGII

### Endpoints API por Ambiente

| Ambiente | URL Base e-CF | URL Base FC | Propósito |
|----------|---------------|-------------|-----------|
| **TesteCF** | `https://ecf.dgii.gov.do/testecf/` | `https://fc.dgii.gov.do/testecf/` | Desarrollo y pruebas |
| **CerteCF** | `https://ecf.dgii.gov.do/certecf/` | `https://fc.dgii.gov.do/certecf/` | Certificación formal |
| **eCF (Producción)** | `https://ecf.dgii.gov.do/ecf/` | `https://fc.dgii.gov.do/ecf/` | Operación real |

### Autenticación (OAuth 2.0 + Certificado)

#### Paso 1: Obtener Semilla
```http
GET /api/autenticacion/semilla
```
**Respuesta XML:**
```xml
<SemillaModel>
  <Semilla>ValorSemillaAleatorio</Semilla>
  <FechaGeneracion>20-04-2026 10:30:00</FechaGeneracion>
</SemillaModel>
```

#### Paso 2: Firmar Semilla
- Firmar el XML de la semilla con certificado P12 usando XML-DSig
- Algoritmo: RSA-SHA256

#### Paso 3: Obtener Token
```http
POST /api/autenticacion/validarsemilla
Content-Type: application/x-www-form-urlencoded

xml={XML_FIRMADO}
```

**Respuesta JSON:**
```json
{
  "token": "eyJhbGciOiJSUzI1NiIs...",
  "expira": "2026-04-20T11:30:00Z",
  "expedido": "2026-04-20T10:30:00Z"
}
```

> **Nota:** El token expira en 1 hora exacta

### Envío de Comprobante

```http
POST /api/facturaselectronicas
Authorization: Bearer {TOKEN}
Content-Type: multipart/form-data

xml: (archivo XML firmado)
```

**Respuesta Exitosa:**
```json
{
  "trackId": "550e8400-e29b-41d4-a716-446655440000",
  "error": null,
  "mensaje": null
}
```

**Respuesta Error:**
```json
{
  "trackId": null,
  "error": "ERROR_CODIGO",
  "mensaje": "Descripción del error"
}
```

### Consulta de Estado

```http
GET /api/consultas/estado?trackid={TRACKID}
Authorization: Bearer {TOKEN}
```

**Respuesta:**
```json
{
  "estado": "Aceptado",
  "mensajes": [
    { "codigo": "00", "valor": "Comprobante aceptado" }
  ]
}
```

---

## 📊 ESTRUCTURA DEL XML (e-CF)

### Elementos Raíz Requeridos

```xml
<ECF>
  <Encabezado>
    <Version>1.0</Version>
    <IdDoc>
      <TipoeCF>31</TipoeCF>
      <eNCF>E310000000001</eNCF>
      <FechaVencimientoSecuencia>31-12-2026</FechaVencimientoSecuencia>
      <TipoIngresos>01</TipoIngresos>
      <TipoPago>1</TipoPago>
      <TablaFormasPago>
        <FormaPago>
          <TipoPago>1</TipoPago>
          <MontoPago>1000.00</MontoPago>
        </FormaPago>
      </TablaFormasPago>
    </IdDoc>
    <Emisor>
      <RNCEmisor>131703836</RNCEmisor>
      <RazonSocialEmisor>EMPRESA DEMO SRL</RazonSocialEmisor>
      <DireccionEmisor>Calle Principal #123</DireccionEmisor>
      <FechaEmision>20-04-2026</FechaEmision>
    </Emisor>
    <Comprador>
      <RNCComprador>00112345678</RNCComprador>
      <RazonSocialComprador>CLIENTE EJEMPLO SA</RazonSocialComprador>
    </Comprador>
    <Totales>
      <MontoGravadoTotal>847.46</MontoGravadoTotal>
      <MontoGravadoI1>847.46</MontoGravadoI1>
      <MontoExento>0.00</MontoExento>
      <ITBIS1>152.54</ITBIS1>
      <TotalITBIS>152.54</TotalITBIS>
      <MontoTotal>1000.00</MontoTotal>
      <MontoNoFacturable>0.00</MontoNoFacturable>
    </Totales>
  </Encabezado>
  <DetallesItems>
    <Item>
      <NumeroLinea>1</NumeroLinea>
      <IndicadorFacturacion>1</IndicadorFacturacion>
      <NombreItem>Producto Ejemplo</NombreItem>
      <IndicadorBienoServicio>1</IndicadorBienoServicio>
      <CantidadItem>10.00</CantidadItem>
      <UnidadMedida>UNIDAD</UnidadMedida>
      <PrecioUnitarioItem>100.00</PrecioUnitarioItem>
      <MontoItem>1000.00</MontoItem>
      <MontoITBIS>152.54</MontoITBIS>
    </Item>
  </DetallesItems>
</ECF>
```

### Indicadores de Facturación

| Código | Descripción |
|--------|-------------|
| 0 | N/A (Informativo) |
| 1 | Producto con ITBIS (18%) |
| 2 | Servicio con ITBIS (18%) |
| 3 | Producto sin ITBIS |
| 4 | Servicio sin ITBIS |
| 5 | Producto con ITBIS (10%) - Turismo hospedaje |
| 6 | Producto con ITBIS (8%) - Combustibles |

### Tipos de Pago

| Código | Descripción |
|--------|-------------|
| 1 | Contado |
| 2 | Crédito |
| 3 | Gratuito |

---

## ⚡ FORMATO E-NCF

```
E + [Tipo 2 dígitos] + [Secuencial 10 dígitos]
```

**Ejemplo:**
- `E310000000001` = Factura de Crédito Fiscal #1
- `E320000000001` = Factura de Consumo #1

**Tipos de Comprobante:**

| Tipo | Descripción | Uso Recomendado |
|------|-------------|-----------------|
| 31 | Factura de Crédito Fiscal | Clientes con RNC (deducible) |
| 32 | Factura de Consumo | Clientes finales (no deducible) |
| 33 | Nota de Débito | Aumentar valor factura |
| 34 | Nota de Crédito | Disminuir valor factura |
| 41 | Comprobante de Compras | Gastos sin factura formal |
| 43 | Gastos Menores | Hasta RD$50,000 anuales |
| 44 | Regímenes Especiales | Agro, zonas francas |
| 45 | Gubernamental | Entidades públicas |
| 46 | Exportaciones | Ventas internacionales |
| 47 | Pagos al Exterior | Servicios no residentes |

---

## 🚨 ERRORES COMUNES Y SOLUCIONES

### Error de Autenticación

**Síntoma:** `Error al obtener semilla` o `Token inválido`

**Causas:**
1. Certificado expirado
2. Certificado sin clave privada
3. Contraseña del certificado incorrecta
4. Servicios DGII caídos

**Solución:**
```csharp
// Verificar certificado
var cert = new X509Certificate2("ruta.pfx", "password");
bool valido = cert.HasPrivateKey && cert.NotAfter > DateTime.Now;
```

### Error de Envío

**Síntoma:** `TrackId` es null o error específico

**Códigos de Error Comunes:**

| Código | Descripción | Solución |
|--------|-------------|----------|
| RNC_INVALIDO | RNC del emisor o comprador incorrecto | Verificar RNCs |
| TOTAL_INCORRECTO | Suma de líneas ≠ Total en encabezado | Recalcular totales |
| SECUENCIA_INVALIDA | Número e-CF fuera de rango | Verificar rangos |
| FIRMA_INVALIDA | XML mal firmado | Verificar certificado |
| XML_MAL_FORMADO | Estructura XML incorrecta | Validar contra XSD |

### Error de Estructura XML

**Validación con XSD:**
```csharp
var settings = new XmlReaderSettings();
settings.ValidationType = ValidationType.Schema;
settings.Schemas.Add("", "ecf.xsd");
settings.ValidationEventHandler += (s, e) => Console.WriteLine(e.Message);

using var reader = XmlReader.Create(xmlStream, settings);
while (reader.Read()) { }
```

---

## 📅 CRONOGRAMA DE OBLIGATORIEDAD

| Fecha | Grupo de Contribuyentes |
|-------|------------------------|
| **15 Nov 2025** | Grandes contribuyentes y medianos nacionales |
| **15 May 2026** | Pequeños y micro contribuyentes |

> **Fuente:** Ley 32-23, Artículo 7

---

## 💾 RESPALDOS Y ALMACENAMIENTO

### Requisitos Legales

1. **Almacenamiento XML:** 10 años obligatorio
2. **Integridad:** No modificar archivos después de emitidos
3. **Disponibilidad:** Debe poder consultarse en cualquier momento
4. **Backup:** Respaldo en ubicación segura diferente al servidor principal

### Estructura de Archivos Recomendada

```
/Almacenamiento/
  /XML/
    /2026/
      /04/
        131703836E3100000001.xml
        131703836E3100000002.xml
      /05/
        ...
  /PDF/
    /2026/
      /04/
        131703836E3100000001.pdf
  /Certificados/
    certificado_empresa.pfx
    certificado_backup.pfx
```

---

## 🔐 SEGURIDAD

### Buenas Prácticas

1. **Certificado Digital**
   - Guardar en directorio protegido (`wwwroot/certificados/`)
   - Permisos de lectura solo para aplicación
   - Backup encriptado en ubicación segura
   - Nunca versionar/certificado en git

2. **Contraseñas**
   - La contraseña del certificado se almacena en base de datos
   - Considerar encriptar campo `PasswordCertificado`
   - Rotar contraseñas periódicamente

3. **Logs**
   - Registrar todas las llamadas a API DGII
   - No loggear contraseñas ni tokens completos
   - Retención de logs: 1 año mínimo

### Variables de Entorno Recomendadas

```bash
# appsettings.Production.json
{
  "DGII": {
    "RncEmisor": "131703836",
    "CertificadoPath": "/secure/certificado.pfx",
    "CertificadoPassword": "[ENCRYPTED]",
    "Ambiente": "Produccion"
  }
}
```

---

## 📞 CONTACTOS DGII

### Portal Oficial
- **Web:** https://dgii.gov.do
- **Facturación Electrónica:** https://dgii.gov.do/cicloContribuyente/facturacion/comprobantesFiscalesElectronicosE-CF

### Soporte Técnico DGII
- **Email:** ecf@dgii.gov.do
- **Teléfono:** (809) 689-XXXX

### Proveedores Autorizados (Alternativas)
- **Alanube:** https://alanube.co/rd/
- **ef2:** https://ef2.do
- **DGMax:** https://dgmax.do

---

## 📚 REFERENCIAS

1. [Descripción Técnica de Facturación Electrónica v1.5 - DGII](https://dgii.gov.do)
2. [Informe Técnico e-CF v1.0](https://dgii.gov.do)
3. [XSD Esquemas de Validación](https://dgii.gov.do)
4. [Comunidad de Ayuda DGII](https://ayuda.dgii.gov.do)

---

## ✅ CHECKLIST PARA PRODUCCIÓN

Antes de pasar a producción, verificar:

- [ ] Certificado digital vigente con clave privada
- [ ] Rangos de numeración activos y vigentes
- [ ] Datos de empresa registrados en DGII
- [ ] Pruebas exitosas en ambiente TesteCF
- [ ] Pruebas exitosas en ambiente CerteCF
- [ ] Backup de base de datos configurado
- [ ] Almacenamiento XML 10 años garantizado
- [ ] Manual de usuario actualizado
- [ ] Capacitación del personal completada
- [ ] Plan de contingencia documentado

---

**⚠️ ESTE DOCUMENTO ES CONFIDENCIAL Y CRÍTICO PARA LA OPERACIÓN**

Mantener actualizado con cada cambio en la implementación de DGII.

**Última revisión:** 20 Abril 2026  
**Próxima revisión recomendada:** Cuando DGII actualice sus especificaciones
