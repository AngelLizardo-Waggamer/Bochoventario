using Microsoft.EntityFrameworkCore;
using inventory_service.Models;

namespace inventory_service.Data
{
    

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            // Nada xd
        }

        public DbSet<Articulo> Articulos { get; set; }
        public DbSet<Inventario> Inventarios { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de Rol
            modelBuilder.Entity<Rol>(entity =>
            {
                entity.HasIndex(e => e.NombreRol).IsUnique();
                
                // Seed data: Los tres roles estandarizados
                entity.HasData(
                    new Rol { IdRol = 1, NombreRol = "Administrador" },
                    new Rol { IdRol = 2, NombreRol = "Gestor" },
                    new Rol { IdRol = 3, NombreRol = "Lector" }
                );
            });

            // Configuración de Usuario
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasIndex(e => e.NombreUsuario).IsUnique();
                
                entity.Property(e => e.FechaCreacion)
                    .ValueGeneratedOnAdd();

                // Relación Usuario -> Rol (ON DELETE RESTRICT)
                entity.HasOne(e => e.Rol)
                    .WithMany(r => r.Usuarios)
                    .HasForeignKey(e => e.IdRol)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de Artículo
            modelBuilder.Entity<Articulo>(entity =>
            {
                entity.HasIndex(e => e.Sku).IsUnique();
                
                entity.Property(e => e.PrecioCosto)
                    .HasPrecision(10, 2);
            });

            // Configuración de Inventario
            modelBuilder.Entity<Inventario>(entity =>
            {
                // Índice único compuesto: id_articulo + ubicacion
                entity.HasIndex(e => new { e.IdArticulo, e.Ubicacion })
                    .IsUnique();
                
                entity.Property(e => e.UltimaActualizacion)
                    .ValueGeneratedOnAddOrUpdate();

                // Relación Inventario -> Artículo (ON DELETE CASCADE)
                entity.HasOne(e => e.Articulo)
                    .WithMany(a => a.Inventarios)
                    .HasForeignKey(e => e.IdArticulo)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relación Inventario -> Usuario (ON DELETE SET NULL)
                entity.HasOne(e => e.UsuarioModificador)
                    .WithMany(u => u.InventariosModificados)
                    .HasForeignKey(e => e.UltimaModificacionPor)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}