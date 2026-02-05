using Logos.AI.Abstractions.Knowledge;
using Microsoft.EntityFrameworkCore;

namespace Logos.AI.Engine.Data;

public class LogosDbContext(DbContextOptions<LogosDbContext> options) : DbContext(options)
{
	// Таблиці
	public DbSet<Document> Documents { get; set; }
	public DbSet<DocumentChunk> Chunks { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<Document>(entity =>
		{
			entity.HasKey(e => e.Id);
			entity.HasMany(d => d.Chunks)
				.WithOne(c => c.Document)
				.HasForeignKey(c => c.DocumentId)
				.OnDelete(DeleteBehavior.Cascade); 
		});

		modelBuilder.Entity<DocumentChunk>(entity =>
		{
			entity.HasKey(e => e.Id);
		});
	}
}
