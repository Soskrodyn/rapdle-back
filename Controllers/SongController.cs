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
