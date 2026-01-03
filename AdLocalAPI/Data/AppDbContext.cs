using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Comercio> Comercios { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Plan> Plans { get; set; }
        public DbSet<Suscripcion> Suscripcions { get; set; }
        public DbSet<Evento> Eventos { get; set; }
        public DbSet<Promocion> Promociones { get; set; }
        public DbSet<Publicidad> Publicidades { get; set; }
        public DbSet<ConfiguracionSistema> ConfiguracionSistema { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Usuario -> Comercio (opcional)
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Comercio)
                .WithMany()
                .HasForeignKey(u => u.ComercioId);

            // Suscripcion -> Usuario
            modelBuilder.Entity<Suscripcion>()
                .HasOne(s => s.Usuario)
                .WithMany()
                .HasForeignKey(s => s.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // Suscripcion -> Plan
            modelBuilder.Entity<Suscripcion>()
                .HasOne(s => s.Plan)
                .WithMany()
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ Plan -> FechaCreacion (PostgreSQL)
            modelBuilder.Entity<Plan>()
                .Property(p => p.FechaCreacion)
                .HasDefaultValueSql("NOW()");

            modelBuilder.Entity<Evento>()
                .HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.UsuarioId);

            modelBuilder.Entity<Promocion>()
                .HasOne(p => p.Usuario)
                .WithMany()
                .HasForeignKey(p => p.UsuarioId);

            modelBuilder.Entity<Publicidad>()
                .HasOne(p => p.Usuario)
                .WithMany()
                .HasForeignKey(p => p.UsuarioId);

            modelBuilder.Entity<Comercio>()
                .HasIndex(c => c.IdUsuario)
                .IsUnique();
        }

    }
}
