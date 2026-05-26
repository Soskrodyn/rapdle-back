using System.Text.Json;

namespace Rapdle.Api.Services;

public class DeezerService
{
    private readonly HttpClient _httpClient;
    private static List<DeezerTrack>? _cachedPlaylist;
    private static DateTime _cacheTime = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    // Pre-built schedule: date string → song
    private static Dictionary<string, DeezerTrack>? _schedule;
    private static DateTime _scheduleBuiltAt = DateTime.MinValue;

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
        catch (Exception)
        {
            // If API call fails but we have cached data, return it even if expired
            if (_cachedPlaylist != null)
            {
                return _cachedPlaylist;
            }
            throw;
        }
    }

    public async Task<DeezerTrack?> GetScheduledSongAsync(string date)
    {
        var playlist = await GetPlaylistTracksAsync();
        if (playlist.Count == 0) return null;

        // Rebuild schedule once per day or if not built yet
        if (_schedule == null || (DateTime.UtcNow - _scheduleBuiltAt).TotalHours > 24)
        {
            _schedule = BuildSchedule(playlist);
            _scheduleBuiltAt = DateTime.UtcNow;
        }

        return _schedule.TryGetValue(date, out var song) ? song : null;
    }

    private static Dictionary<string, DeezerTrack> BuildSchedule(List<DeezerTrack> playlist)
    {
        var schedule = new Dictionary<string, DeezerTrack>();
        var startDate = new DateTime(2026, 4, 1);
        var endDate = DateTime.UtcNow.Date.AddDays(30); // Build 30 days ahead

        // Track last-used date per song and per artist
        var songLastUsed = new Dictionary<string, DateTime>();
        var artistLastUsed = new Dictionary<string, DateTime>();

        const int songCooldownDays = 200;
        const int artistCooldownDays = 20;

        var current = startDate;
        while (current <= endDate)
        {
            string dateStr = current.ToString("yyyy-MM-dd");

            // Build pool: songs respecting both cooldowns
            var available = playlist.Where(t =>
            {
                var songKey = $"{t.Title} \u2013 {t.Artist?.Name}".ToLower();
                var artistKey = t.Artist?.Name?.ToLower() ?? "";

                if (songLastUsed.TryGetValue(songKey, out var lastSong) && (current - lastSong).Days < songCooldownDays)
                    return false;

                if (!string.IsNullOrEmpty(artistKey) &&
                    artistLastUsed.TryGetValue(artistKey, out var lastArtist) &&
                    (current - lastArtist).Days < artistCooldownDays)
                    return false;

                return true;
            }).ToList();

            // Fallback: relax artist cooldown only
            if (available.Count == 0)
            {
                available = playlist.Where(t =>
                {
                    var songKey = $"{t.Title} \u2013 {t.Artist?.Name}".ToLower();
                    return !songLastUsed.TryGetValue(songKey, out var lastSong) ||
                           (current - lastSong).Days >= songCooldownDays;
                }).ToList();
            }

            // Last resort
            if (available.Count == 0)
                available = playlist;

            // Deterministic pick for this date
            var rng = new Random(dateStr.GetHashCode());
            var song = available[rng.Next(available.Count)];

            schedule[dateStr] = song;

            // Update cooldown tracking
            songLastUsed[$"{song.Title} \u2013 {song.Artist?.Name}".ToLower()] = current;
            var artist = song.Artist?.Name?.ToLower() ?? "";
            if (!string.IsNullOrEmpty(artist))
                artistLastUsed[artist] = current;

            current = current.AddDays(1);
        }

        return schedule;
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
