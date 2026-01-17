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
        public DbSet<RelComercioImagen> RelComercioImagen { get; set; }
        public DbSet<HorarioComercio> HorarioComercio { get; set; }
        public DbSet<Estado> Estados { get; set; }
        public DbSet<Municipio> Municipios { get; set; }
        public DbSet<EstadoMunicipio> EstadosMunicipios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Comercio)
                .WithMany()
                .HasForeignKey(u => u.ComercioId);

            modelBuilder.Entity<Usuario>()
                .Property(u => u.StripeCustomerId)
                .HasColumnName("stripecustomerid") 
                .HasMaxLength(100);

            modelBuilder.Entity<Suscripcion>()
                .HasOne(s => s.Usuario)
                .WithMany()
                .HasForeignKey(s => s.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Suscripcion>()
                .HasOne(s => s.Plan)
                .WithMany()
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.Cascade);

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
                entity.Property(e => e.FechaCreacion)
                    .ValueGeneratedOnAdd()
                    .HasDefaultValueSql("NOW()")
                    .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });
            modelBuilder.Entity<ProductosServicios>()
                .Property(e => e.FechaCreacion)
                .ValueGeneratedOnAdd()
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            modelBuilder.Entity<RelComercioImagen>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                      .HasColumnName("id");

                entity.Property(e => e.IdComercio)
                      .HasColumnName("id_comercio");

                entity.Property(e => e.FotoUrl)
                      .HasColumnName("foto_url")
                      .IsRequired();

                entity.Property(e => e.FechaCreacion)
                      .HasColumnName("fecha_creacion")
                      .HasDefaultValueSql("NOW()");

                entity.Property(e => e.FechaActualizacion)
                      .HasColumnName("fecha_actualizacion");
            });
            modelBuilder.Entity<HorarioComercio>()
                .HasOne<Comercio>()              
                .WithMany()
                .HasForeignKey(h => h.ComercioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HorarioComercio>()
                .HasIndex(h => new { h.ComercioId, h.Dia })
                .IsUnique();

            modelBuilder.Entity<Comercio>()
                        .Property(c => c.Ubicacion)
                        .HasColumnType("geometry(Point,4326)");
            modelBuilder.Entity<EstadoMunicipio>()
                        .HasOne(em => em.Estado)
                        .WithMany(e => e.EstadosMunicipios)
                        .HasForeignKey(em => em.EstadoId);

            modelBuilder.Entity<EstadoMunicipio>()
                        .HasOne(em => em.Municipio)
                        .WithMany(m => m.EstadosMunicipios)
                        .HasForeignKey(em => em.MunicipioId);

            modelBuilder.Entity<Comercio>()
                        .HasOne(c => c.Estado)
                        .WithMany()
                        .HasForeignKey(c => c.EstadoId)
                        .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comercio>()
                        .HasOne(c => c.Municipio)
                        .WithMany()
                        .HasForeignKey(c => c.MunicipioId)
                        .OnDelete(DeleteBehavior.Restrict);
        }

    }
}
