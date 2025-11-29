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
    public class DeleteProductTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly InventoryController _controller;

        public DeleteProductTests()
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

            var articulos = new List<Articulo>
            {
                new Articulo
                {
                    IdArticulo = 1,
                    Sku = "SKU-001",
                    Nombre = "Laptop Dell",
                    Descripcion = "Laptop para oficina",
                    PrecioCosto = 15000.00m
                },
                new Articulo
                {
                    IdArticulo = 2,
                    Sku = "SKU-002",
                    Nombre = "Mouse Logitech",
                    Descripcion = "Mouse inalambrico",
                    PrecioCosto = 350.00m
                },
                new Articulo
                {
                    IdArticulo = 3,
                    Sku = "SKU-003",
                    Nombre = "Teclado Mecanico",
                    Descripcion = "Teclado RGB",
                    PrecioCosto = 1200.00m
                }
            };
            _context.Articulos.AddRange(articulos);

            _context.SaveChanges();
        }

        private void SetupUserClaims(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task DeleteProduct_ComoAdministrador_RetornaNoContent()
        {
            // Arrange
            SetupUserClaims(1); // Usuario Administrador

            // Act
            var result = await _controller.DeleteProduct(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            // Verificar que el producto fue eliminado
            var productoEliminado = await _context.Articulos.FindAsync(1);
            Assert.Null(productoEliminado);
        }

        [Fact]
        public async Task DeleteProduct_ComoGestor_RetornaNoContent()
        {
            // Arrange
            SetupUserClaims(2); // Usuario Gestor

            // Act
            var result = await _controller.DeleteProduct(2);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            // Verificar que el producto fue eliminado
            var productoEliminado = await _context.Articulos.FindAsync(2);
            Assert.Null(productoEliminado);
        }

        [Fact]
        public async Task DeleteProduct_SinAutenticacion_RetornaUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await _controller.DeleteProduct(1);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            dynamic mensaje = unauthorizedResult.Value!;
            Assert.Equal("No se pudo obtener el ID del usuario del token JWT", mensaje.message.ToString());
            
            // Verificar que el producto NO fue eliminado
            var producto = await _context.Articulos.FindAsync(1);
            Assert.NotNull(producto);
        }

        [Fact]
        public async Task DeleteProduct_UsuarioSinTokenValido_RetornaUnauthorized()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("otherClaim", "valor")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.DeleteProduct(1);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            dynamic mensaje = unauthorizedResult.Value!;
            Assert.Equal("No se pudo obtener el ID del usuario del token JWT", mensaje.message.ToString());
            
            // Verificar que el producto NO fue eliminado
            var producto = await _context.Articulos.FindAsync(1);
            Assert.NotNull(producto);
        }

        [Fact]
        public async Task DeleteProduct_UsuarioRolLector_RetornaUnauthorized()
        {
            // Arrange
            SetupUserClaims(3); // Usuario Lector

            // Act
            var result = await _controller.DeleteProduct(1);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            dynamic mensaje = unauthorizedResult.Value!;
            Assert.Contains("no tiene permisos suficientes", mensaje.message.ToString());
            
            // Verificar que el producto NO fue eliminado
            var producto = await _context.Articulos.FindAsync(1);
            Assert.NotNull(producto);
        }

        [Fact]
        public async Task DeleteProduct_ProductoNoEncontrado_RetornaNotFound()
        {
            // Arrange
            SetupUserClaims(1);

            // Act
            var result = await _controller.DeleteProduct(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            dynamic mensaje = notFoundResult.Value!;
            Assert.Contains("Producto con ID 999 no encontrado", mensaje.message.ToString());
        }

        [Fact]
        public async Task DeleteProduct_UsuarioNoExisteEnBD_RetornaUnauthorized()
        {
            // Arrange
            SetupUserClaims(999); // Usuario que no existe

            // Act
            var result = await _controller.DeleteProduct(1);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            dynamic mensaje = unauthorizedResult.Value!;
            Assert.Contains("Usuario con ID 999 no encontrado", mensaje.message.ToString());
            
            // Verificar que el producto NO fue eliminado
            var producto = await _context.Articulos.FindAsync(1);
            Assert.NotNull(producto);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
