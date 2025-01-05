using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace WypozyczalniaSamochodowa.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<WypozyczalniaSamochodowa.Models.Auto> Auto { get; set; } = default!;
        public DbSet<WypozyczalniaSamochodowa.Models.Oferta> Oferta { get; set; } = default!;
        public DbSet<WypozyczalniaSamochodowa.Models.Wynajecie> Wynajecie { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

           
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(string) && property.GetColumnType() == null)
                    {
                        property.SetColumnType("TEXT");
                    }
                }
            }
        }
    }
}
