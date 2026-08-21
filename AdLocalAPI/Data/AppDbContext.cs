using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AdLocalAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

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
        public DbSet<TipoComercio> TipoComercio { get; set; }

        /* =========================
           MARKETPLACE - FASE 1
        ========================== */

        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<PedidoDetalle> PedidoDetalles { get; set; }
        public DbSet<PedidoHistorial> PedidoHistorial { get; set; }

        public DbSet<DireccionUsuario> DireccionesUsuarios { get; set; }

        public DbSet<ConfiguracionComercioPedido> ConfiguracionComercioPedidos { get; set; }
        public DbSet<CuentaBancariaComercio> CuentasBancariasComercio { get; set; }

        public DbSet<ComprobantePago> ComprobantesPago { get; set; }

        public DbSet<Notificacion> Notificaciones { get; set; }

        public DbSet<Comision> Comisiones { get; set; }
        public DbSet<ConfiguracionComision> ConfiguracionComisiones { get; set; }
        public DbSet<CuentaBancariaAdLocal> CuentasBancariasAdLocal { get; set; }
        public DbSet<PagoComision> PagosComisiones { get; set; }
        public DbSet<PagoComisionDetalle> PagosComisionesDetalle { get; set; }

        public DbSet<Carrito> Carritos { get; set; }
        public DbSet<CarritoDetalle> CarritoDetalles { get; set; }
        public DbSet<ConfiguracionPagoComercio> ConfiguracionPagoComercios { get; set; }
        public DbSet<Cita> Citas { get; set; }
        public DbSet<HorarioCitaServicio> HorariosCitaServicio { get; set; }
        public DbSet<Cotizacion> Cotizaciones { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Cotizacion>(entity =>
            {
                entity.HasIndex(x => x.Uuid).IsUnique();
                entity.HasIndex(x => new {x.IdUsuario, x.FechaCreacion});
                entity.HasOne<Usuario>().WithMany().HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Comercio>().WithMany().HasForeignKey(x => x.IdComercio).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<ProductosServicios>().WithMany().HasForeignKey(x => x.IdProductoServicio).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Cita>(entity =>
            {
                entity.HasIndex(x => x.Uuid).IsUnique();
                entity.HasIndex(x => new { x.IdComercio, x.FechaInicio, x.FechaFin });
                entity.HasIndex(x => new { x.IdUsuario, x.FechaInicio });
                entity.Property(x => x.FechaInicio).HasColumnType("timestamp without time zone");
                entity.Property(x => x.FechaFin).HasColumnType("timestamp without time zone");
                entity.Property(x => x.FechaCreacion).HasColumnType("timestamp with time zone");
                entity.Property(x => x.FechaActualizacion).HasColumnType("timestamp with time zone");
                entity.HasOne<Usuario>().WithMany().HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Comercio>().WithMany().HasForeignKey(x => x.IdComercio).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<ProductosServicios>().WithMany().HasForeignKey(x => x.IdProductoServicio).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<HorarioCitaServicio>(entity =>
            {
                entity.HasIndex(x => x.Uuid).IsUnique();
                entity.HasIndex(x => new { x.IdProductoServicio, x.Fecha, x.HoraInicio }).IsUnique();
                entity.HasIndex(x => new { x.IdComercio, x.Fecha, x.Disponible });
                entity.HasOne<ProductosServicios>().WithMany().HasForeignKey(x => x.IdProductoServicio).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<Comercio>().WithMany().HasForeignKey(x => x.IdComercio).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<Cita>().WithMany().HasForeignKey(x => x.IdCita).OnDelete(DeleteBehavior.SetNull);
            });

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
               COMERCIO
               USUARIO 1 -> N
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
               IMAGENES DE COMERCIO
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
                .HasIndex(h => new
                {
                    h.ComercioId,
                    h.Dia
                })
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


            /* =========================
               CODIGO REFERIDO
            ========================== */

            modelBuilder.Entity<UsoCodigoReferido>(entity =>
            {
                entity.HasIndex(e => e.UsuarioReferidoId)
                    .IsUnique();

                entity.HasCheckConstraint(
                    "CK_NoAutoReferido",
                    "\"UsuarioReferidorId\" <> \"UsuarioReferidoId\""
                );

                entity.Property(e => e.CodigoReferido)
                    .HasMaxLength(50)
                    .IsRequired();
            });


            /* =========================
               TIPO COMERCIO
            ========================== */

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


            /* ============================================================
               MARKETPLACE
               PEDIDOS
            ============================================================ */

            modelBuilder.Entity<Pedido>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.Uuid)
                    .IsUnique();

                entity.HasIndex(x => x.NumeroPedido)
                    .IsUnique();

                entity.HasIndex(x => x.IdUsuario);

                entity.HasIndex(x => x.IdComercio);

                entity.HasIndex(x => x.Estado);

                entity.HasIndex(x => x.EstadoPago);

                entity.HasIndex(x => x.FechaCreacion);

                entity.HasOne<Usuario>()
                    .WithMany()
                    .HasForeignKey(x => x.IdUsuario)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Comercio>()
                    .WithMany()
                    .HasForeignKey(x => x.IdComercio)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<DireccionUsuario>()
                    .WithMany()
                    .HasForeignKey(x => x.IdDireccionUsuario)
                    .OnDelete(DeleteBehavior.SetNull);
            });


            /* =========================
               PEDIDO DETALLES
            ========================== */

            modelBuilder.Entity<PedidoDetalle>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.Uuid)
                    .IsUnique();

                entity.HasIndex(x => x.IdPedido);

                entity.HasOne(x => x.Pedido)
                    .WithMany(x => x.Detalles)
                    .HasForeignKey(x => x.IdPedido)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<ProductosServicios>()
                    .WithMany()
                    .HasForeignKey(x => x.IdProductoServicio)
                    .OnDelete(DeleteBehavior.SetNull);
            });


            /* =========================
               PEDIDO HISTORIAL
            ========================== */

            modelBuilder.Entity<PedidoHistorialEstado>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.Uuid)
                    .IsUnique();

                entity.HasIndex(x => x.IdPedido);

                entity.HasIndex(x => x.FechaCreacion);

                entity.HasOne(x => x.Pedido)
                    .WithMany(x => x.HistorialEstados)
                    .HasForeignKey(x => x.IdPedido)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            /* =========================
               DIRECCIONES USUARIO
            ========================== */

            modelBuilder.Entity<DireccionUsuario>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.Uuid)
                    .IsUnique();

                entity.HasIndex(x => x.IdUsuario);

                entity.HasIndex(x => new
                {
                    x.IdUsuario,
                    x.EsPredeterminada
                });

                entity.HasOne(x => x.Usuario)
                    .WithMany(x => x.Direcciones)
                    .HasForeignKey(x => x.IdUsuario)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Estado)
                    .WithMany()
                    .HasForeignKey(x => x.IdEstado)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Municipio)
                    .WithMany()
                    .HasForeignKey(x => x.IdMunicipio)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            /* =========================
               CONFIGURACION
               PEDIDOS COMERCIO
            ========================== */

            modelBuilder.Entity<ConfiguracionComercioPedido>(entity =>
            {
                entity.HasIndex(x => x.Uuid)
                    .IsUnique();

                entity.HasIndex(x => x.IdComercio)
                    .IsUnique();

                entity.HasOne<Comercio>()
                    .WithOne()
                    .HasForeignKey<ConfiguracionComercioPedido>(
                        x => x.IdComercio
                    )
                    .OnDelete(DeleteBehavior.Cascade);
            });


            /* =========================
               CUENTAS BANCARIAS
            ========================== */

            modelBuilder.Entity<CuentaBancariaComercio>(entity =>
            {
                entity.HasIndex(x => x.Uuid)
                    .IsUnique();

                entity.HasIndex(x => x.IdComercio);

                entity.HasIndex(x => new
                {
                    x.IdComercio,
                    x.Principal
                });

                entity.HasOne<Comercio>()
                    .WithMany()
                    .HasForeignKey(x => x.IdComercio)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            /* =========================
               COMPROBANTES DE PAGO
            ========================== */

            modelBuilder.Entity<ComprobantePago>(entity =>
            {
                entity.HasIndex(x => x.Uuid)
                    .IsUnique();

                entity.HasIndex(x => x.IdPedido);

                entity.HasIndex(x => x.IdUsuario);

                entity.HasIndex(x => x.Estatus);

                entity.HasOne<Pedido>()
                    .WithMany()
                    .HasForeignKey(x => x.IdPedido)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Usuario>()
                    .WithMany()
                    .HasForeignKey(x => x.IdUsuario)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Usuario>()
                    .WithMany()
                    .HasForeignKey(x => x.IdUsuarioValidacion)
                    .OnDelete(DeleteBehavior.SetNull);
            });


            /* =========================
               NOTIFICACIONES
            ========================== */

            modelBuilder.Entity<Notificacion>(entity =>
            {
                entity.HasIndex(x => x.Uuid)
                    .IsUnique();

                entity.HasIndex(x => x.IdUsuario);

                entity.HasIndex(x => x.Leida);

                entity.HasIndex(x => x.FechaCreacion);

                entity.HasIndex(x => new
                {
                    x.TipoReferencia,
                    x.IdReferencia
                });

                entity.HasOne<Usuario>()
                    .WithMany()
                    .HasForeignKey(x => x.IdUsuario)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            /* =========================
               COMISIONES
            ========================== */

            modelBuilder.Entity<Comision>(entity =>
            {
                entity.HasIndex(x => x.Uuid)
                    .IsUnique();

                entity.HasIndex(x => x.IdComercio);

                entity.HasIndex(x => x.Estatus);

                entity.HasIndex(x => new
                {
                    x.TipoOperacion,
                    x.IdReferencia
                })
                .IsUnique();

                entity.HasOne<Comercio>()
                    .WithMany()
                    .HasForeignKey(x => x.IdComercio)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            /* =========================
               CONFIGURACION COMISIONES
            ========================== */

            modelBuilder.Entity<ConfiguracionComision>(entity =>
            {
                entity.HasIndex(x => x.Uuid)
                    .IsUnique();

                entity.HasIndex(x => new
                {
                    x.TipoOperacion,
                    x.Activo
                });
            });

            modelBuilder.Entity<CuentaBancariaAdLocal>(entity =>
            {
                entity.HasIndex(x => x.Uuid).IsUnique();
                entity.HasIndex(x => new { x.Principal, x.Activo });
            });

            modelBuilder.Entity<PagoComision>(entity =>
            {
                entity.HasIndex(x => x.Uuid).IsUnique();
                entity.HasIndex(x => new { x.IdComercio, x.Estatus });
                entity.HasOne<Comercio>().WithMany().HasForeignKey(x => x.IdComercio).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<CuentaBancariaAdLocal>().WithMany().HasForeignKey(x => x.IdCuentaBancariaAdLocal).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PagoComisionDetalle>(entity =>
            {
                entity.HasIndex(x => x.IdComision).IsUnique();
                entity.HasOne(x => x.PagoComision).WithMany(x => x.Detalles).HasForeignKey(x => x.IdPagoComision).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<Comision>().WithMany().HasForeignKey(x => x.IdComision).OnDelete(DeleteBehavior.Restrict);
            });


            /* =========================
               CARRITOS
            ========================== */

            modelBuilder.Entity<Carrito>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.Uuid)
                    .IsUnique();

                entity.HasIndex(x => new
                {
                    x.IdUsuario,
                    x.Activo
                });

                entity.HasOne<Usuario>()
                    .WithMany()
                    .HasForeignKey(x => x.IdUsuario)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            /* =========================
               CARRITO DETALLES
            ========================== */

            modelBuilder.Entity<CarritoDetalle>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.Uuid)
                    .IsUnique();

                entity.HasIndex(x => new
                {
                    x.IdCarrito,
                    x.IdProductoServicio
                })
                .IsUnique();

                entity.HasOne(x => x.Carrito)
                    .WithMany(x => x.Detalles)
                    .HasForeignKey(x => x.IdCarrito)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.ProductoServicio)
                    .WithMany()
                    .HasForeignKey(x => x.IdProductoServicio)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<CuentaBancariaComercio>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.Uuid)
                    .IsUnique();

                entity.HasIndex(x => x.IdComercio);

                entity.HasOne(x => x.Comercio)
                    .WithMany()
                    .HasForeignKey(x => x.IdComercio)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<ConfiguracionPagoComercio>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.Uuid)
                    .IsUnique();

                entity.HasIndex(x => x.IdComercio)
                    .IsUnique();

                entity.HasOne(x => x.Comercio)
                    .WithOne()
                    .HasForeignKey<ConfiguracionPagoComercio>(
                        x => x.IdComercio
                    )
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
