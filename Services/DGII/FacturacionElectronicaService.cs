using Facturapro.Models.Entities;
using System.Xml;
using System.Xml.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Facturapro.Services.DGII
{
    /// <summary>
    /// Servicio para generación y firma de e-CF según normativa DGII República Dominicana
    /// </summary>
    public class FacturacionElectronicaService
    {
        private readonly ILogger<FacturacionElectronicaService> _logger;
        private readonly ConfiguracionEmpresa _config;

        public FacturacionElectronicaService(
            ILogger<FacturacionElectronicaService> logger,
            ConfiguracionEmpresa config)
        {
            _logger = logger;
            _config = config;
        }

        /// <summary>
        /// Genera el XML del e-CF en formato DGII
        /// </summary>
        public XDocument GenerarXMLECF(Factura factura, Cliente cliente)
        {
            if (factura.Lineas == null || !factura.Lineas.Any())
                throw new InvalidOperationException("La factura debe tener al menos una línea");

            if (string.IsNullOrEmpty(factura.eNCF))
                throw new InvalidOperationException("El número e-CF es requerido");

            // Crear el documento XML con encoding UTF-8
            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null)
            );

            // Elemento raíz ECF
            var ecf = new XElement("ECF");

            // Agregar namespace si es necesario
            // ecf.SetAttributeValue(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance");

            // 1. ENCABEZADO
            var encabezado = GenerarEncabezado(factura, cliente);
            ecf.Add(encabezado);

            // 2. DETALLES DE ITEMS
            var detallesItems = GenerarDetallesItems(factura.Lineas);
            ecf.Add(detallesItems);

            // 3. SUBTOTALES (opcional pero recomendado)
            var subtotales = GenerarSubtotales(factura);
            if (subtotales != null)
                ecf.Add(subtotales);

            // 4. FECHA Y HORA DE FIRMA (se agrega después de la firma)
            // var fechaHoraFirma = new XElement("FechaHoraFirma", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
            // ecf.Add(fechaHoraFirma);

            doc.Add(ecf);
            return doc;
        }

        /// <summary>
        /// Genera la sección Encabezado del XML
        /// </summary>
        private XElement GenerarEncabezado(Factura factura, Cliente cliente)
        {
            var encabezado = new XElement("Encabezado");

            // Versión del XML
            encabezado.Add(new XElement("Version", factura.VersionXML ?? "1.0"));

            // IdDoc - Información del Documento
            var idDoc = new XElement("IdDoc");
            idDoc.Add(new XElement("TipoeCF", factura.TipoECF ?? "31"));
            idDoc.Add(new XElement("eNCF", factura.eNCF));
            idDoc.Add(new XElement("FechaVencimientoSecuencia",
                factura.FechaVencimiento.ToString("dd-MM-yyyy")));
            idDoc.Add(new XElement("TipoIngresos", factura.TipoIngresos ?? "01"));
            idDoc.Add(new XElement("TipoPago", factura.TipoPago.ToString()));

            // TablaFormasPago si aplica
            if (factura.TipoPago == 1) // Contado
            {
                var tablaFormasPago = new XElement("TablaFormasPago");
                var formaPago = new XElement("FormaPago");
                formaPago.Add(new XElement("TipoPago", "1")); // 1=Efectivo, 2=Cheque, 3=Tarjeta, etc.
                formaPago.Add(new XElement("MontoPago", FormatearDecimal(factura.Total)));
                tablaFormasPago.Add(formaPago);
                idDoc.Add(tablaFormasPago);
            }

            encabezado.Add(idDoc);

            // Emisor
            var emisor = new XElement("Emisor");
            emisor.Add(new XElement("RNCEmisor", _config.RNCEmisor));
            emisor.Add(new XElement("RazonSocialEmisor", _config.RazonSocialEmisor));
            emisor.Add(new XElement("DireccionEmisor", _config.DireccionEmisor));
            emisor.Add(new XElement("FechaEmision", factura.FechaEmision.ToString("dd-MM-yyyy")));

            if (!string.IsNullOrEmpty(_config.Municipio))
                emisor.Add(new XElement("Municipio", _config.Municipio));

            if (!string.IsNullOrEmpty(_config.Provincia))
                emisor.Add(new XElement("Provincia", _config.Provincia));

            encabezado.Add(emisor);

            // Comprador
            var comprador = new XElement("Comprador");
            comprador.Add(new XElement("RNCComprador", cliente.RNC ?? "000000000"));
            comprador.Add(new XElement("RazonSocialComprador", cliente.Nombre));

            if (!string.IsNullOrEmpty(cliente.Direccion))
                comprador.Add(new XElement("DireccionComprador", cliente.Direccion));

            encabezado.Add(comprador);

            // Totales
            var totales = new XElement("Totales");
            totales.Add(new XElement("MontoGravadoTotal", FormatearDecimal(factura.Subtotal)));
            totales.Add(new XElement("MontoGravadoI1", FormatearDecimal(factura.Subtotal)));
            totales.Add(new XElement("MontoExento", "0.00"));
            totales.Add(new XElement("ITBIS1", FormatearDecimal(factura.TotalITBIS)));
            totales.Add(new XElement("TotalITBIS", FormatearDecimal(factura.TotalITBIS)));
            totales.Add(new XElement("MontoTotal", FormatearDecimal(factura.Total)));
            totales.Add(new XElement("MontoNoFacturable", "0.00"));

            encabezado.Add(totales);

            return encabezado;
        }

        /// <summary>
        /// Genera la sección DetallesItems
        /// </summary>
        private XElement GenerarDetallesItems(ICollection<FacturaLinea> lineas)
        {
            var detallesItems = new XElement("DetallesItems");

            foreach (var linea in lineas.OrderBy(l => l.NumeroLinea))
            {
                var item = new XElement("Item");

                item.Add(new XElement("NumeroLinea", linea.NumeroLinea));
                item.Add(new XElement("IndicadorFacturacion", linea.IndicadorFacturacion));
                item.Add(new XElement("NombreItem", linea.NombreItem ?? linea.Descripcion));
                item.Add(new XElement("IndicadorBienoServicio", linea.IndicadorBienoServicio));
                item.Add(new XElement("CantidadItem", FormatearDecimal(linea.Cantidad)));

                if (!string.IsNullOrEmpty(linea.UnidadMedida))
                    item.Add(new XElement("UnidadMedida", linea.UnidadMedida));

                item.Add(new XElement("PrecioUnitarioItem", FormatearDecimal(linea.PrecioUnitario)));
                item.Add(new XElement("MontoItem", FormatearDecimal(linea.Subtotal)));

                // ITBIS por línea
                if (linea.MontoITBIS > 0)
                {
                    item.Add(new XElement("MontoITBIS", FormatearDecimal(linea.MontoITBIS)));
                }

                detallesItems.Add(item);
            }

            return detallesItems;
        }

        /// <summary>
        /// Genera la sección Subtotales (opcional)
        /// </summary>
        private XElement? GenerarSubtotales(Factura factura)
        {
            // Esta sección es opcional según la DGII
            // Se puede agregar información adicional de subtotales aquí
            return null;
        }

        /// <summary>
        /// Firma el XML con certificado digital X.509
        /// </summary>
        public async Task<string> FirmarXMLAsync(XDocument xmlDoc, string certificadoPath, string password)
        {
            try
            {
                // Cargar certificado
                var certificado = new X509Certificate2(certificadoPath, password,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

                // Verificar que el certificado tenga clave privada
                if (!certificado.HasPrivateKey)
                    throw new InvalidOperationException("El certificado no contiene la clave privada");

                // Convertir XML a string
                var xmlString = xmlDoc.ToString();

                // Crear el envelope de firma (implementación básica)
                // En producción, usar una librería como BouncyCastle o System.Security.Cryptography.Xml
                var signedXml = await Task.Run(() =>
                {
                    return FirmarConCertificado(xmlString, certificado);
                });

                return signedXml;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al firmar el XML");
                throw;
            }
        }

        /// <summary>
        /// Implementación básica de firma digital
        /// En producción, usar implementación completa según especificaciones DGII
        /// </summary>
        private string FirmarConCertificado(string xmlContent, X509Certificate2 certificado)
        {
            // NOTA: Esta es una implementación simplificada
            // Para producción, se debe implementar la firma XML-DSig según especificaciones DGII

            var doc = new XmlDocument();
            doc.LoadXml(xmlContent);

            // Agregar nodo de firma (implementación completa requeriría System.Security.Cryptography.Xml)
            var firmaNode = doc.CreateElement("FirmaDigital");
            firmaNode.InnerText = Convert.ToBase64String(certificado.GetRawCertData());
            doc.DocumentElement?.AppendChild(firmaNode);

            // Agregar fecha de firma
            var fechaFirma = doc.CreateElement("FechaHoraFirma");
            fechaFirma.InnerText = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
            doc.DocumentElement?.AppendChild(fechaFirma);

            return doc.OuterXml;
        }

        /// <summary>
        /// Genera el código QR para la representación impresa
        /// </summary>
        public string GenerarCodigoQR(Factura factura)
        {
            // Formato del QR según DGII:
            // RNCEmisor_eNCF_FechaEmision_MontoTotal
            var qrContent = $"{_config.RNCEmisor}_{factura.eNCF}_{factura.FechaEmision:dd-MM-yyyy}_{FormatearDecimal(factura.Total)}";

            // En producción, usar librería como QRCoder para generar imagen
            return qrContent;
        }

        /// <summary>
        /// Genera el número de e-CF con el formato E + Tipo + Secuencial
        /// </summary>
        public static string GenerarNumeroECF(string tipoECF, long secuencial)
        {
            // Formato: E + Tipo (2 dígitos) + Secuencial (10 dígitos)
            // Ejemplo: E310000000001
            return $"E{tipoECF}{secuencial:D10}";
        }

        /// <summary>
        /// Valida que el XML cumpla con el esquema XSD de la DGII
        /// </summary>
        public bool ValidarEsquemaXML(string xmlContent, string xsdPath)
        {
            try
            {
                var settings = new XmlReaderSettings();
                settings.ValidationType = ValidationType.Schema;
                settings.Schemas.Add("", xsdPath);

                using (var reader = XmlReader.Create(new StringReader(xmlContent), settings))
                {
                    while (reader.Read()) { }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error de validación XSD");
                return false;
            }
        }

        /// <summary>
        /// Formatea un decimal al formato requerido por DGII (punto como separador decimal)
        /// </summary>
        private static string FormatearDecimal(decimal valor)
        {
            return valor.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
