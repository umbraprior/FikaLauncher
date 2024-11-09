using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using FikaLauncher.Services;

namespace FikaLauncher.Database;

public class AppDbContext : DbContext
{
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<ServerBookmarkEntity> ServerBookmarks { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbFolder = Path.Combine(FileSystemService.SettingsDirectory, "database");
        if (!Directory.Exists(dbFolder))
        {
            Directory.CreateDirectory(dbFolder);
        }
        
        var dbPath = Path.Combine(dbFolder, "users.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        
        // Suppress the pending model changes warning
        optionsBuilder.ConfigureWarnings(warnings => 
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            
            // Configure one-to-many relationship with ServerBookmarks
            entity.HasMany(e => e.ServerBookmarks)
                  .WithOne(e => e.User)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServerBookmarkEntity>(entity =>
        {
            entity.ToTable("ServerBookmarks");
            entity.HasKey(e => e.Id);
            
            // Create an index on UserId for faster lookups
            entity.HasIndex(e => e.UserId);
            
            // Create a unique index on the combination of UserId, ServerAddress, and ServerPort
            entity.HasIndex(e => new { e.UserId, e.ServerAddress, e.ServerPort }).IsUnique();
        });
    }
}
