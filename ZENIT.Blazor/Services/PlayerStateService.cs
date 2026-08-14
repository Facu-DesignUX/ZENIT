using Microsoft.JSInterop;
using ZENIT.Blazor.Models;

namespace ZENIT.Blazor.Services;

public class PlayerStateService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<PlayerStateService>? _objRef;

    public RadioStation? CurrentStation { get; private set; }
    public IReadOnlyList<RadioStation>? CurrentPlaylist { get; private set; }
    public bool IsPlaying { get; private set; }
    public bool IsBuffering { get; private set; }
    public bool HasError { get; private set; }
    
    // UI State for mobile full-screen player
    public bool IsImmersiveMode { get; private set; }

    public float Volume { get; private set; } = 1.0f;

    public event Action? OnStateChanged;

    public async Task SetVolumeAsync(float volume)
    {
        Volume = volume;
        NotifyStateChanged();
        if (_objRef != null)
        {
            await _jsRuntime.InvokeVoidAsync("ZenitAudioPlayer.setVolume", volume);
        }
    }

    public PlayerStateService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        if (_objRef == null)
        {
            _objRef = DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync("ZenitAudioPlayer.initialize", _objRef);
        }
    }

    public async Task PlayAsync(RadioStation station, IReadOnlyList<RadioStation>? playlist = null)
    {
        CurrentStation = station;
        if (playlist != null)
        {
            CurrentPlaylist = playlist;
        }
        
        IsBuffering = true;
        HasError = false;
        NotifyStateChanged();

        if (_objRef == null)
        {
            await InitializeAsync();
        }

        // url_resolved is the actual audio stream
        var streamUrl = !string.IsNullOrEmpty(station.UrlResolved) ? station.UrlResolved : station.Url;
        
        // Pass metadata for Media Session API
        await _jsRuntime.InvokeVoidAsync("ZenitAudioPlayer.play", streamUrl, new {
            title = station.Name,
            artist = station.Country,
            album = "ZENIT Radio",
            artworkUrl = station.Favicon
        });
    }

    public async Task PlayNextAsync()
    {
        if (CurrentPlaylist == null || !CurrentPlaylist.Any() || CurrentStation == null) return;

        var currentIndex = CurrentPlaylist.ToList().FindIndex(s => s.StationUuid == CurrentStation.StationUuid);
        if (currentIndex >= 0 && currentIndex < CurrentPlaylist.Count - 1)
        {
            await PlayAsync(CurrentPlaylist[currentIndex + 1], CurrentPlaylist);
        }
        else if (currentIndex == CurrentPlaylist.Count - 1)
        {
            // Wrap around to the beginning
            await PlayAsync(CurrentPlaylist[0], CurrentPlaylist);
        }
    }

    public async Task PauseAsync()
    {
        await _jsRuntime.InvokeVoidAsync("ZenitAudioPlayer.pause");
    }

    public async Task TogglePlayPauseAsync()
    {
        if (CurrentStation == null) return;
        
        if (IsPlaying)
        {
            await PauseAsync();
        }
        else
        {
            await _jsRuntime.InvokeVoidAsync("ZenitAudioPlayer.resume");
        }
    }

    public void ToggleImmersiveMode()
    {
        IsImmersiveMode = !IsImmersiveMode;
        NotifyStateChanged();
    }

    public void CloseImmersiveMode()
    {
        IsImmersiveMode = false;
        NotifyStateChanged();
    }

    // --- Invokable from JS ---
    [JSInvokable]
    public void OnAudioPlaying()
    {
        IsPlaying = true;
        IsBuffering = false;
        HasError = false;
        NotifyStateChanged();
    }

    [JSInvokable]
    public void OnAudioPaused()
    {
        IsPlaying = false;
        IsBuffering = false;
        NotifyStateChanged();
    }

    [JSInvokable]
    public void OnAudioWaiting()
    {
        IsBuffering = true;
        NotifyStateChanged();
    }

    [JSInvokable]
    public void OnAudioError()
    {
        IsPlaying = false;
        IsBuffering = false;
        HasError = true;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();

    public async ValueTask DisposeAsync()
    {
        if (_objRef != null)
        {
            _objRef.Dispose();
            _objRef = null;
        }
    }
}
