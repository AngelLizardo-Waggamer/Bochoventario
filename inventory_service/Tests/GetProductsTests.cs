using System;
using System.Collections.Generic;
using System.Linq;
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
    public class GetProductsTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly InventoryController _controller;

        public GetProductsTests()
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
            var articulos = new List<Articulo>
            {
                new Articulo
                {
                    IdArticulo = 1,
                    Sku = "SKU-001",
                    Nombre = "Laptop Dell",
                    Descripcion = "Laptop para oficina, categoria: electronica",
                    PrecioCosto = 15000.00m
                },
                new Articulo
                {
                    IdArticulo = 2,
                    Sku = "SKU-002",
                    Nombre = "Mouse Logitech",
                    Descripcion = "Mouse inalambrico, categoria: accesorios",
                    PrecioCosto = 350.00m
                },
                new Articulo
                {
                    IdArticulo = 3,
                    Sku = "SKU-003",
                    Nombre = "Teclado Mecanico",
                    Descripcion = "Teclado RGB, categoria: accesorios",
                    PrecioCosto = 1200.00m
                },
                new Articulo
                {
                    IdArticulo = 4,
                    Sku = "SKU-004",
                    Nombre = "Monitor Samsung",
                    Descripcion = "Monitor 24 pulgadas, categoria: electronica",
                    PrecioCosto = 3500.00m
                }
            };

            _context.Articulos.AddRange(articulos);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetProducts_SinFiltros_RetornaTodosLosProductos()
        {
            // Act
            var result = await _controller.GetProducts(null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResult.Value);
            Assert.Equal(4, productos.Count());
        }

        [Fact]
        public async Task GetProducts_FiltroPorNombre_RetornaProductosCoincidentes()
        {
            // Act
            var result = await _controller.GetProducts("Laptop", null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResult.Value);
            Assert.Single(productos);
            Assert.Contains(productos, p => p.Nombre.Contains("Laptop"));
        }

        [Fact]
        public async Task GetProducts_FiltroPorSKU_RetornaProductosCoincidentes()
        {
            // Act
            var result = await _controller.GetProducts("SKU-002", null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResult.Value);
            Assert.Single(productos);
            Assert.Contains(productos, p => p.Sku == "SKU-002");
        }

        [Fact]
        public async Task GetProducts_FiltroPorDescripcion_RetornaProductosCoincidentes()
        {
            // Act
            var result = await _controller.GetProducts("inalambrico", null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResult.Value);
            Assert.Single(productos);
            Assert.Contains(productos, p => p.Descripcion != null && p.Descripcion.Contains("inalambrico"));
        }

        [Fact]
        public async Task GetProducts_FiltroPorCategoria_RetornaProductosCoincidentes()
        {
            // Act
            var result = await _controller.GetProducts(null, "electronica");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResult.Value);
            Assert.Equal(2, productos.Count());
            Assert.All(productos, p => Assert.Contains("electronica", p.Descripcion));
        }

        [Fact]
        public async Task GetProducts_FiltrosConCategoriaAccesorios_RetornaProductosCoincidentes()
        {
            // Act
            var result = await _controller.GetProducts(null, "accesorios");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResult.Value);
            Assert.Equal(2, productos.Count());
            Assert.All(productos, p => Assert.Contains("accesorios", p.Descripcion));
        }

        [Fact]
        public async Task GetProducts_SinResultados_RetornaListaVacia()
        {
            // Act
            var result = await _controller.GetProducts("ProductoInexistente", null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResult.Value);
            Assert.Empty(productos);
        }

        [Fact]
        public async Task GetProducts_FiltrosCombinados_RetornaProductosCoincidentes()
        {
            // Act
            var result = await _controller.GetProducts("Mouse", "accesorios");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResult.Value);
            Assert.Single(productos);
            Assert.Contains(productos, p => p.Nombre.Contains("Mouse") && p.Descripcion!.Contains("accesorios"));
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
