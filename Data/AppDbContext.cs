using Microsoft.EntityFrameworkCore;
using TodoApi.Entities;

namespace TodoApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        //Tabla Patientes
        public DbSet<Patient> Patients { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Evitar duplicados en (DocumentType, DocumentNumber)
            modelBuilder.Entity<Patient>() 
                .HasIndex(p => new { p.DocumentType, p.DocumentNumber })
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}
