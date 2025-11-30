using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using Xunit;
using inventory_service.Data;
using inventory_service.Models;

namespace inventory_service.IntegrationTests
{
    /// <summary>
    /// Fixture que mantiene una única instancia del contenedor MySQL
    /// compartida entre todos los tests de integración.
    /// Esto reduce significativamente el tiempo de ejecución de los tests.
    /// </summary>
    public class DatabaseFixture : IAsyncLifetime
    {
        private MySqlContainer _mySqlContainer = null!;
        public string ConnectionString { get; private set; } = string.Empty;

        public async Task InitializeAsync()
        {
            // Configurar y levantar el contenedor MySQL UNA SOLA VEZ
            _mySqlContainer = new MySqlBuilder()
                .WithImage("mysql:8.4")
                .WithDatabase("Bochoventario")
                .WithUsername("test_user")
                .WithPassword("test_password")
                .Build();

            await _mySqlContainer.StartAsync();
            ConnectionString = _mySqlContainer.GetConnectionString();

            // Aplicar el schema una sola vez
            await ApplyDatabaseSchema();
        }

        private async Task ApplyDatabaseSchema()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseMySql(ConnectionString, ServerVersion.AutoDetect(ConnectionString))
                .Options;

            using var context = new AppDbContext(options);

            // Buscar el archivo schema.sql
            var possiblePaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "schema.sql"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "schema.sql"),
                Path.Combine(AppContext.BaseDirectory, "schema.sql"),
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
                throw new FileNotFoundException("No se encontró el archivo schema.sql");
            }

            var schema = await File.ReadAllTextAsync(schemaPath);
            
            // Limpiar comentarios
            var cleanedSchema = System.Text.RegularExpressions.Regex.Replace(
                schema, 
                @"--[^\r\n]*|/\*[\s\S]*?\*/", 
                string.Empty, 
                System.Text.RegularExpressions.RegexOptions.Multiline
            );
            
            // Ejecutar comandos SQL
            var commands = cleanedSchema.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var command in commands)
            {
                var trimmedCommand = command.Trim();
                
                if (string.IsNullOrWhiteSpace(trimmedCommand) ||
                    trimmedCommand.StartsWith("CREATE DATABASE", StringComparison.OrdinalIgnoreCase) ||
                    trimmedCommand.StartsWith("USE ", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                try
                {
                    await context.Database.ExecuteSqlRawAsync(trimmedCommand);
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("database exists") && 
                        !ex.Message.Contains("already exists"))
                    {
                        Console.WriteLine($"Error ejecutando comando SQL: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public async Task DisposeAsync()
        {
            // Limpiar el contenedor al final de todos los tests
            await _mySqlContainer.DisposeAsync();
        }

        /// <summary>
        /// Crea un nuevo DbContext conectado al contenedor compartido
        /// </summary>
        public AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseMySql(ConnectionString, ServerVersion.AutoDetect(ConnectionString))
                .Options;

            return new AppDbContext(options);
        }
    }
}
