using System.Net.Http.Json;
using ZENIT.Blazor.Models;

namespace ZENIT.Blazor.Services;

public class RadioApiService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://de1.api.radio-browser.info/json";

    public RadioApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Ejecuta una petición HTTP con lógica de reintentos (Retry Policy)
    /// </summary>
    private async Task<List<RadioStation>> ExecuteWithRetryAsync(string url, CancellationToken cancellationToken = default)
    {
        int maxRetries = 3;
        int delayMs = 1000;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<RadioStation>>(url, cancellationToken);
                return response ?? new List<RadioStation>();
            }
            catch (TaskCanceledException)
            {
                // Si la cancelación vino desde el usuario (ej. cambió de página), salimos.
                if (cancellationToken.IsCancellationRequested) return new List<RadioStation>();
                
                // Si fue por Timeout del HttpClient, intentamos de nuevo.
                Console.WriteLine($"Timeout de API (Intento {i + 1}/{maxRetries}). Reintentando...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error API: {ex.Message} (Intento {i + 1}/{maxRetries})");
            }

            if (i < maxRetries - 1 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(delayMs, cancellationToken);
            }
        }

        return new List<RadioStation>();
    }

    public Task<List<RadioStation>> GetTopVotedStationsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/stations/search?order=votes&reverse=true&hidebroken=true&is_https=true&limit={limit}";
        return ExecuteWithRetryAsync(url, cancellationToken);
    }

    public Task<List<RadioStation>> SearchStationsAsync(string query, int limit = 50, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Task.FromResult(new List<RadioStation>());

        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"{BaseUrl}/stations/search?name={encodedQuery}&order=votes&reverse=true&hidebroken=true&is_https=true&limit={limit}";
        return ExecuteWithRetryAsync(url, cancellationToken);
    }

    public Task<List<RadioStation>> GetStationsByCountryAsync(string country, int limit = 20, CancellationToken cancellationToken = default)
    {
        var encodedCountry = Uri.EscapeDataString(country);
        var url = $"{BaseUrl}/stations/search?country={encodedCountry}&order=votes&reverse=true&hidebroken=true&is_https=true&limit={limit}";
        return ExecuteWithRetryAsync(url, cancellationToken);
    }

    public Task<List<RadioStation>> GetStationsByTagAsync(string tag, int limit = 20, CancellationToken cancellationToken = default)
    {
        var encodedTag = Uri.EscapeDataString(tag);
        var url = $"{BaseUrl}/stations/search?tag={encodedTag}&order=votes&reverse=true&hidebroken=true&is_https=true&limit={limit}";
        return ExecuteWithRetryAsync(url, cancellationToken);
    }
}
