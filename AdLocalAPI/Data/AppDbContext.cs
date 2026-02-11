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
        public DbSet<CalificacionComentario> CalificacionComentario { get; set; }
        public DbSet<ComercioVisita> ComercioVisitas { get; set; }
        public DbSet<UsoCodigoReferido> UsoCodigoReferido { get; set; }
        public ICollection<UsoCodigoReferido> CodigosReferidosUsados { get; set; }
        public ICollection<UsoCodigoReferido> CodigosReferidosGenerados { get; set; }
        public DbSet<TipoComercio> TipoComercio { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            /* =========================
               USUARIO
            ========================== */

            modelBuilder.Entity<Usuario>()
                .Property(u => u.StripeCustomerId)
                .HasColumnName("stripecustomerid")
                .HasMaxLength(100);

            /* =========================
               SUSCRIPCION / PLAN
            ========================== */

            modelBuilder.Entity<Suscripcion>()
                .HasOne(s => s.Usuario)
                .WithMany(u => u.Suscripciones)
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

            /* =========================
               RELACIONES CON USUARIO
            ========================== */

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

            /* =========================
               COMERCIO (USUARIO 1 → N)
            ========================== */

            modelBuilder.Entity<Comercio>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.HasOne(c => c.Usuario)
                      .WithMany(u => u.Comercios)
                      .HasForeignKey(c => c.IdUsuario)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(c => c.IdUsuario);

                entity.Property(c => c.Ubicacion)
                      .HasColumnType("geometry(Point,4326)");
                 
            });

            /* =========================
               TARJETAS
            ========================== */

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

                // BOOL limpio
                entity.Property(e => e.Status)
                      .HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("NOW()");
            });

            /* =========================
               PRODUCTOS / SERVICIOS
            ========================== */

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

            /* =========================
               IMÁGENES DE COMERCIO
            ========================== */

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

            /* =========================
               HORARIOS
            ========================== */

            modelBuilder.Entity<HorarioComercio>()
                .HasOne<Comercio>()
                .WithMany()
                .HasForeignKey(h => h.ComercioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HorarioComercio>()
                .HasIndex(h => new { h.ComercioId, h.Dia })
                .IsUnique();

            /* =========================
               ESTADOS / MUNICIPIOS
            ========================== */

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

            /* =========================
               CALIFICACIONES
            ========================== */

            modelBuilder.Entity<CalificacionComentario>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Comentario)
                      .HasMaxLength(250)
                      .IsRequired();

                entity.Property(e => e.NombrePersona)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(e => e.Calificacion)
                      .IsRequired();

                entity.Property(e => e.FechaCreacion)
                      .HasDefaultValueSql("NOW()")
                      .IsRequired();
            });

            modelBuilder.Entity<Comercio>()
                .HasMany(c => c.CalificacionesComentarios)
                .WithOne(cc => cc.Comercio)
                .HasForeignKey(cc => cc.IdComercio)
                .OnDelete(DeleteBehavior.Cascade);

            /* =========================
               VISITAS DEL COMERCIO
            ========================== */

            modelBuilder.Entity<ComercioVisita>()
                .HasIndex(v => v.ComercioId);

            modelBuilder.Entity<ComercioVisita>()
                .HasIndex(v => v.FechaVisita);

            modelBuilder.Entity<UsoCodigoReferido>(entity =>
            {
                // Un usuario solo puede usar un código una vez
                entity.HasIndex(e => e.UsuarioReferidoId)
                      .IsUnique();

                // Evitar que se auto-refiera
                entity.HasCheckConstraint(
                    "CK_NoAutoReferido",
                    "\"UsuarioReferidorId\" <> \"UsuarioReferidoId\""
                );

                entity.Property(e => e.CodigoReferido)
                      .HasMaxLength(50)
                      .IsRequired();
            });

            modelBuilder.Entity<TipoComercio>(entity =>
            {
                entity.ToTable("TipoComercio");

                entity.Property(e => e.Nombre)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Descripcion)
                      .HasMaxLength(250);
            });

            modelBuilder.Entity<Comercio>(entity =>
            {
                entity.HasOne(c => c.TipoComercio)
                      .WithMany(t => t.Comercios)
                      .HasForeignKey(c => c.TipoComercioId)
                      .OnDelete(DeleteBehavior.Restrict);
            });


        }


    }
}
