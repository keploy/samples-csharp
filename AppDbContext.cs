using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Example: Configuring the User entity
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id); // Set the primary key

        modelBuilder.Entity<User>()
            .Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(50); // Set Name property as required and with a maximum length of 50 characters

        modelBuilder.Entity<User>()
            .Property(u => u.Age)
            .IsRequired();

        // Add any additional configurations for other entities or relationships here
    }
}