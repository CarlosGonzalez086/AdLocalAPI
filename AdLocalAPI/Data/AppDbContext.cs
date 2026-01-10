using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

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
        public DbSet<Tarjeta> Tarjeta { get; set; }
        public DbSet<ProductosServicios> ProductosServicios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Usuario -> Comercio (opcional)
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Comercio)
                .WithMany()
                .HasForeignKey(u => u.ComercioId);

            modelBuilder.Entity<Usuario>()
                .Property(u => u.StripeCustomerId)
                .HasColumnName("stripecustomerid") // coincide con la columna real
                .HasMaxLength(100); // opcional


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

            modelBuilder.Entity<Comercio>()
                .Property(c => c.Ubicacion)
                .HasColumnType("geography (point, 4326)");

            modelBuilder.Entity<Tarjeta>(entity =>
            {
                entity.ToTable("tarjetas");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.StripeCustomerId)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.StripePaymentMethodId)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasIndex(e => e.StripePaymentMethodId)
                      .IsUnique();

                entity.Property(e => e.Brand)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.Last4)
                      .IsRequired()
                      .HasMaxLength(4);

                entity.Property(e => e.CardType)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(e => e.Status)
                      .HasMaxLength(20)
                      .HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("NOW()");
            });
            modelBuilder.Entity<ProductosServicios>(entity =>
            {
                entity.HasIndex(e => e.IdComercio);
                entity.HasIndex(e => e.IdUsuario);
                entity.HasIndex(e => e.Activo);
                entity.HasIndex(e => e.Eliminado);
                entity.HasQueryFilter(e => !e.Eliminado);
            });
            modelBuilder.Entity<ProductosServicios>()
                .Property(e => e.FechaCreacion)
                .ValueGeneratedOnAdd()
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        }

    }
}
