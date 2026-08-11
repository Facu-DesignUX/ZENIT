using Microsoft.JSInterop;
using System.Text.Json;
using ZENIT.Blazor.Models;

namespace ZENIT.Blazor.Services;

public class FavoritesService
{
    private readonly IJSRuntime _jsRuntime;
    private const string StorageKey = "zenit_favorites";
    
    private List<RadioStation> _favorites = new();
    public bool IsInitialized { get; private set; }

    public event Action? OnFavoritesChanged;

    public FavoritesService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        if (IsInitialized) return;

        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrEmpty(json))
            {
                _favorites = JsonSerializer.Deserialize<List<RadioStation>>(json) ?? new();
            }
        }
        catch 
        {
            _favorites = new();
        }
        
        IsInitialized = true;
        OnFavoritesChanged?.Invoke();
    }

    public IReadOnlyList<RadioStation> Favorites => _favorites;

    public bool IsFavorite(string uuid)
    {
        return _favorites.Any(x => x.StationUuid == uuid);
    }

    public async Task ToggleFavoriteAsync(RadioStation station)
    {
        var existing = _favorites.FirstOrDefault(x => x.StationUuid == station.StationUuid);

        if (existing != null)
        {
            _favorites.Remove(existing);
        }
        else
        {
            _favorites.Add(station);
        }

        try
        {
            var json = JsonSerializer.Serialize(_favorites);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
        catch { /* Handle potential localStorage quota errors if necessary */ }
        
        OnFavoritesChanged?.Invoke();
    }
}
