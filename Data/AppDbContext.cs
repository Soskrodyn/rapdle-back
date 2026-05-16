using Microsoft.EntityFrameworkCore;
using Rapdle.Api.Models;

namespace Rapdle.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserGuess> UserGuesses { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserGuess>()
            .HasKey(g => new { g.UserId, g.Date });

        modelBuilder.Entity<UserGuess>()
            .Property(g => g.Guesses)
            .HasConversion(
                v => string.Join("|", v),
                v => v.Split("|", StringSplitOptions.None).ToList()
            );
    }
}
