using System.Text;

namespace Facturapro.Services
{
    public interface IBarcodeService
    {
        string GenerarCodigoBarras(string codigoProducto, int productoId);
        string GenerarImagenBarcodeSvg(string code);
    }

    public class BarcodeService : IBarcodeService
    {
        // Code 128 patterns (B subset - most common)
        private static readonly Dictionary<char, string> Code128Patterns = new()
        {
            [' '] = "11011001100", ['!'] = "11001101100", ['"'] = "11001100110",
            ['#'] = "10010011000", ['$'] = "10010001100", ['%'] = "10001001100",
            ['&'] = "10011001000", ['\''] = "10011000100", ['('] = "10001100100",
            [')'] = "11001001000", ['*'] = "11001000100", ['+'] = "11000100100",
            [','] = "10110011100", ['-'] = "10011011100", ['.'] = "10011001110",
            ['/'] = "10111001100", ['0'] = "10011101100", ['1'] = "10011100110",
            ['2'] = "11001110010", ['3'] = "11001011100", ['4'] = "11001001110",
            ['5'] = "11011100100", ['6'] = "11001110100", ['7'] = "11101101110",
            ['8'] = "11101001100", ['9'] = "11100101100", [':'] = "11100100110",
            [';'] = "11101100100", ['<'] = "11100110100", ['='] = "11100110010",
            ['>'] = "11011011000", ['?'] = "11011000110", ['@'] = "11000110100",
            ['A'] = "10100011000", ['B'] = "10001011000", ['C'] = "10001000110",
            ['D'] = "10100001000", ['E'] = "10000101100", ['F'] = "10000100110",
            ['G'] = "10011010000", ['H'] = "10011000010", ['I'] = "10001100010",
            ['J'] = "10110001000", ['K'] = "10001101110", ['L'] = "10111011000",
            ['M'] = "10111000110", ['N'] = "10001110110", ['O'] = "10111101000",
            ['P'] = "10111100010", ['Q'] = "11110101000", ['R'] = "11110100010",
            ['S'] = "10111011110", ['T'] = "10111101110", ['U'] = "11101011110",
            ['V'] = "11110101110", ['W'] = "11010011110", ['X'] = "11011101010",
            ['Y'] = "11011101110", ['Z'] = "11101011010", ['['] = "11101011110",
            ['\\'] = "11101101010", [']'] = "11101101110", ['^'] = "11011011110",
            ['_'] = "11011110110", ['`'] = "11101101110", ['a'] = "11001000010",
            ['b'] = "11110101010", ['c'] = "10111011110", ['d'] = "10111101110",
            ['e'] = "11101011110", ['f'] = "11110101110", ['g'] = "11010011110",
            ['h'] = "11011101010", ['i'] = "11011101110", ['j'] = "11101011010",
            ['k'] = "11101011110", ['l'] = "11101101010", ['m'] = "11101101110",
            ['n'] = "11011011110", ['o'] = "11011110110", ['p'] = "11101101110",
            ['q'] = "11001110010", ['r'] = "11001110010", ['s'] = "11001110010",
            ['t'] = "11001110010", ['u'] = "11001110010", ['v'] = "11001110010",
            ['w'] = "11001110010", ['x'] = "11001110010", ['y'] = "11001110010",
            ['z'] = "11001110010", ['{'] = "11001110010", ['|'] = "11001110010",
            ['}'] = "11001110010", ['~'] = "11001110010"
        };

        private const string StartB = "11010010000";
        private const string StopPattern = "1100011101011";

        public string GenerarCodigoBarras(string codigoProducto, int productoId)
        {
            // Generar un código de barras único basado en el código del producto
            // Formato: PROD + ID del producto (rellenado con ceros) + código
            var barcode = $"{codigoProducto}-{productoId:D6}";
            return barcode.Length > 20 ? barcode.Substring(0, 20) : barcode;
        }

        public string GenerarImagenBarcodeSvg(string code)
        {
            if (string.IsNullOrEmpty(code))
                return string.Empty;

            var encoded = EncodeCode128(code);
            var barWidth = 2;
            var height = 80;
            var width = encoded.Length * barWidth;

            var svg = new StringBuilder();
            svg.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height + 20}\" viewBox=\"0 0 {width} {height + 20}\">");
            svg.AppendLine("<rect width=\"100%\" height=\"100%\" fill=\"white\"/>");

            int x = 0;
            for (int i = 0; i < encoded.Length; i++)
            {
                if (encoded[i] == '1')
                {
                    svg.AppendLine($"<rect x=\"{x * barWidth}\" y=\"0\" width=\"{barWidth}\" height=\"{height}\" fill=\"black\"/>");
                }
                x++;
            }

            // Añadir texto del código debajo
            svg.AppendLine($"<text x=\"{width / 2}\" y=\"{height + 15}\" text-anchor=\"middle\" font-family=\"Arial, sans-serif\" font-size=\"12\">{code}</text>");
            svg.AppendLine("</svg>");

            return svg.ToString();
        }

        private string EncodeCode128(string code)
        {
            var result = new StringBuilder();
            result.Append(StartB);

            int checksum = 104; // Start B value

            for (int i = 0; i < code.Length; i++)
            {
                char c = code[i];
                if (Code128Patterns.TryGetValue(c, out string? pattern))
                {
                    result.Append(pattern);
                    checksum += (c - 32) * (i + 1);
                }
            }

            checksum = checksum % 103;
            result.Append(GetChecksumPattern(checksum));
            result.Append(StopPattern);

            return result.ToString();
        }

        private string GetChecksumPattern(int checksum)
        {
            // Simplified - map checksum to a pattern (Code 128 B subset)
            // This is a basic implementation
            if (checksum >= 0 && checksum <= 94)
            {
                char c = (char)(checksum + 32);
                if (Code128Patterns.TryGetValue(c, out string? pattern))
                    return pattern;
            }
            return "1100011101011"; // Default to stop pattern variant
        }
    }
}
