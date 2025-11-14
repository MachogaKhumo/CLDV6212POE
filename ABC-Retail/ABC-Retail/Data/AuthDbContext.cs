using ABC_Retail.Models;
using Microsoft.EntityFrameworkCore;

namespace ABC_Retail.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        // Add parameterless constructor for migrations
        public AuthDbContext()
        {
        }

        // Only include models that will be stored in Azure SQL
        public DbSet<User> Users => Set<User>();
        public DbSet<Cart> Cart => Set<Cart>();

        // REMOVE this line - Order is for Azure Table Storage only
        // public DbSet<Order> Orders => Set<Order>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // This is only used for migrations
                optionsBuilder.UseSqlServer("Server=tcp:abc-retail-sql-server.database.windows.net,1433;Initial Catalog=ABC_RetailDB;Persist Security Info=False;User ID=st10396677;Password=Ch0c0late;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure unique constraint on Username
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
        }
    }
}