using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using Xunit;
using inventory_service.Controllers;
using inventory_service.Data;
using inventory_service.Models;

namespace inventory_service.IntegrationTests
{
    /// <summary>
    /// Clase base para tests de integración que levanta un contenedor MySQL real
    /// con el schema de producción y proporciona métodos helper comunes.
    /// </summary>
    public abstract class IntegrationTestBase : IAsyncLifetime
    {
        protected MySqlContainer _mySqlContainer = null!;
        protected AppDbContext _context = null!;
        protected InventoryController _controller = null!;

        public async Task InitializeAsync()
        {
            // Configurar y levantar el contenedor MySQL con la misma versión de producción
            _mySqlContainer = new MySqlBuilder()
                .WithImage("mysql:8.4")
                .WithDatabase("Bochoventario")
                .WithUsername("test_user")
                .WithPassword("test_password")
                .Build();

            await _mySqlContainer.StartAsync();

            // Configurar DbContext con la conexión al contenedor
            var connectionString = _mySqlContainer.GetConnectionString();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
                .Options;

            _context = new AppDbContext(options);

            // Aplicar el schema SQL de producción
            await ApplyDatabaseSchema();

            // Seed data inicial para los tests
            await SeedDatabase();

            // Crear el controlador
            _controller = new InventoryController(_context);
        }

        private async Task ApplyDatabaseSchema()
        {
            // Buscar el archivo schema.sql en múltiples ubicaciones posibles
            var possiblePaths = new[]
            {
                // 1. Directorio actual (bin/Debug/net8.0 cuando se ejecutan tests)
                Path.Combine(Directory.GetCurrentDirectory(), "schema.sql"),
                // 2. Directorio raíz del proyecto (3 niveles arriba de bin/Debug/net8.0)
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "schema.sql"),
                // 3. Directorio padre del ejecutable
                Path.Combine(AppContext.BaseDirectory, "schema.sql"),
                // 4. Tres niveles arriba del ejecutable
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "schema.sql")
            };

            string? schemaPath = null;
            foreach (var path in possiblePaths)
            {
                var normalizedPath = Path.GetFullPath(path);
                if (File.Exists(normalizedPath))
                {
                    schemaPath = normalizedPath;
                    break;
                }
            }

            if (schemaPath == null)
            {
                var searchedPaths = string.Join("\n  - ", possiblePaths.Select(Path.GetFullPath));
                throw new FileNotFoundException(
                    $"No se encontró el archivo schema.sql en ninguna de estas ubicaciones:\n  - {searchedPaths}");
            }

            var schema = await File.ReadAllTextAsync(schemaPath);
            
            // Limpiar comentarios de una línea (--) y multi-línea (/* */)
            var cleanedSchema = System.Text.RegularExpressions.Regex.Replace(
                schema, 
                @"--[^\r\n]*|/\*[\s\S]*?\*/", 
                string.Empty, 
                System.Text.RegularExpressions.RegexOptions.Multiline
            );
            
            // Dividir por comandos individuales (separados por ;)
            var commands = cleanedSchema.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            
            int executedCount = 0;
            
            foreach (var command in commands)
            {
                var trimmedCommand = command.Trim();
                
                // Filtrar comandos que no necesitamos ejecutar o que causan problemas
                if (string.IsNullOrWhiteSpace(trimmedCommand) ||
                    trimmedCommand.StartsWith("CREATE DATABASE", StringComparison.OrdinalIgnoreCase) ||
                    trimmedCommand.StartsWith("USE ", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                try
                {
                    await _context.Database.ExecuteSqlRawAsync(trimmedCommand);
                    executedCount++;
                }
                catch (Exception ex)
                {
                    // Ignorar errores comunes que no afectan la funcionalidad
                    if (!ex.Message.Contains("database exists") && 
                        !ex.Message.Contains("already exists"))
                    {
                        Console.WriteLine($"❌ Error ejecutando comando SQL: {ex.Message}");
                        throw; // Re-lanzar para que el test falle si es un error crítico
                    }
                }
            }
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
            await _context.DisposeAsync();
            await _mySqlContainer.DisposeAsync();
        }
    }
}
