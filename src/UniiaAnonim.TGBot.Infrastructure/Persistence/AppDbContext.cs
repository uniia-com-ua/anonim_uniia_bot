using Microsoft.EntityFrameworkCore;

namespace UniiaAnonim.TGBot.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    /// <summary>
    /// Configures the entity mappings and seeds initial data for the model.
    /// </summary>
    /// <param name="modelBuilder">The modelBuilder used to construct the model for the context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
