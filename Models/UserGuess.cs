namespace Rapdle.Api.Models;

public class UserGuess
{
    public string UserId { get; set; } = "";
    public string Date { get; set; } = "";
    public List<string> Guesses { get; set; } = new();
    public bool Won { get; set; }
    public string Solution { get; set; } = "";
}
