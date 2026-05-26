using Microsoft.AspNetCore.Mvc;
using Rapdle.Api.Services;
using Rapdle.Api.Data;
using Rapdle.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Rapdle.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SongController : ControllerBase
{
    private readonly DeezerService _deezer;
    private readonly AppDbContext _db;

    public SongController(DeezerService deezer, AppDbContext db)
    {
        _deezer = deezer;
        _db = db;
    }

[HttpGet("daily")]
public async Task<IActionResult> GetDailySong()
{
    var playlist = await _deezer.GetPlaylistTracksAsync();
    if (playlist == null || playlist.Count == 0)
        return NotFound();

    string today = DateTime.UtcNow.ToString("yyyy-MM-dd");

    int seed = today.GetHashCode();
    var rng = new Random(seed);

    int index = rng.Next(playlist.Count);

    return Ok(playlist[index]);
}

[HttpGet("daily/{userId}/{date}")]
public async Task<IActionResult> GetDailySongWithCooldown(string userId, string date)
{
    var playlist = await _deezer.GetPlaylistTracksAsync();
    if (playlist == null || playlist.Count == 0)
        return NotFound();

    // Get user history
    var userHistory = await _db.UserGuesses
        .Where(g => g.UserId == userId)
        .ToListAsync();

    // Parse cooldown data
    var excludedSongs = new HashSet<string>();
    var excludedArtists = new HashSet<string>();

    var selectedDate = DateTime.Parse(date);

    foreach (var entry in userHistory)
    {
        if (string.IsNullOrEmpty(entry.Solution)) continue;

        var entryDate = DateTime.Parse(entry.Date);
        var daysDiff = (selectedDate - entryDate).Days;

        // Song cooldown: 100 days
        if (daysDiff >= 0 && daysDiff < 100)
        {
            excludedSongs.Add(entry.Solution.ToLower());
        }

        // Artist cooldown: 10 days
        if (daysDiff >= 0 && daysDiff < 10)
        {
            var parts = entry.Solution.Split(" – ");
            if (parts.Length > 1)
            {
                excludedArtists.Add(parts[1].Trim().ToLower());
            }
        }
    }

    // Find first available song respecting cooldowns
    int seed = date.GetHashCode();
    var rng = new Random(seed);

    // Try deterministic approach first
    int index = rng.Next(playlist.Count);
    var song = playlist[index];

    // If song violates cooldown, find next valid song
    int attempts = 0;
    while (attempts < playlist.Count)
    {
        var songTitle = $"{song.Title} – {song.Artist?.Name ?? ""}".ToLower().Trim();
        var artistName = song.Artist?.Name?.ToLower().Trim();
        
        // Check if song or artist is in cooldown
        bool songExcluded = excludedSongs.Contains(songTitle);
        bool artistExcluded = !string.IsNullOrEmpty(artistName) && excludedArtists.Contains(artistName);
        
        if (!songExcluded && !artistExcluded)
        {
            return Ok(song);
        }

        // Move to next song
        index = (index + 1) % playlist.Count;
        song = playlist[index];
        attempts++;
    }

    // If no valid song found (unlikely), return the original
    return Ok(playlist[rng.Next(playlist.Count)]);
}



    [HttpGet("playlist")]
    public async Task<IActionResult> GetPlaylist()
    {
        var tracks = await _deezer.GetPlaylistTracksAsync();
        return Ok(tracks);
    }

    [HttpGet("history/{userId}")]
    public async Task<IActionResult> GetHistory(string userId)
    {
        var history = await _db.UserGuesses
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.Date)
            .ToListAsync();

        return Ok(history);
    }

    [HttpPost("guess")]
    public async Task<IActionResult> SaveGuess([FromBody] UserGuess guess)
    {
        var existing = await _db.UserGuesses
            .FirstOrDefaultAsync(g => g.UserId == guess.UserId && g.Date == guess.Date);

        if (existing != null)
        {
            existing.Guesses = guess.Guesses;
            existing.Won = guess.Won;
            existing.Solution = guess.Solution;
            _db.UserGuesses.Update(existing);
        }
        else
        {
            _db.UserGuesses.Add(guess);
        }

        await _db.SaveChangesAsync();
        return Ok(guess);
    }
}
