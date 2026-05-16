using Rapdle.Api.Data;
using Rapdle.Api.Models;

namespace Rapdle.Api.Services;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        // Nur seeden wenn noch keine Daten vorhanden sind
        if (db.UserGuesses.Any()) return;

        var testGuesses = new List<UserGuess>
        {
            // Test-User 1: ein paar Tage mit Guesses
            new UserGuess
            {
                UserId = "test-user-1",
                Date = "2026-05-16",
                Guesses = new List<string> { "Song1 – Artist1", "Song2 – Artist2" },
                Won = false,
                Solution = "Song3 – Artist3"
            },
            new UserGuess
            {
                UserId = "test-user-1",
                Date = "2026-05-15",
                Guesses = new List<string> { "Song A – Artist A" },
                Won = true,
                Solution = "Song A – Artist A"
            },
            new UserGuess
            {
                UserId = "test-user-1",
                Date = "2026-05-14",
                Guesses = new List<string> { "Wrong1", "Wrong2", "Wrong3", "Wrong4", "Wrong5" },
                Won = false,
                Solution = "Correct – Song"
            },
            
            // Test-User 2: andere Einträge
            new UserGuess
            {
                UserId = "test-user-2",
                Date = "2026-05-16",
                Guesses = new List<string> { "Test1 – Test1" },
                Won = true,
                Solution = "Test1 – Test1"
            },
            new UserGuess
            {
                UserId = "test-user-2",
                Date = "2026-05-15",
                Guesses = new List<string> { "Nope – Nope" },
                Won = false,
                Solution = "Yep – Yep"
            }
        };

        db.UserGuesses.AddRange(testGuesses);
        db.SaveChanges();
    }
}
