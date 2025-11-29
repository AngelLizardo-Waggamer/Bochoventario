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
    public class UpdateProductTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly InventoryController _controller;

        public UpdateProductTests()
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
                }
            };
            _context.Articulos.AddRange(articulos);

            var inventarios = new List<Inventario>
            {
                new Inventario
                {
                    IdInventario = 1,
                    IdArticulo = 1,
                    Cantidad = 10,
                    Ubicacion = "Almacen A",
                    UltimaModificacionPor = 1,
                    UltimaActualizacion = DateTime.Now.AddDays(-1)
                },
                new Inventario
                {
                    IdInventario = 2,
                    IdArticulo = 1,
                    Cantidad = 5,
                    Ubicacion = "Almacen B",
                    UltimaModificacionPor = 1,
                    UltimaActualizacion = DateTime.Now.AddDays(-1)
                }
            };
            _context.Inventarios.AddRange(inventarios);

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
        public async Task UpdateProduct_ComoAdministrador_RetornaNoContent()
        {
            // Arrange
            SetupUserClaims(1); // Usuario Administrador
            var request = new UpdateProductRequest
            {
                Articulo = new Articulo
                {
                    IdArticulo = 1,
                    Sku = "SKU-001-ACTUALIZADO",
                    Nombre = "Laptop Dell Actualizada",
                    Descripcion = "Descripcion actualizada",
                    PrecioCosto = 16000.00m
                }
            };

            // Act
            var result = await _controller.UpdateProduct(1, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            // Verificar que el producto se actualizó
            var productoActualizado = await _context.Articulos.FindAsync(1);
            Assert.NotNull(productoActualizado);
            Assert.Equal("SKU-001-ACTUALIZADO", productoActualizado.Sku);
            Assert.Equal("Laptop Dell Actualizada", productoActualizado.Nombre);
            Assert.Equal(16000.00m, productoActualizado.PrecioCosto);
        }

        [Fact]
        public async Task UpdateProduct_ComoGestor_ActualizaInventarios()
        {
            // Arrange
            SetupUserClaims(2); // Usuario Gestor
            var fechaAntes = DateTime.Now.AddDays(-1);
            var request = new UpdateProductRequest
            {
                Articulo = new Articulo
                {
                    IdArticulo = 1,
                    Sku = "SKU-001-MODIFICADO",
                    Nombre = "Laptop Dell Modificada",
                    Descripcion = "Modificada por gestor",
                    PrecioCosto = 15500.00m
                }
            };

            // Act
            var result = await _controller.UpdateProduct(1, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            // Verificar que los inventarios se actualizaron
            var inventarios = await _context.Inventarios.Where(i => i.IdArticulo == 1).ToListAsync();
            Assert.All(inventarios, inv => 
            {
                Assert.Equal(2, inv.UltimaModificacionPor); // Gestor
                Assert.True(inv.UltimaActualizacion > fechaAntes);
            });
        }

        [Fact]
        public async Task UpdateProduct_IDNoCoincide_RetornaBadRequest()
        {
            // Arrange
            SetupUserClaims(1);
            var request = new UpdateProductRequest
            {
                Articulo = new Articulo
                {
                    IdArticulo = 5, // Diferente al parámetro de la URL
                    Sku = "SKU-001",
                    Nombre = "Laptop",
                    Descripcion = "Descripcion",
                    PrecioCosto = 15000.00m
                }
            };

            // Act
            var result = await _controller.UpdateProduct(1, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            dynamic mensaje = badRequestResult.Value!;
            Assert.Equal("El ID del producto no coincide", mensaje.message.ToString());
        }

        [Fact]
        public async Task UpdateProduct_SinAutenticacion_RetornaUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            
            var request = new UpdateProductRequest
            {
                Articulo = new Articulo
                {
                    IdArticulo = 1,
                    Sku = "SKU-001",
                    Nombre = "Laptop",
                    Descripcion = "Descripcion",
                    PrecioCosto = 15000.00m
                }
            };

            // Act
            var result = await _controller.UpdateProduct(1, request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            dynamic mensaje = unauthorizedResult.Value!;
            Assert.Equal("No se pudo obtener el ID del usuario del token JWT", mensaje.message.ToString());
        }

        [Fact]
        public async Task UpdateProduct_UsuarioSinTokenValido_RetornaUnauthorized()
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
            
            var request = new UpdateProductRequest
            {
                Articulo = new Articulo
                {
                    IdArticulo = 1,
                    Sku = "SKU-001",
                    Nombre = "Laptop",
                    Descripcion = "Descripcion",
                    PrecioCosto = 15000.00m
                }
            };

            // Act
            var result = await _controller.UpdateProduct(1, request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            dynamic mensaje = unauthorizedResult.Value!;
            Assert.Equal("No se pudo obtener el ID del usuario del token JWT", mensaje.message.ToString());
        }

        [Fact]
        public async Task UpdateProduct_UsuarioRolLector_RetornaUnauthorized()
        {
            // Arrange
            SetupUserClaims(3); // Usuario Lector
            var request = new UpdateProductRequest
            {
                Articulo = new Articulo
                {
                    IdArticulo = 1,
                    Sku = "SKU-001",
                    Nombre = "Laptop Actualizada",
                    Descripcion = "Descripcion",
                    PrecioCosto = 15000.00m
                }
            };

            // Act
            var result = await _controller.UpdateProduct(1, request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            dynamic mensaje = unauthorizedResult.Value!;
            Assert.Contains("no tiene permisos suficientes", mensaje.message.ToString());
        }

        [Fact]
        public async Task UpdateProduct_ProductoNoEncontrado_RetornaNotFound()
        {
            // Arrange
            SetupUserClaims(1);
            var request = new UpdateProductRequest
            {
                Articulo = new Articulo
                {
                    IdArticulo = 999,
                    Sku = "SKU-999",
                    Nombre = "Producto Inexistente",
                    Descripcion = "No existe",
                    PrecioCosto = 1000.00m
                }
            };

            // Act
            var result = await _controller.UpdateProduct(999, request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            dynamic mensaje = notFoundResult.Value!;
            Assert.Contains("Producto con ID 999 no encontrado", mensaje.message.ToString());
        }

        [Fact]
        public async Task UpdateProduct_SKUDuplicadoConOtroProducto_RetornaConflict()
        {
            // Arrange
            SetupUserClaims(1);
            var request = new UpdateProductRequest
            {
                Articulo = new Articulo
                {
                    IdArticulo = 1,
                    Sku = "SKU-002", // SKU del producto 2
                    Nombre = "Laptop Dell",
                    Descripcion = "Descripcion",
                    PrecioCosto = 15000.00m
                }
            };

            // Act
            var result = await _controller.UpdateProduct(1, request);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            dynamic mensaje = conflictResult.Value!;
            Assert.Contains("ya está en uso por otro producto", mensaje.message.ToString());
        }

        [Fact]
        public async Task UpdateProduct_UsuarioNoExisteEnBD_RetornaUnauthorized()
        {
            // Arrange
            SetupUserClaims(999); // Usuario que no existe
            var request = new UpdateProductRequest
            {
                Articulo = new Articulo
                {
                    IdArticulo = 1,
                    Sku = "SKU-001",
                    Nombre = "Laptop",
                    Descripcion = "Descripcion",
                    PrecioCosto = 15000.00m
                }
            };

            // Act
            var result = await _controller.UpdateProduct(1, request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            dynamic mensaje = unauthorizedResult.Value!;
            Assert.Contains("Usuario con ID 999 no encontrado", mensaje.message.ToString());
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
