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
    /// Tests unitarios para la actualización y ajuste de inventario (stock)
    /// </summary>
    public class UpdateInventoryTests
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
        public void UpdateInventory_ComoAdministrador_ActualizaCorrectamente()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            
            var inventario = new Inventario 
            { 
                IdInventario = 1, 
                IdArticulo = 1, 
                Cantidad = 50, 
                Ubicacion = "Almacen A", 
                UltimaModificacionPor = 2 
            };
            context.Inventarios.Add(inventario);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            var request = new UpdateInventoryRequest { Cantidad = 100 };

            // Act
            var result = controller.UpdateInventory(1, request).Result;

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            var updated = context.Inventarios.Find(1);
            Assert.Equal(100, updated!.Cantidad);
            Assert.Equal(1, updated.UltimaModificacionPor); // Cambió el modificador
        }

        [Fact]
        public void UpdateInventory_ComoGestor_ActualizaCorrectamente()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            
            var inventario = new Inventario 
            { 
                IdInventario = 1, 
                IdArticulo = 1, 
                Cantidad = 50, 
                Ubicacion = "Almacen A", 
                UltimaModificacionPor = 1 
            };
            context.Inventarios.Add(inventario);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 2, 2, "gestor");

            var request = new UpdateInventoryRequest { Cantidad = 75 };

            // Act
            var result = controller.UpdateInventory(1, request).Result;

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(75, context.Inventarios.Find(1)!.Cantidad);
        }

        [Fact]
        public void UpdateInventory_ComoLector_RetornaUnauthorized()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            
            var inventario = new Inventario 
            { 
                IdInventario = 1, 
                IdArticulo = 1, 
                Cantidad = 50, 
                Ubicacion = "Almacen A", 
                UltimaModificacionPor = 1 
            };
            context.Inventarios.Add(inventario);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 3, 3, "lector");

            var request = new UpdateInventoryRequest { Cantidad = 75 };

            // Act
            var result = controller.UpdateInventory(1, request).Result;

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(50, context.Inventarios.Find(1)!.Cantidad); // No cambió
        }

        [Fact]
        public void UpdateInventory_InventarioNoExiste_RetornaNotFound()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            var request = new UpdateInventoryRequest { Cantidad = 100 };

            // Act
            var result = controller.UpdateInventory(999, request).Result;

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            dynamic mensaje = notFoundResult.Value!;
            Assert.Contains("no encontrado", mensaje.message.ToString());
        }

        [Fact]
        public void UpdateInventory_CantidadCero_ActualizaCorrectamente()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            
            var inventario = new Inventario 
            { 
                IdInventario = 1, 
                IdArticulo = 1, 
                Cantidad = 50, 
                Ubicacion = "Almacen A", 
                UltimaModificacionPor = 1 
            };
            context.Inventarios.Add(inventario);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            var request = new UpdateInventoryRequest { Cantidad = 0 };

            // Act
            var result = controller.UpdateInventory(1, request).Result;

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(0, context.Inventarios.Find(1)!.Cantidad);
        }

        [Fact]
        public void UpdateInventory_CantidadNegativa_ActualizaCorrectamente()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            
            var inventario = new Inventario 
            { 
                IdInventario = 1, 
                IdArticulo = 1, 
                Cantidad = 50, 
                Ubicacion = "Almacen A", 
                UltimaModificacionPor = 1 
            };
            context.Inventarios.Add(inventario);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            var request = new UpdateInventoryRequest { Cantidad = -10 };

            // Act
            var result = controller.UpdateInventory(1, request).Result;

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(-10, context.Inventarios.Find(1)!.Cantidad);
        }

        // === TESTS PARA ADJUST INVENTORY ===

        [Fact]
        public void AdjustInventory_AjustePositivo_IncrementaCantidad()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            
            var inventario = new Inventario 
            { 
                IdInventario = 1, 
                IdArticulo = 1, 
                Cantidad = 50, 
                Ubicacion = "Almacen A", 
                UltimaModificacionPor = 1 
            };
            context.Inventarios.Add(inventario);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            var request = new AdjustInventoryRequest { Ajuste = 25 };

            // Act
            var result = controller.AdjustInventory(1, request).Result;

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic response = okResult.Value!;
            Assert.Equal(75, (int)response.cantidad);
            Assert.Equal(75, context.Inventarios.Find(1)!.Cantidad);
        }

        [Fact]
        public void AdjustInventory_AjusteNegativo_DecrementaCantidad()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            
            var inventario = new Inventario 
            { 
                IdInventario = 1, 
                IdArticulo = 1, 
                Cantidad = 50, 
                Ubicacion = "Almacen A", 
                UltimaModificacionPor = 1 
            };
            context.Inventarios.Add(inventario);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            var request = new AdjustInventoryRequest { Ajuste = -20 };

            // Act
            var result = controller.AdjustInventory(1, request).Result;

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic response = okResult.Value!;
            Assert.Equal(30, (int)response.cantidad);
        }

        [Fact]
        public void AdjustInventory_AjusteQueResultaEnNegativo_RetornaBadRequest()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            
            var inventario = new Inventario 
            { 
                IdInventario = 1, 
                IdArticulo = 1, 
                Cantidad = 50, 
                Ubicacion = "Almacen A", 
                UltimaModificacionPor = 1 
            };
            context.Inventarios.Add(inventario);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            var request = new AdjustInventoryRequest { Ajuste = -100 };

            // Act
            var result = controller.AdjustInventory(1, request).Result;

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            dynamic mensaje = badRequestResult.Value!;
            Assert.Contains("No hay suficiente stock", mensaje.message.ToString());
            Assert.Equal(50, context.Inventarios.Find(1)!.Cantidad); // No cambió
        }

        [Fact]
        public void AdjustInventory_AjusteExactoACero_Funciona()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            
            var inventario = new Inventario 
            { 
                IdInventario = 1, 
                IdArticulo = 1, 
                Cantidad = 50, 
                Ubicacion = "Almacen A", 
                UltimaModificacionPor = 1 
            };
            context.Inventarios.Add(inventario);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            var request = new AdjustInventoryRequest { Ajuste = -50 };

            // Act
            var result = controller.AdjustInventory(1, request).Result;

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, context.Inventarios.Find(1)!.Cantidad);
        }

        [Fact]
        public void AdjustInventory_AjusteCero_NoModificaCantidad()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            
            var inventario = new Inventario 
            { 
                IdInventario = 1, 
                IdArticulo = 1, 
                Cantidad = 50, 
                Ubicacion = "Almacen A", 
                UltimaModificacionPor = 1 
            };
            context.Inventarios.Add(inventario);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            var request = new AdjustInventoryRequest { Ajuste = 0 };

            // Act
            var result = controller.AdjustInventory(1, request).Result;

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(50, context.Inventarios.Find(1)!.Cantidad);
        }

        [Fact]
        public void AdjustInventory_ComoLector_RetornaUnauthorized()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            
            var inventario = new Inventario 
            { 
                IdInventario = 1, 
                IdArticulo = 1, 
                Cantidad = 50, 
                Ubicacion = "Almacen A", 
                UltimaModificacionPor = 1 
            };
            context.Inventarios.Add(inventario);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 3, 3, "lector");

            var request = new AdjustInventoryRequest { Ajuste = 10 };

            // Act
            var result = controller.AdjustInventory(1, request).Result;

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(50, context.Inventarios.Find(1)!.Cantidad);
        }

        [Fact]
        public void AdjustInventory_InventarioNoExiste_RetornaNotFound()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            var request = new AdjustInventoryRequest { Ajuste = 10 };

            // Act
            var result = controller.AdjustInventory(999, request).Result;

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public void AdjustInventory_AjusteMuyGrande_ManejaCorrectamente()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            
            var inventario = new Inventario 
            { 
                IdInventario = 1, 
                IdArticulo = 1, 
                Cantidad = 100, 
                Ubicacion = "Almacen A", 
                UltimaModificacionPor = 1 
            };
            context.Inventarios.Add(inventario);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            var request = new AdjustInventoryRequest { Ajuste = 1000000 };

            // Act
            var result = controller.AdjustInventory(1, request).Result;

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(1000100, context.Inventarios.Find(1)!.Cantidad);
        }

        [Fact]
        public void AdjustInventory_ActualizaFechaYUsuario()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            
            var inventario = new Inventario 
            { 
                IdInventario = 1, 
                IdArticulo = 1, 
                Cantidad = 50, 
                Ubicacion = "Almacen A", 
                UltimaModificacionPor = 1,
                UltimaActualizacion = System.DateTime.Now.AddDays(-1)
            };
            context.Inventarios.Add(inventario);
            context.SaveChanges();

            var fechaAntes = inventario.UltimaActualizacion;

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 2, 2, "gestor");

            var request = new AdjustInventoryRequest { Ajuste = 10 };

            // Act
            var result = controller.AdjustInventory(1, request).Result;

            // Assert
            var updated = context.Inventarios.Find(1)!;
            Assert.Equal(2, updated.UltimaModificacionPor);
            Assert.True(updated.UltimaActualizacion > fechaAntes);
        }
    }
}
