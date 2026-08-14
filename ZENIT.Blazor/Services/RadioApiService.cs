using System.Net.Http.Json;
using ZENIT.Blazor.Models;

namespace ZENIT.Blazor.Services;

public class RadioApiService
{
    private readonly HttpClient _httpClient;
    private static string[] _knownNodes = new[] {
        "https://de1.api.radio-browser.info",
        "https://nl1.api.radio-browser.info",
        "https://at1.api.radio-browser.info"
    };
    private static int _currentNodeIndex = 0;
    private static bool _nodesFetched = false;

    public RadioApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        
        // Radio-Browser API requires a custom User-Agent to avoid blocks
        if (!_httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("ZENIT.Blazor/1.0"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ZENIT.Blazor/1.0");
        }
    }

    private async Task EnsureNodesAsync(CancellationToken cancellationToken)
    {
        if (_nodesFetched) return;
        
        try
        {
            var servers = await _httpClient.GetFromJsonAsync<List<RadioServer>>("https://all.api.radio-browser.info/json/servers", cancellationToken);
            if (servers != null && servers.Count > 0)
            {
                _knownNodes = servers.Select(s => $"https://{s.Name}").ToArray();
                _currentNodeIndex = new Random().Next(0, _knownNodes.Length);
            }
        }
        catch
        {
            // Fallback a los nodos conocidos si falla la obtención dinámica
        }
        finally
        {
            _nodesFetched = true;
        }
    }

    private void RotateNode()
    {
        _currentNodeIndex = (_currentNodeIndex + 1) % _knownNodes.Length;
    }

    /// <summary>
    /// Ejecuta una petición HTTP con lógica de reintentos (Retry Policy) y rotación de nodos
    /// </summary>
    private async Task<List<RadioStation>> ExecuteWithRetryAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        await EnsureNodesAsync(cancellationToken);

        int maxRetries = 3;
        int delayMs = 1000;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var baseUrl = _knownNodes[_currentNodeIndex];
                var url = $"{baseUrl}{endpoint}";
                var response = await _httpClient.GetFromJsonAsync<List<RadioStation>>(url, cancellationToken);
                return response ?? new List<RadioStation>();
            }
            catch (TaskCanceledException)
            {
                if (cancellationToken.IsCancellationRequested) return new List<RadioStation>();
                
                Console.WriteLine($"Timeout de API. Rotando nodo...");
                RotateNode();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error API: {ex.Message}. Rotando nodo...");
                RotateNode();
            }

            if (i < maxRetries - 1 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(delayMs, cancellationToken);
            }
        }

        return new List<RadioStation>();
    }

    public Task<List<RadioStation>> GetTopVotedStationsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        var endpoint = $"/json/stations/search?order=votes&reverse=true&hidebroken=true&is_https=true&limit={limit}";
        return ExecuteWithRetryAsync(endpoint, cancellationToken);
    }

    public Task<List<RadioStation>> SearchStationsAsync(string query, int limit = 100, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Task.FromResult(new List<RadioStation>());

        var encodedQuery = Uri.EscapeDataString(query);
        var endpoint = $"/json/stations/search?name={encodedQuery}&order=votes&reverse=true&hidebroken=true&is_https=true&limit={limit}";
        return ExecuteWithRetryAsync(endpoint, cancellationToken);
    }

    public Task<List<RadioStation>> GetStationsByCountryAsync(string country, int limit = 50, string? tag = null, CancellationToken cancellationToken = default)
    {
        var encodedCountry = Uri.EscapeDataString(country);
        var endpoint = $"/json/stations/search?country={encodedCountry}&order=votes&reverse=true&hidebroken=true&is_https=true&limit={limit}";
        
        if (!string.IsNullOrWhiteSpace(tag))
        {
            endpoint += $"&tag={Uri.EscapeDataString(tag)}";
        }
        
        return ExecuteWithRetryAsync(endpoint, cancellationToken);
    }

    public Task<List<RadioStation>> GetStationsByTagAsync(string tag, int limit = 50, CancellationToken cancellationToken = default)
    {
        var encodedTag = Uri.EscapeDataString(tag);
        var endpoint = $"/json/stations/search?tag={encodedTag}&order=votes&reverse=true&hidebroken=true&is_https=true&limit={limit}";
        return ExecuteWithRetryAsync(endpoint, cancellationToken);
    }
}
