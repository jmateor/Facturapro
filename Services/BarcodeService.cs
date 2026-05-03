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

        // EAN-13 Patterns
        private static readonly string[] EAN_L = { "0001101", "0011001", "0010011", "0111101", "0100011", "0110001", "0101111", "0111011", "0110111", "0001011" };
        private static readonly string[] EAN_G = { "0100111", "0110011", "0011011", "0100001", "0011101", "0111001", "0000101", "0010001", "0001001", "0010111" };
        private static readonly string[] EAN_R = { "1110010", "1100110", "1101100", "1000010", "1011100", "1001110", "1010000", "1000100", "1001000", "1110100" };
        private static readonly string[] EAN_Parity = { "LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG", "LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL" };

        public string GenerarCodigoBarras(string codigoProducto, int productoId)
        {
            // Si el código ya parece ser un EAN-13 (12 o 13 dígitos numéricos)
            if (codigoProducto.Length >= 12 && codigoProducto.All(char.IsDigit))
            {
                if (codigoProducto.Length == 12)
                {
                    return codigoProducto + CalcularCheckDigitEan13(codigoProducto);
                }
                return codigoProducto;
            }

            // Generar un código de barras único basado en el código del producto para uso interno
            var barcode = $"{codigoProducto}-{productoId:D6}";
            return barcode.Length > 20 ? barcode.Substring(0, 20) : barcode;
        }

        public string GenerarImagenBarcodeSvg(string code)
        {
            if (string.IsNullOrEmpty(code))
                return string.Empty;

            bool isEan13 = code.Length == 13 && code.All(char.IsDigit);
            string encoded = isEan13 ? EncodeEan13(code) : EncodeCode128(code);
            
            var barWidth = 2;
            var height = 80;
            var width = encoded.Length * barWidth;

            var svg = new StringBuilder();
            svg.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height + 25}\" viewBox=\"0 0 {width} {height + 25}\">");
            svg.AppendLine("<rect width=\"100%\" height=\"100%\" fill=\"white\"/>");

            for (int i = 0; i < encoded.Length; i++)
            {
                if (encoded[i] == '1')
                {
                    svg.AppendLine($"<rect x=\"{i * barWidth}\" y=\"0\" width=\"{barWidth}\" height=\"{height}\" fill=\"black\"/>");
                }
            }

            // Texto descriptivo (GS1 Formatting si es EAN-13)
            string displayCode = isEan13 ? $"{code[0]} {code.Substring(1, 6)} {code.Substring(7)}" : code;
            svg.AppendLine($"<text x=\"{width / 2}\" y=\"{height + 18}\" text-anchor=\"middle\" font-family=\"monospace\" font-size=\"14\" font-weight=\"bold\">{displayCode}</text>");
            svg.AppendLine("</svg>");

            return svg.ToString();
        }

        private string EncodeEan13(string code)
        {
            var result = new StringBuilder();
            result.Append("101"); // Left Guard

            int firstDigit = code[0] - '0';
            string parity = EAN_Parity[firstDigit];

            // Left side (6 digits)
            for (int i = 1; i <= 6; i++)
            {
                int digit = code[i] - '0';
                result.Append(parity[i - 1] == 'L' ? EAN_L[digit] : EAN_G[digit]);
            }

            result.Append("01010"); // Center Guard

            // Right side (6 digits)
            for (int i = 7; i <= 12; i++)
            {
                int digit = code[i] - '0';
                result.Append(EAN_R[digit]);
            }

            result.Append("101"); // Right Guard
            return result.ToString();
        }

        private int CalcularCheckDigitEan13(string code12)
        {
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int digit = code12[i] - '0';
                sum += (i % 2 == 0) ? digit : digit * 3;
            }
            int checkDigit = (10 - (sum % 10)) % 10;
            return checkDigit;
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
            if (checksum >= 0 && checksum <= 94)
            {
                char c = (char)(checksum + 32);
                if (Code128Patterns.TryGetValue(c, out string? pattern))
                    return pattern;
            }
            return "1100011101011";
        }
    }
}
