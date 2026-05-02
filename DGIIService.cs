// DGIIService.cs - DGII API Client para C# / .NET
// Uso: var dgii = new DGIIService("tu_api_key");
//      var empresa = await dgii.ConsultarRNC("101001234");

using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace DGII.Api;

public class Contribuyente
{
    [JsonPropertyName("rnc")] public string Rnc { get; set; } = "";
    [JsonPropertyName("nombre")] public string Nombre { get; set; } = "";
    [JsonPropertyName("actividad_economica")] public string? ActividadEconomica { get; set; }
    [JsonPropertyName("estatus")] public string? Estatus { get; set; }
    [JsonPropertyName("categoria")] public string? Categoria { get; set; }
    [JsonPropertyName("regimen")] public string? Regimen { get; set; }
    [JsonPropertyName("provincia")] public string? Provincia { get; set; }
    [JsonPropertyName("municipio")] public string? Municipio { get; set; }
}

public class SearchResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("data")] public List<Contribuyente> Data { get; set; } = new();
    [JsonPropertyName("total")] public int Total { get; set; }
}

public class DGIIService : IDisposable
{
    private readonly HttpClient _client;

    public DGIIService(string apiKey, string apiUrl = "https://pptonanntevatndjyzmk.supabase.co/functions/v1/dgii-api")
    {
        _client = new HttpClient { BaseAddress = new Uri(apiUrl) };
        _client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _client.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<Contribuyente?> ConsultarRNC(string rnc)
        => await _client.GetFromJsonAsync<Contribuyente>($"/rnc/{rnc}");

    public async Task<Contribuyente?> ConsultarCedula(string cedula)
        => await _client.GetFromJsonAsync<Contribuyente>($"/cedula/{cedula}");

    public async Task<SearchResponse?> Buscar(string? q = null, string? provincia = null, int page = 1, int limit = 20)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(q)) queryParams.Add($"q={Uri.EscapeDataString(q)}");
        if (!string.IsNullOrEmpty(provincia)) queryParams.Add($"provincia={Uri.EscapeDataString(provincia)}");
        queryParams.Add($"page={page}&limit={limit}");

        var query = string.Join("&", queryParams);
        return await _client.GetFromJsonAsync<SearchResponse>($"/search?{query}");
    }

    public async Task<SearchResponse?> Autocomplete(string query, string tipo = "rnc", int limit = 5)
        => await _client.GetFromJsonAsync<SearchResponse>(
            $"/autocomplete?q={Uri.EscapeDataString(query)}&tipo={tipo}&limit={limit}");

    public void Dispose() => _client.Dispose();
}
