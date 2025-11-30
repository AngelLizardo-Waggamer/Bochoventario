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
    /// Tests unitarios para la eliminación de registros de inventario (stock)
    /// </summary>
    public class DeleteInventoryTests
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
        public void DeleteInventory_ComoAdministrador_EliminaCorrectamente()
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

            // Act
            var result = controller.DeleteInventory(1).Result;

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(context.Inventarios.Find(1));
        }

        [Fact]
        public void DeleteInventory_ComoGestor_EliminaCorrectamente()
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
            SetupUserClaims(controller, 2, 2, "gestor");

            // Act
            var result = controller.DeleteInventory(1).Result;

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(context.Inventarios.Find(1));
        }

        [Fact]
        public void DeleteInventory_ComoLector_RetornaUnauthorized()
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

            // Act
            var result = controller.DeleteInventory(1).Result;

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.NotNull(context.Inventarios.Find(1)); // Sigue existiendo
        }

        [Fact]
        public void DeleteInventory_SinAutenticacion_RetornaUnauthorized()
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
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = controller.DeleteInventory(1).Result;

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.NotNull(context.Inventarios.Find(1));
        }

        [Fact]
        public void DeleteInventory_InventarioNoExiste_RetornaNotFound()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            // Act
            var result = controller.DeleteInventory(999).Result;

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            dynamic mensaje = notFoundResult.Value!;
            Assert.Contains("no encontrado", mensaje.message.ToString());
        }

        [Fact]
        public void DeleteInventory_NoEliminaArticuloAsociado()
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

            // Act
            var result = controller.DeleteInventory(1).Result;

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(context.Inventarios.Find(1));
            Assert.NotNull(context.Articulos.Find(1)); // Artículo sigue existiendo
        }

        [Fact]
        public void DeleteInventory_EliminaUnoDeVariosInventariosDelMismoArticulo()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            
            var inventario1 = new Inventario 
            { 
                IdInventario = 1, 
                IdArticulo = 1, 
                Cantidad = 50, 
                Ubicacion = "Almacen A", 
                UltimaModificacionPor = 1 
            };
            var inventario2 = new Inventario 
            { 
                IdInventario = 2, 
                IdArticulo = 1, 
                Cantidad = 30, 
                Ubicacion = "Almacen B", 
                UltimaModificacionPor = 1 
            };
            context.Inventarios.AddRange(inventario1, inventario2);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            // Act
            var result = controller.DeleteInventory(1).Result;

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(context.Inventarios.Find(1));
            Assert.NotNull(context.Inventarios.Find(2)); // El otro inventario sigue existiendo
        }

        [Fact]
        public void DeleteInventory_IDNegativo_RetornaNotFound()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            // Act
            var result = controller.DeleteInventory(-1).Result;

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public void DeleteInventory_IDCero_RetornaNotFound()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            // Act
            var result = controller.DeleteInventory(0).Result;

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public void DeleteInventory_EliminarDosveces_SegundaVezRetornaNotFound()
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

            // Act
            var result1 = controller.DeleteInventory(1).Result;
            var result2 = controller.DeleteInventory(1).Result;

            // Assert
            Assert.IsType<NoContentResult>(result1);
            Assert.IsType<NotFoundObjectResult>(result2);
        }

        [Fact]
        public void DeleteInventory_NoAfectaOtrosInventarios()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo1 = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            var articulo2 = new Articulo { IdArticulo = 2, Sku = "SKU002", Nombre = "Producto 2", PrecioCosto = 200m };
            context.Articulos.AddRange(articulo1, articulo2);
            
            var inventario1 = new Inventario 
            { 
                IdInventario = 1, 
                IdArticulo = 1, 
                Cantidad = 50, 
                Ubicacion = "Almacen A", 
                UltimaModificacionPor = 1 
            };
            var inventario2 = new Inventario 
            { 
                IdInventario = 2, 
                IdArticulo = 2, 
                Cantidad = 75, 
                Ubicacion = "Almacen A", 
                UltimaModificacionPor = 1 
            };
            var inventario3 = new Inventario 
            { 
                IdInventario = 3, 
                IdArticulo = 1, 
                Cantidad = 30, 
                Ubicacion = "Almacen B", 
                UltimaModificacionPor = 1 
            };
            context.Inventarios.AddRange(inventario1, inventario2, inventario3);
            context.SaveChanges();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            // Act
            var result = controller.DeleteInventory(2).Result; // Elimina solo el inventario 2

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.NotNull(context.Inventarios.Find(1));
            Assert.Null(context.Inventarios.Find(2));
            Assert.NotNull(context.Inventarios.Find(3));
        }

        [Fact]
        public void DeleteInventory_ReduceConteoDeInventarios()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            
            var inventario1 = new Inventario 
            { 
                IdInventario = 1, 
                IdArticulo = 1, 
                Cantidad = 50, 
                Ubicacion = "Almacen A", 
                UltimaModificacionPor = 1 
            };
            var inventario2 = new Inventario 
            { 
                IdInventario = 2, 
                IdArticulo = 1, 
                Cantidad = 30, 
                Ubicacion = "Almacen B", 
                UltimaModificacionPor = 1 
            };
            context.Inventarios.AddRange(inventario1, inventario2);
            context.SaveChanges();

            var countInicial = context.Inventarios.Count();

            var controller = new InventoryController(context);
            SetupUserClaims(controller, 1, 1, "admin");

            // Act
            var result = controller.DeleteInventory(1).Result;

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(countInicial - 1, context.Inventarios.Count());
        }
    }
}
