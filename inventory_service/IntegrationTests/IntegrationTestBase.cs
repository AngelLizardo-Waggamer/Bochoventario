using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using inventory_service.Controllers;
using inventory_service.Data;
using inventory_service.Models;

namespace inventory_service.IntegrationTests
{
    /// <summary>
    /// Clase base para tests de integración que usa un DatabaseFixture compartido.
    /// Esto mejora drásticamente el rendimiento al reutilizar el mismo contenedor MySQL
    /// entre todos los tests en lugar de crear uno nuevo por cada archivo de test.
    /// </summary>
    [Collection("Database collection")]
    public abstract class IntegrationTestBase : IAsyncLifetime
    {
        protected readonly DatabaseFixture _fixture;
        protected AppDbContext _context = null!;
        protected InventoryController _controller = null!;

        protected IntegrationTestBase(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            // Crear un nuevo contexto para este test específico
            _context = _fixture.CreateContext();

            // Limpiar y seedear datos para este test
            await CleanDatabase();
            await SeedDatabase();

            // Crear el controlador
            _controller = new InventoryController(_context);
        }

        /// <summary>
        /// Limpia las tablas de la base de datos para aislar cada test.
        /// Mantiene los roles que son datos de referencia estáticos.
        /// </summary>
        private async Task CleanDatabase()
        {
            // Usar DELETE en lugar de TRUNCATE debido a foreign keys
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM Inventario");
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM Articulos");
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM Usuarios");
        }

        protected virtual async Task SeedDatabase()
        {
            // Los roles ya están insertados por el schema.sql, pero verificamos
            var rolesCount = await _context.Roles.CountAsync();
            if (rolesCount == 0)
            {
                var roles = new List<Rol>
                {
                    new Rol { IdRol = 1, NombreRol = "Administrador" },
                    new Rol { IdRol = 2, NombreRol = "Gestor" },
                    new Rol { IdRol = 3, NombreRol = "Lector" }
                };
                _context.Roles.AddRange(roles);
                await _context.SaveChangesAsync();
            }

            // Seed usuarios de prueba
            var usuariosCount = await _context.Usuarios.CountAsync();
            if (usuariosCount == 0)
            {
                var usuarios = new List<Usuario>
                {
                    new Usuario
                    {
                        IdUsuario = 1,
                        IdRol = 1,
                        NombreUsuario = "admin",
                        PasswordHash = "hash_admin",
                        NombreCompleto = "Usuario Administrador"
                    },
                    new Usuario
                    {
                        IdUsuario = 2,
                        IdRol = 2,
                        NombreUsuario = "gestor",
                        PasswordHash = "hash_gestor",
                        NombreCompleto = "Usuario Gestor"
                    },
                    new Usuario
                    {
                        IdUsuario = 3,
                        IdRol = 3,
                        NombreUsuario = "lector",
                        PasswordHash = "hash_lector",
                        NombreCompleto = "Usuario Lector"
                    }
                };
                _context.Usuarios.AddRange(usuarios);
                await _context.SaveChangesAsync();
            }
        }

        protected void SetupUserClaims(int userId, int roleId, string username)
        {
            var claims = new List<Claim>
            {
                new Claim("id_usuario", userId.ToString()),
                new Claim("nombre_usuario", username),
                new Claim("id_rol", roleId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        protected void ClearUserClaims()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        protected async Task<Articulo> CreateTestArticulo(string sku, string nombre, decimal precio = 1000.00m)
        {
            var articulo = new Articulo
            {
                Sku = sku,
                Nombre = nombre,
                Descripcion = $"Descripción de {nombre}",
                PrecioCosto = precio
            };
            _context.Articulos.Add(articulo);
            await _context.SaveChangesAsync();
            return articulo;
        }

        public async Task DisposeAsync()
        {
            // Solo limpiar el contexto, NO el contenedor (que es compartido)
            await _context.DisposeAsync();
        }
    }
}
