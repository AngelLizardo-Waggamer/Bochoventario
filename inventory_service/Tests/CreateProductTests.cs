using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using inventory_service.Controllers;
using inventory_service.Data;
using inventory_service.Models;

namespace inventory_service.Tests
{
    public class CreateProductTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly InventoryController _controller;

        public CreateProductTests()
        {
            // Configurar base de datos en memoria
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            
            // Seed data inicial
            SeedDatabase();
            
            _controller = new InventoryController(_context);
        }

        private void SeedDatabase()
        {
            var roles = new List<Rol>
            {
                new Rol { IdRol = 1, NombreRol = "Administrador" },
                new Rol { IdRol = 2, NombreRol = "Gestor" },
                new Rol { IdRol = 3, NombreRol = "Lector" }
            };
            _context.Roles.AddRange(roles);

            var usuarios = new List<Usuario>
            {
                new Usuario
                {
                    IdUsuario = 1,
                    IdRol = 1,
                    NombreUsuario = "admin",
                    PasswordHash = "hash",
                    NombreCompleto = "Administrador"
                },
                new Usuario
                {
                    IdUsuario = 2,
                    IdRol = 2,
                    NombreUsuario = "gestor",
                    PasswordHash = "hash",
                    NombreCompleto = "Gestor"
                },
                new Usuario
                {
                    IdUsuario = 3,
                    IdRol = 3,
                    NombreUsuario = "lector",
                    PasswordHash = "hash",
                    NombreCompleto = "Lector"
                }
            };
            _context.Usuarios.AddRange(usuarios);

            // Artículo existente para probar SKU duplicado
            var articuloExistente = new Articulo
            {
                IdArticulo = 1,
                Sku = "SKU-EXISTENTE",
                Nombre = "Producto Existente",
                Descripcion = "Descripcion del producto existente",
                PrecioCosto = 1000.00m
            };
            _context.Articulos.Add(articuloExistente);

            _context.SaveChanges();
        }

        private void SetupUserClaims(int userId, int roleId = 1)
        {
            var username = userId == 1 ? "admin" : userId == 2 ? "gestor" : "lector";
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

        [Fact]
        public async Task CreateProduct_ComoAdministrador_RetornaCreated()
        {
            // Arrange
            SetupUserClaims(1, 1); // Usuario Administrador (id_rol = 1)
            var request = new CreateProductRequest
            {
                Articulo = new Articulo
                {
                    Sku = "SKU-NUEVO-001",
                    Nombre = "Laptop HP",
                    Descripcion = "Laptop nueva",
                    PrecioCosto = 18000.00m
                }
            };

            // Act
            var result = await _controller.CreateProduct(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var producto = Assert.IsType<Articulo>(createdResult.Value);
            Assert.Equal("SKU-NUEVO-001", producto.Sku);
            Assert.Equal("Laptop HP", producto.Nombre);
            Assert.True(producto.IdArticulo > 0);
        }

        [Fact]
        public async Task CreateProduct_ComoGestor_RetornaCreated()
        {
            // Arrange
            SetupUserClaims(2, 2); // Usuario Gestor (id_rol = 2)
            var request = new CreateProductRequest
            {
                Articulo = new Articulo
                {
                    Sku = "SKU-NUEVO-002",
                    Nombre = "Teclado USB",
                    Descripcion = "Teclado economico",
                    PrecioCosto = 250.00m
                }
            };

            // Act
            var result = await _controller.CreateProduct(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var producto = Assert.IsType<Articulo>(createdResult.Value);
            Assert.Equal("SKU-NUEVO-002", producto.Sku);
            Assert.Equal("Teclado USB", producto.Nombre);
        }

        [Fact]
        public async Task CreateProduct_SinAutenticacion_RetornaUnauthorized()
        {
            // Arrange - No se configura ningún claim
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            
            var request = new CreateProductRequest
            {
                Articulo = new Articulo
                {
                    Sku = "SKU-NUEVO-003",
                    Nombre = "Mouse",
                    Descripcion = "Mouse",
                    PrecioCosto = 300.00m
                }
            };

            // Act
            var result = await _controller.CreateProduct(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            dynamic mensaje = unauthorizedResult.Value!;
            Assert.Equal("No se pudo obtener el ID del usuario del token JWT", mensaje.message.ToString());
        }

        [Fact]
        public async Task CreateProduct_UsuarioSinTokenValido_RetornaUnauthorized()
        {
            // Arrange - Configurar claims sin id_usuario válido
            var claims = new List<Claim>
            {
                new Claim("nombre_usuario", "test"),
                new Claim("id_rol", "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
            
            var request = new CreateProductRequest
            {
                Articulo = new Articulo
                {
                    Sku = "SKU-NUEVO-004",
                    Nombre = "Monitor",
                    Descripcion = "Monitor LED",
                    PrecioCosto = 3000.00m
                }
            };

            // Act
            var result = await _controller.CreateProduct(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            dynamic mensaje = unauthorizedResult.Value!;
            Assert.Equal("No se pudo obtener el ID del usuario del token JWT", mensaje.message.ToString());
        }

        [Fact]
        public async Task CreateProduct_UsuarioRolLector_RetornaUnauthorized()
        {
            // Arrange
            SetupUserClaims(3, 3); // Usuario Lector (id_rol = 3)
            var request = new CreateProductRequest
            {
                Articulo = new Articulo
                {
                    Sku = "SKU-NUEVO-005",
                    Nombre = "Impresora",
                    Descripcion = "Impresora laser",
                    PrecioCosto = 5000.00m
                }
            };

            // Act
            var result = await _controller.CreateProduct(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            dynamic mensaje = unauthorizedResult.Value!;
            Assert.Contains("no tiene permisos suficientes", mensaje.message.ToString());
        }

        [Fact]
        public async Task CreateProduct_SKUDuplicado_RetornaConflict()
        {
            // Arrange
            SetupUserClaims(1); // Usuario Administrador
            var request = new CreateProductRequest
            {
                Articulo = new Articulo
                {
                    Sku = "SKU-EXISTENTE", // SKU que ya existe
                    Nombre = "Otro Producto",
                    Descripcion = "Otro producto",
                    PrecioCosto = 2000.00m
                }
            };

            // Act
            var result = await _controller.CreateProduct(request);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
            dynamic mensaje = conflictResult.Value!;
            Assert.Contains("Ya existe un producto con el SKU", mensaje.message.ToString());
        }

        [Fact]
        public async Task CreateProduct_ConTokenValido_RetornaCreated()
        {
            // Arrange - Usuario que no existe en BD pero tiene token válido
            SetupUserClaims(999, 1); // Administrador
            var request = new CreateProductRequest
            {
                Articulo = new Articulo
                {
                    Sku = "SKU-NUEVO-006",
                    Nombre = "Tablet",
                    Descripcion = "Tablet Android",
                    PrecioCosto = 4000.00m
                }
            };

            // Act
            var result = await _controller.CreateProduct(request);

            // Assert - Ahora debe funcionar porque valida solo el token, no la BD
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var producto = Assert.IsType<Articulo>(createdResult.Value);
            Assert.Equal("SKU-NUEVO-006", producto.Sku);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
