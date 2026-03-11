using KnowledgeLM.App.Models.AI;
using KnowledgeLM.App.Models.Notes;
using Microsoft.EntityFrameworkCore;
using Resource = KnowledgeLM.App.Models.Notes.Resource;

namespace KnowledgeLM.App.Data;

public class NotesContext : DbContext
{
    public NotesContext(DbContextOptions<NotesContext> options) : base(options)
    {
    }

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<VectorizedResource> VectorizedResources => Set<VectorizedResource>();
    public DbSet<Flashcard> Flashcards => Set<Flashcard>();
    public DbSet<Podcast> Podcasts => Set<Podcast>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Course configuration
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasIndex(c => c.Name);

            entity.HasMany(c => c.Sections)
                  .WithOne(s => s.Course)
                  .HasForeignKey(s => s.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Section configuration
        modelBuilder.Entity<Section>(entity =>
        {
            entity.HasIndex(s => s.CourseId);

            entity.HasMany(s => s.Resources)
                  .WithOne(r => r.Section)
                  .HasForeignKey(r => r.SectionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Resource configuration
        modelBuilder.Entity<Resource>(entity =>
        {
            entity.HasIndex(r => r.SectionId);

            entity.Property(r => r.ResourceType)
                  .HasConversion<string>()
                  .HasMaxLength(50);
        });

        // VectorizedResource configuration
        modelBuilder.Entity<VectorizedResource>(entity =>
        {
            entity.HasIndex(v => v.ResourceId).IsUnique();
            entity.Property(v => v.Status).HasMaxLength(50);
            entity.HasOne(v => v.Resource)
                  .WithMany()
                  .HasForeignKey(v => v.ResourceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Flashcard configuration
        modelBuilder.Entity<Flashcard>(entity =>
        {
            entity.HasIndex(f => f.ResourceId);
            entity.HasOne(f => f.Resource)
                  .WithMany()
                  .HasForeignKey(f => f.ResourceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Podcast configuration
        modelBuilder.Entity<Podcast>(entity =>
        {
            entity.HasIndex(p => p.ResourceId);
            entity.Ignore(p => p.Duration);
            entity.HasOne(p => p.Resource)
                  .WithMany()
                  .HasForeignKey(p => p.ResourceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges()
    {
        SetTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<CommonItemProperties>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.LastModified = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastModified = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<Flashcard>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;
        }

        foreach (var entry in ChangeTracker.Entries<Podcast>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;
        }

        foreach (var entry in ChangeTracker.Entries<VectorizedResource>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.VectorizedAt = now;
        }
    }
}
