using System.Collections.Generic;
using System.Linq;
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
    /// Tests unitarios para los endpoints de consulta de inventario (stock)
    /// </summary>
    public class GetInventoryTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);
            
            // Seed roles
            context.Roles.AddRange(
                new Rol { IdRol = 1, NombreRol = "Administrador" },
                new Rol { IdRol = 2, NombreRol = "Gestor" },
                new Rol { IdRol = 3, NombreRol = "Lector" }
            );
            
            // Seed usuarios
            context.Usuarios.AddRange(
                new Usuario { IdUsuario = 1, IdRol = 1, NombreUsuario = "admin", PasswordHash = "hash", NombreCompleto = "Admin User" },
                new Usuario { IdUsuario = 2, IdRol = 2, NombreUsuario = "gestor", PasswordHash = "hash", NombreCompleto = "Gestor User" },
                new Usuario { IdUsuario = 3, IdRol = 3, NombreUsuario = "lector", PasswordHash = "hash", NombreCompleto = "Lector User" }
            );
            
            context.SaveChanges();
            return context;
        }

        [Fact]
        public void GetInventoryByProduct_ProductoConInventario_RetornaInventarios()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            
            var inventarios = new List<Inventario>
            {
                new Inventario { IdInventario = 1, IdArticulo = 1, Cantidad = 50, Ubicacion = "Almacen A", UltimaModificacionPor = 1 },
                new Inventario { IdInventario = 2, IdArticulo = 1, Cantidad = 30, Ubicacion = "Almacen B", UltimaModificacionPor = 1 }
            };
            context.Inventarios.AddRange(inventarios);
            context.SaveChanges();

            var controller = new InventoryController(context);

            // Act
            var result = controller.GetInventoryByProduct(1).Result;

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedInventarios = Assert.IsAssignableFrom<IEnumerable<Inventario>>(okResult.Value);
            Assert.Equal(2, returnedInventarios.Count());
        }

        [Fact]
        public void GetInventoryByProduct_ProductoSinInventario_RetornaListaVacia()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulo = new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m };
            context.Articulos.Add(articulo);
            context.SaveChanges();

            var controller = new InventoryController(context);

            // Act
            var result = controller.GetInventoryByProduct(1).Result;

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedInventarios = Assert.IsAssignableFrom<IEnumerable<Inventario>>(okResult.Value);
            Assert.Empty(returnedInventarios);
        }

        [Fact]
        public void GetInventoryByProduct_ProductoNoExiste_RetornaNotFound()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new InventoryController(context);

            // Act
            var result = controller.GetInventoryByProduct(999).Result;

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            dynamic mensaje = notFoundResult.Value!;
            Assert.Contains("no encontrado", mensaje.message.ToString());
        }

        [Fact]
        public void GetInventoryByLocation_UbicacionConInventarios_RetornaInventarios()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulos = new List<Articulo>
            {
                new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m },
                new Articulo { IdArticulo = 2, Sku = "SKU002", Nombre = "Producto 2", PrecioCosto = 200m }
            };
            context.Articulos.AddRange(articulos);
            
            var inventarios = new List<Inventario>
            {
                new Inventario { IdInventario = 1, IdArticulo = 1, Cantidad = 50, Ubicacion = "Almacen A", UltimaModificacionPor = 1 },
                new Inventario { IdInventario = 2, IdArticulo = 2, Cantidad = 30, Ubicacion = "Almacen A", UltimaModificacionPor = 1 },
                new Inventario { IdInventario = 3, IdArticulo = 1, Cantidad = 20, Ubicacion = "Almacen B", UltimaModificacionPor = 1 }
            };
            context.Inventarios.AddRange(inventarios);
            context.SaveChanges();

            var controller = new InventoryController(context);

            // Act
            var result = controller.GetInventoryByLocation("Almacen A").Result;

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedInventarios = Assert.IsAssignableFrom<IEnumerable<Inventario>>(okResult.Value).ToList();
            Assert.Equal(2, returnedInventarios.Count);
            Assert.All(returnedInventarios, i => Assert.Equal("Almacen A", i.Ubicacion));
        }

        [Fact]
        public void GetInventoryByLocation_UbicacionSinInventarios_RetornaListaVacia()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new InventoryController(context);

            // Act
            var result = controller.GetInventoryByLocation("Almacen Vacio").Result;

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedInventarios = Assert.IsAssignableFrom<IEnumerable<Inventario>>(okResult.Value);
            Assert.Empty(returnedInventarios);
        }

        [Fact]
        public void GetInventoryByLocation_CaracteresEspeciales_ManejaCorrectamente()
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
                Ubicacion = "Almacén #1 (Principal)", 
                UltimaModificacionPor = 1 
            };
            context.Inventarios.Add(inventario);
            context.SaveChanges();

            var controller = new InventoryController(context);

            // Act
            var result = controller.GetInventoryByLocation("Almacén #1 (Principal)").Result;

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedInventarios = Assert.IsAssignableFrom<IEnumerable<Inventario>>(okResult.Value).ToList();
            Assert.Single(returnedInventarios);
        }

        [Fact]
        public void GetAllInventory_VariosInventarios_RetornaTodos()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var articulos = new List<Articulo>
            {
                new Articulo { IdArticulo = 1, Sku = "SKU001", Nombre = "Producto 1", PrecioCosto = 100m },
                new Articulo { IdArticulo = 2, Sku = "SKU002", Nombre = "Producto 2", PrecioCosto = 200m }
            };
            context.Articulos.AddRange(articulos);
            
            var inventarios = new List<Inventario>
            {
                new Inventario { IdInventario = 1, IdArticulo = 1, Cantidad = 50, Ubicacion = "Almacen A", UltimaModificacionPor = 1 },
                new Inventario { IdInventario = 2, IdArticulo = 2, Cantidad = 30, Ubicacion = "Almacen B", UltimaModificacionPor = 1 },
                new Inventario { IdInventario = 3, IdArticulo = 1, Cantidad = 20, Ubicacion = "Almacen C", UltimaModificacionPor = 1 }
            };
            context.Inventarios.AddRange(inventarios);
            context.SaveChanges();

            var controller = new InventoryController(context);

            // Act
            var result = controller.GetAllInventory().Result;

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedInventarios = Assert.IsAssignableFrom<IEnumerable<Inventario>>(okResult.Value);
            Assert.Equal(3, returnedInventarios.Count());
        }

        [Fact]
        public void GetAllInventory_SinInventarios_RetornaListaVacia()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new InventoryController(context);

            // Act
            var result = controller.GetAllInventory().Result;

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedInventarios = Assert.IsAssignableFrom<IEnumerable<Inventario>>(okResult.Value);
            Assert.Empty(returnedInventarios);
        }

        [Fact]
        public void GetAllInventory_IncluyeRelaciones_ArticuloYUsuario()
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

            // Act
            var result = controller.GetAllInventory().Result;

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedInventarios = Assert.IsAssignableFrom<IEnumerable<Inventario>>(okResult.Value).ToList();
            Assert.Single(returnedInventarios);
            Assert.NotNull(returnedInventarios[0].Articulo);
            Assert.Equal("Producto 1", returnedInventarios[0].Articulo.Nombre);
        }
    }
}
