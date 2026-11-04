using ChatServer.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatServer.Data;

public class MySqlDbContext : DbContext
{
    //Represents the connection to the database and the mapping of classes to tables.
    public MySqlDbContext(DbContextOptions<MySqlDbContext> options): base(options)
    {
    }

    //Defines how the User model will be stored in the database.
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();

        user.ToTable("Users");

        user.HasKey(u => u.Id);

        user.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(30)
            .UseCollation("utf8mb4_bin");

        user.HasIndex(u => u.Username)
            .IsUnique();
    }
}