using System.Text.Json;

namespace Rapdle.Api.Services;

public class DeezerService
{
    private readonly HttpClient _httpClient;
    private static List<DeezerTrack>? _cachedPlaylist;
    private static DateTime _cacheTime = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    // Deine Playlist ID (öffentlich!)
    private const string playlist_id = "15286556683"; 

    public DeezerService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<DeezerTrack>> GetPlaylistTracksAsync()
    {
        // Return cached playlist if still valid
        if (_cachedPlaylist != null && DateTime.UtcNow - _cacheTime < CacheDuration)
        {
            return _cachedPlaylist;
        }

        var allTracks = new List<DeezerTrack>();
        string url = $"https://api.deezer.com/playlist/{playlist_id}";

        try
        {
            // Fetch with pagination support (Deezer API returns 50 items per page by default)
            int limit = 500; // Request up to 500 per page
            url = $"{url}?limit={limit}";
            
            var json = await _httpClient.GetStringAsync(url);

            var playlist = JsonSerializer.Deserialize<DeezerPlaylist>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (playlist?.Tracks?.Data != null)
            {
                allTracks.AddRange(playlist.Tracks.Data);
            }

            // Handle pagination if there's a "next" link
            while (!string.IsNullOrEmpty(playlist?.Tracks?.Next))
            {
                try
                {
                    json = await _httpClient.GetStringAsync(playlist.Tracks.Next);
                    playlist = JsonSerializer.Deserialize<DeezerPlaylist>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    
                    if (playlist?.Tracks?.Data != null)
                    {
                        allTracks.AddRange(playlist.Tracks.Data);
                    }
                }
                catch
                {
                    // If pagination fails, return what we have so far
                    break;
                }
            }
            
            // Cache the results
            _cachedPlaylist = allTracks;
            _cacheTime = DateTime.UtcNow;

            return allTracks;
        }
        catch (Exception ex)
        {
            // If API call fails but we have cached data, return it even if expired
            if (_cachedPlaylist != null)
            {
                return _cachedPlaylist;
            }
            throw;
        }
    }

    public async Task<DeezerTrack?> GetDailySongAsync()
    {
        var tracks = await GetPlaylistTracksAsync();
        if (tracks.Count == 0) return null;

        // Nur Tracks mit Preview nehmen
        var withPreview = tracks
            .Where(t => !string.IsNullOrEmpty(t.Preview))
            .ToList();

        if (withPreview.Count == 0) return null;

        var random = new Random();
        return withPreview[random.Next(withPreview.Count)];
    }
}


public class DeezerPlaylist
{
    public DeezerTrackList Tracks { get; set; } = new();
}

public class DeezerTrackList
{
    public List<DeezerTrack> Data { get; set; } = new();
    public string? Next { get; set; } // For pagination
}

public class DeezerTrack
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Preview { get; set; } = ""; // 30s MP3
    public List<DeezerArtist> Contributors { get; set; } = new();
    public DeezerArtist Artist { get; set; } = new();
}

public class DeezerArtist
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
}
