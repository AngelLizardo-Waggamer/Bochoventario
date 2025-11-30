using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using inventory_service.Controllers;
using inventory_service.Data;
using inventory_service.Models;

namespace inventory_service.Tests
{
    /// <summary>
    /// Tests unitarios para la creación de registros de inventario (stock)
    /// </summary>
    public class CreateInventoryTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);
            
            context.Roles.AddRange(
                new Rol { IdRol = 1, NombreRol = "Administrador" },
                new Rol { IdRol = 2, NombreRol = "Gestor" },
                new Rol { IdRol = 3, NombreRol = "Lector" }
            );
            
            context.Usuarios.AddRange(
                new Usuario { IdUsuario = 1, IdRol = 1, NombreUsuario = "admin", PasswordHash = "hash", NombreCompleto = "Admin User" },
                new Usuario { IdUsuario = 2, IdRol = 2, NombreUsuario = "gestor", PasswordHash = "hash", NombreCompleto = "Gestor User" },
                new Usuario { IdUsuario = 3, IdRol = 3, NombreUsuario = "lector", PasswordHash = "hash", NombreCompleto = "Lector User" }
            );
            
            context.SaveChanges();
            return context;
        }

        private void SetupUserClaims(InventoryController controller, int userId, int roleId, string username)
        {
            var claims = new[]
            {
                new Claim("id_usuario", userId.ToString()),
                new Claim("nombre_usuario", username),
                new Claim("id_rol", roleId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public void CreateInventory_ComoAdministrador_CreaCorrectamente()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            var request = new CreateInventoryRequest
            {
                IdArticulo = 1,
                Cantidad = 100,
                Ubicacion = "Almacen A"
            };

            // Act
            var result = controller.CreateInventory(request).Result;

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var inventario = Assert.IsType<Inventario>(createdResult.Value);
            Assert.Equal(100, inventario.Cantidad);
            Assert.Equal("Almacen A", inventario.Ubicacion);
            Assert.Equal(1, inventario.UltimaModificacionPor);
        }

        [Fact]
        public void CreateInventory_ComoGestor_CreaCorrectamente()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 2, 2, "gestor");

            var request = new CreateInventoryRequest
            {
                IdArticulo = 1,
                Cantidad = 50,
                Ubicacion = "Almacen B"
            };

            // Act
            var result = controller.CreateInventory(request).Result;

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var inventario = Assert.IsType<Inventario>(createdResult.Value);
            Assert.Equal(2, inventario.UltimaModificacionPor);
        }

        [Fact]
        public void CreateInventory_ComoLector_RetornaUnauthorized()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 3, 3, "lector");

            var request = new CreateInventoryRequest
            {
                IdArticulo = 1,
                Cantidad = 50,
                Ubicacion = "Almacen A"
            };

            // Act
            var result = controller.CreateInventory(request).Result;

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            dynamic mensaje = unauthorizedResult.Value!;
            Assert.Contains("no tiene permisos suficientes", mensaje.message.ToString());
        }

        [Fact]
        public void CreateInventory_SinAutenticacion_RetornaUnauthorized()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            context.SaveChanges();

            var controller = new InventoryController(context);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var request = new CreateInventoryRequest
            {
                IdArticulo = 1,
                Cantidad = 50,
                Ubicacion = "Almacen A"
            };

            // Act
            var result = controller.CreateInventory(request).Result;

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        [Fact]
        public void CreateInventory_ArticuloNoExiste_RetornaNotFound()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            var request = new CreateInventoryRequest
            {
                IdArticulo = 999,
                Cantidad = 50,
                Ubicacion = "Almacen A"
            };

            // Act
            var result = controller.CreateInventory(request).Result;

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            dynamic mensaje = notFoundResult.Value!;
            Assert.Contains("no encontrado", mensaje.message.ToString());
        }

        [Fact]
        public void CreateInventory_InventarioDuplicado_RetornaConflict()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            
            var inventarioExistente = new Inventario 
            { 
                IdInventario = 1, 
                IdArticulo = 1, 
                Cantidad = 50, 
                Ubicacion = "Almacen A", 
                UltimaModificacionPor = 1 
            };
            context.Inventarios.Add(inventarioExistente);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            var request = new CreateInventoryRequest
            {
                IdArticulo = 1,
                Cantidad = 100,
                Ubicacion = "Almacen A"
            };

            // Act
            var result = controller.CreateInventory(request).Result;

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
            dynamic mensaje = conflictResult.Value!;
            Assert.Contains("Ya existe un registro de inventario", mensaje.message.ToString());
        }

        [Fact]
        public void CreateInventory_CantidadCero_CreaCorrectamente()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            var request = new CreateInventoryRequest
            {
                IdArticulo = 1,
                Cantidad = 0,
                Ubicacion = "Almacen A"
            };

            // Act
            var result = controller.CreateInventory(request).Result;

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var inventario = Assert.IsType<Inventario>(createdResult.Value);
            Assert.Equal(0, inventario.Cantidad);
        }

        [Fact]
        public void CreateInventory_CantidadNegativa_CreaCorrectamente()
        {
            // Arrange - Puede ser útil para ajustes de inventario negativo
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            var request = new CreateInventoryRequest
            {
                IdArticulo = 1,
                Cantidad = -10,
                Ubicacion = "Almacen A"
            };

            // Act
            var result = controller.CreateInventory(request).Result;

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var inventario = Assert.IsType<Inventario>(createdResult.Value);
            Assert.Equal(-10, inventario.Cantidad);
        }

        [Fact]
        public void CreateInventory_UbicacionConCaracteresEspeciales_CreaCorrectamente()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            var request = new CreateInventoryRequest
            {
                IdArticulo = 1,
                Cantidad = 100,
                Ubicacion = "Almacén #1 - Sección A/B (Principal)"
            };

            // Act
            var result = controller.CreateInventory(request).Result;

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var inventario = Assert.IsType<Inventario>(createdResult.Value);
            Assert.Equal("Almacén #1 - Sección A/B (Principal)", inventario.Ubicacion);
        }

        [Fact]
        public void CreateInventory_MismoArticuloDiferentesUbicaciones_CreaTodos()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            var request1 = new CreateInventoryRequest
            {
                IdArticulo = 1,
                Cantidad = 50,
                Ubicacion = "Almacen A"
            };

            var request2 = new CreateInventoryRequest
            {
                IdArticulo = 1,
                Cantidad = 30,
                Ubicacion = "Almacen B"
            };

            // Act
            var result1 = controller.CreateInventory(request1).Result;
            var result2 = controller.CreateInventory(request2).Result;

            // Assert
            Assert.IsType<CreatedAtActionResult>(result1.Result);
            Assert.IsType<CreatedAtActionResult>(result2.Result);
            
            var inventarios = context.Inventarios.Where(i => i.IdArticulo == 1).ToList();
            Assert.Equal(2, inventarios.Count);
        }

        [Fact]
        public void CreateInventory_CantidadMuyGrande_CreaCorrectamente()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            var request = new CreateInventoryRequest
            {
                IdArticulo = 1,
                Cantidad = int.MaxValue,
                Ubicacion = "Almacen A"
            };

            // Act
            var result = controller.CreateInventory(request).Result;

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var inventario = Assert.IsType<Inventario>(createdResult.Value);
            Assert.Equal(int.MaxValue, inventario.Cantidad);
        }

        [Fact]
        public void CreateInventory_RegistraFechaDeModificacion()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            var request = new CreateInventoryRequest
            {
                IdArticulo = 1,
                Cantidad = 100,
                Ubicacion = "Almacen A"
            };

            var beforeTime = System.DateTime.Now.AddSeconds(-1);

            // Act
            var result = controller.CreateInventory(request).Result;

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var inventario = Assert.IsType<Inventario>(createdResult.Value);
            
            var afterTime = System.DateTime.Now.AddSeconds(1);
            Assert.True(inventario.UltimaActualizacion >= beforeTime);
            Assert.True(inventario.UltimaActualizacion <= afterTime);
        }
    }
}
