# Guía de Implementación a Producción - Facturación Electrónica DGII

## Sistema Facturapro - República Dominicana

---

## FASE 1: Preparación (Semanas 1-2)

### 1.1 Requisitos Legales y Tributarios

| Requisito | Descripción | Tiempo |
|-----------|-------------|--------|
| **RNC Activo** | Verificar estado en Oficina Virtual DGII | 1 día |
| **Contribuyente al Día** | Sin deudas pendientes | Verificación inmediata |
| **Correo en DGII** | Tener correo registrado y activo | 1 día |

**Acciones:**
- Acceder a [Oficina Virtual DGII](https://dgii.gov.do)
- Verificar estado de RNC
- Actualizar correo electrónico si es necesario

---

### 1.2 Infraestructura Técnica

```
Requisitos del Servidor:
✓ Windows Server 2019+ o Linux Ubuntu 20.04+
✓ .NET 8 Runtime instalado
✓ SQL Server 2019+ o PostgreSQL 13+
✓ Certificado SSL válido (HTTPS)
✓ IP Pública Estática
✓ Backup automatizado diario
```

**Configuración de Red:**
- Puerto 443 (HTTPS)
- Puerto 80 (HTTP redirect)
- Puerto 1433 (SQL Server, si es externo)

---

## FASE 2: Certificación Digital (Semana 2-3)

### 2.1 Obtener Certificado Digital

**Proveedores Autorizados (INDOTEL):**

| Proveedor | Costo Aprox. | Tiempo |
|-----------|--------------|--------|
| **CAMARDOM** | RD$ 2,500-5,000/año | 3-5 días |
| **Cámara de Comercio Sto. Dgo.** | RD$ 3,000-6,000/año | 3-5 días |
| **Datacert** | RD$ 2,800-4,500/año | 2-4 días |

**Documentos Requeridos:**
- [ ] Copia Cédula/RNC del solicitante
- [ ] Copia Cédula/RNC del representante legal
- [ ] Registro Mercantil (vigente)
- [ ] Carta de autorización

---

## FASE 3: Rangos de Numeración (Semana 3)

### 3.1 Solicitar Rangos a la DGII

**Pasos en Portal DGII:**

1. Acceder a [Portal e-CF DGII](https://dgii.gov.do/ecf)
2. Menú: **Contribuyente → e-CF → Solicitud de Rangos**
3. Completar formulario:
   - Tipo de Comprobante (31, 32, etc.)
   - Cantidad de comprobantes solicitados
   - Justificación

### 3.2 Configurar Rangos en el Sistema

En el módulo **Rangos de Numeración** del sistema:
- Tipo de Comprobante: E31, E32, etc.
- Rango Desde: Ej. E310000000001
- Rango Hasta: Ej. E310000100000
- Fecha de Vencimiento: Según autorización DGII

---

## FASE 4: Pruebas y Certificación (Semanas 4-5)

### 4.1 Set de Pruebas DGII

**Requisito:** Enviar mínimo **25 documentos electrónicos** al ambiente de pruebas

**Casos de Prueba Recomendados:**

| Tipo | Escenario | Valor |
|------|-----------|-------|
| E31 | Factura normal con ITBIS 18% | RD$ 1,000 + ITBIS |
| E31 | Factura con descuento | RD$ 2,000 - 10% |
| E31 | Factura con múltiples líneas | 5+ líneas |
| E32 | Factura de consumo | RD$ 500 |
| E33 | Nota de débito | +RD$ 200 |
| E34 | Nota de crédito total | -RD$ 1,000 |
| E34 | Nota de crédito parcial | -50% |
| E41 | Compra a proveedor informal | RD$ 5,000 |

### 4.2 Validaciones XML

Descargar esquemas XSD oficiales de DGII:
- ecf_31_v1.0.xsd
- ecf_32_v1.0.xsd
- ecf_33_v1.0.xsd
- ecf_34_v1.0.xsd

---

## FASE 5: Declaración y Habilitación (Semana 5-6)

### 5.1 Documentos a Presentar

| Documento | Descripción |
|-----------|-------------|
| **Declaración Jurada** | Compromiso de cumplimiento (formato DGII) |
| **Set de Pruebas** | Confirmación de 25+ documentos enviados |
| **Constancia de Certificado** | De entidad autorizada |
| **Resumen de Rangos** | Autorización de rangos de numeración |

### 5.2 Proceso en Portal DGII

```
Menú: Facturación Electrónica → Habilitación de Emisor
├── 1. Completar cuestionario
├── 2. Subir documentos (PDF firmados)
├── 3. Confirmar datos del sistema
└── 4. Enviar solicitud
```

**Tiempo de respuesta DGII:** 5-10 días hábiles

---

## FASE 6: Go Live (Semana 6+)

### 6.1 Checklist Pre-Go Live

- [ ] Certificado válido (más de 30 días de vigencia)
- [ ] Rangos activos disponibles (más de 100)
- [ ] Configuración RNC completa
- [ ] Backup configurado y probado
- [ ] Modo cambiado a Producción
- [ ] URL DGII producción configurada
- [ ] Logs habilitados
- [ ] Notificaciones configuradas

### 6.2 Primer e-CF en Producción

Recomendación: Emitir una factura de prueba pequeña primero:
1. Crear factura de RD$ 100
2. Generar XML
3. Firmar digitalmente
4. Enviar a DGII
5. Verificar respuesta
6. Confirmar recepción del e-NCF

---

## FASE 7: Post-Implementación

### 7.1 Monitoreo Continuo

| Métrica | Alerta |
|---------|--------|
| Rangos disponibles | < 100 |
| Certificado días | < 30 |
| Facturas rechazadas | > 5% |
| Tiempo respuesta DGII | > 10 seg |

### 7.2 Reportes Mensuales DGII

- **606** - Comprobantes de Retención
- **607** - Comprobantes de Ventas (e-CF emitidos)
- **608** - Comprobantes de Compras
- **IT-1** - Declaración de Impuestos

---

## Configuración appsettings.json (Producción)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=produccion-db;Database=FacturaproProd;..."
  },
  "DGII": {
    "ModoPruebas": false,
    "UrlProduccion": "https://ecf.dgii.gov.do/prod/v1",
    "UrlPruebas": "https://ecf.dgii.gov.do/test/v1",
    "TimeoutSegundos": 30,
    "Reintentos": 3
  },
  "Certificado": {
    "Ruta": "/seguro/certificado.pfx",
    "DiasAlertaVencimiento": 30
  }
}
```

---

## Contactos de Soporte

| Entidad | Contacto | Uso |
|---------|----------|-----|
| **DGII** | 809-689-3366 | Problemas técnicos e-CF |
| **DGII** | ecf@dgii.gov.do | Consultas de facturación |
| **CAMARDOM** | 809-567-8899 | Certificados digitales |

---

## Timeline Resumen

```
Semana 1-2:  [████] Preparación
Semana 2-3:  [████] Certificado Digital
Semana 3:    [██]   Rangos DGII
Semana 4-5:  [████] Pruebas y Certificación
Semana 5-6:  [██]   Declaración DGII
Semana 6+:   [░░]   Producción + Monitoreo
```

**Tiempo total estimado:** 6-8 semanas

---

## Notas Importantes

- El sistema está diseñado siguiendo mejores prácticas de facturación electrónica
- Compatible con requisitos de la Ley 32-23
- Soporta todos los tipos de comprobantes e-CF (E31-E47)
- Implementación propia, sin dependencia de terceros para operación
