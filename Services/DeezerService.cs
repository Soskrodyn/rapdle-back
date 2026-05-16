using System.Text.Json;

namespace Rapdle.Api.Services;

public class DeezerService
{
    private readonly HttpClient _httpClient;

    // Deine Playlist ID (öffentlich!)
    private const string playlist_id = "15286556683"; 

    public DeezerService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<DeezerTrack>> GetPlaylistTracksAsync()
    {
        string url = $"https://api.deezer.com/playlist/{playlist_id}";

        var json = await _httpClient.GetStringAsync(url);

        var playlist = JsonSerializer.Deserialize<DeezerPlaylist>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        return playlist?.Tracks?.Data ?? new List<DeezerTrack>();
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
