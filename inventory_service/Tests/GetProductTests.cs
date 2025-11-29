using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using inventory_service.Controllers;
using inventory_service.Data;
using inventory_service.Models;

namespace inventory_service.Tests
{
    public class GetProductTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly InventoryController _controller;

        public GetProductTests()
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
            var rol = new Rol { IdRol = 1, NombreRol = "Administrador" };
            _context.Roles.Add(rol);

            var usuario = new Usuario
            {
                IdUsuario = 1,
                IdRol = 1,
                NombreUsuario = "admin",
                PasswordHash = "hash",
                NombreCompleto = "Administrador"
            };
            _context.Usuarios.Add(usuario);

            var articulo = new Articulo
            {
                IdArticulo = 1,
                Sku = "SKU-001",
                Nombre = "Laptop Dell",
                Descripcion = "Laptop para oficina",
                PrecioCosto = 15000.00m
            };
            _context.Articulos.Add(articulo);

            var inventarios = new List<Inventario>
            {
                new Inventario
                {
                    IdInventario = 1,
                    IdArticulo = 1,
                    Cantidad = 10,
                    Ubicacion = "Almacen A",
                    UltimaModificacionPor = 1,
                    UltimaActualizacion = DateTime.Now
                },
                new Inventario
                {
                    IdInventario = 2,
                    IdArticulo = 1,
                    Cantidad = 5,
                    Ubicacion = "Almacen B",
                    UltimaModificacionPor = 1,
                    UltimaActualizacion = DateTime.Now
                }
            };
            _context.Inventarios.AddRange(inventarios);

            _context.SaveChanges();
        }

        [Fact]
        public async Task GetProduct_ProductoExistente_RetornaProductoConInventarios()
        {
            // Act
            var result = await _controller.GetProduct(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var producto = Assert.IsType<Articulo>(okResult.Value);
            Assert.Equal(1, producto.IdArticulo);
            Assert.Equal("SKU-001", producto.Sku);
            Assert.Equal("Laptop Dell", producto.Nombre);
            Assert.NotNull(producto.Inventarios);
            Assert.Equal(2, producto.Inventarios.Count);
        }

        [Fact]
        public async Task GetProduct_ProductoNoEncontrado_Retorna404()
        {
            // Act
            var result = await _controller.GetProduct(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            dynamic mensaje = notFoundResult.Value!;
            Assert.Equal("Producto con ID 999 no encontrado", mensaje.message.ToString());
        }

        [Fact]
        public async Task GetProduct_ProductoSinInventarios_RetornaProductoConListaVacia()
        {
            // Arrange - Crear producto sin inventarios
            var articuloSinInventarios = new Articulo
            {
                IdArticulo = 2,
                Sku = "SKU-002",
                Nombre = "Mouse",
                Descripcion = "Mouse inalambrico",
                PrecioCosto = 350.00m
            };
            _context.Articulos.Add(articuloSinInventarios);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetProduct(2);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var producto = Assert.IsType<Articulo>(okResult.Value);
            Assert.Equal(2, producto.IdArticulo);
            Assert.NotNull(producto.Inventarios);
            Assert.Empty(producto.Inventarios);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
