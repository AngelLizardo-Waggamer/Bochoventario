using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using inventory_service.Controllers;
using inventory_service.Models;

namespace inventory_service.IntegrationTests
{
    /// <summary>
    /// Tests de integración para los endpoints de consulta de inventario (stock).
    /// Verifica el funcionamiento real contra una base de datos MySQL en Docker.
    /// </summary>
    public class GetInventoryIntegrationTests : IntegrationTestBase
    {
        public GetInventoryIntegrationTests(DatabaseFixture fixture) : base(fixture) { }

        protected override async Task SeedDatabase()
        {
            await base.SeedDatabase();

            // Seed artículos
            var articulos = new List<Articulo>
            {
                new Articulo { IdArticulo = 1, Sku = "INV-SKU-001", Nombre = "Laptop Dell", Descripcion = "Laptop profesional", PrecioCosto = 25000m },
                new Articulo { IdArticulo = 2, Sku = "INV-SKU-002", Nombre = "Mouse Logitech", Descripcion = "Mouse inalámbrico", PrecioCosto = 500m },
                new Articulo { IdArticulo = 3, Sku = "INV-SKU-003", Nombre = "Teclado Mecánico", Descripcion = "Teclado gaming", PrecioCosto = 2000m }
            };
            _context.Articulos.AddRange(articulos);

            // Seed inventarios
            var inventarios = new List<Inventario>
            {
                new Inventario { IdInventario = 1, IdArticulo = 1, Cantidad = 50, Ubicacion = "Almacen A", UltimaModificacionPor = 1 },
                new Inventario { IdInventario = 2, IdArticulo = 1, Cantidad = 30, Ubicacion = "Almacen B", UltimaModificacionPor = 1 },
                new Inventario { IdInventario = 3, IdArticulo = 2, Cantidad = 100, Ubicacion = "Almacen A", UltimaModificacionPor = 2 },
                new Inventario { IdInventario = 4, IdArticulo = 3, Cantidad = 75, Ubicacion = "Almacen C", UltimaModificacionPor = 1 }
            };
            _context.Inventarios.AddRange(inventarios);
            
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetInventoryByProduct_ArticuloConInventarios_RetornaCorrectamente()
        {
            // Act
            var result = await _controller.GetInventoryByProduct(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var inventarios = Assert.IsAssignableFrom<IEnumerable<Inventario>>(okResult.Value).ToList();
            
            Assert.Equal(2, inventarios.Count);
            Assert.All(inventarios, i => Assert.Equal(1, i.IdArticulo));
            
            // Verificar en base de datos
            var inventariosDB = await _context.Inventarios
                .Where(i => i.IdArticulo == 1)
                .ToListAsync();
            Assert.Equal(inventarios.Count, inventariosDB.Count);
        }

        [Fact]
        public async Task GetInventoryByProduct_ArticuloSinInventarios_RetornaListaVacia()
        {
            // Arrange - Crear artículo sin inventario
            var articulo = await CreateTestArticulo("NO-INV-001", "Sin Inventario", 1000m);

            // Act
            var result = await _controller.GetInventoryByProduct(articulo.IdArticulo);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var inventarios = Assert.IsAssignableFrom<IEnumerable<Inventario>>(okResult.Value);
            Assert.Empty(inventarios);
        }

        [Fact]
        public async Task GetInventoryByProduct_ArticuloNoExiste_RetornaNotFound()
        {
            // Act
            var result = await _controller.GetInventoryByProduct(99999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            dynamic mensaje = notFoundResult.Value!;
            Assert.Contains("no encontrado", mensaje.message.ToString());
        }

        [Fact]
        public async Task GetInventoryByProduct_IncluyeRelaciones_ArticuloYUsuario()
        {
            // Act
            var result = await _controller.GetInventoryByProduct(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var inventarios = Assert.IsAssignableFrom<IEnumerable<Inventario>>(okResult.Value).ToList();
            
            Assert.NotEmpty(inventarios);
            Assert.All(inventarios, i => Assert.NotNull(i.UsuarioModificador));
            Assert.Contains(inventarios, i => i.UsuarioModificador!.NombreUsuario == "admin");
        }

        [Fact]
        public async Task GetInventoryByLocation_UbicacionConInventarios_RetornaCorrectamente()
        {
            // Act
            var result = await _controller.GetInventoryByLocation("Almacen A");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var inventarios = Assert.IsAssignableFrom<IEnumerable<Inventario>>(okResult.Value).ToList();
            
            Assert.Equal(2, inventarios.Count);
            Assert.All(inventarios, i => Assert.Equal("Almacen A", i.Ubicacion));
            
            // Verificar artículos diferentes en misma ubicación
            var articuloIds = inventarios.Select(i => i.IdArticulo).Distinct().ToList();
            Assert.Equal(2, articuloIds.Count);
        }

        [Fact]
        public async Task GetInventoryByLocation_UbicacionSinInventarios_RetornaListaVacia()
        {
            // Act
            var result = await _controller.GetInventoryByLocation("Almacen Inexistente");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var inventarios = Assert.IsAssignableFrom<IEnumerable<Inventario>>(okResult.Value);
            Assert.Empty(inventarios);
        }

        [Fact]
        public async Task GetInventoryByLocation_IncluyeArticulos_ConInformacionCompleta()
        {
            // Act
            var result = await _controller.GetInventoryByLocation("Almacen A");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var inventarios = Assert.IsAssignableFrom<IEnumerable<Inventario>>(okResult.Value).ToList();
            
            Assert.NotEmpty(inventarios);
            Assert.All(inventarios, i => 
            {
                Assert.NotNull(i.Articulo);
                Assert.NotEmpty(i.Articulo.Nombre);
                Assert.NotEmpty(i.Articulo.Sku);
            });
        }

        [Fact]
        public async Task GetInventoryByLocation_CaracteresEspeciales_ManejaCorrectamente()
        {
            // Arrange
            var articulo = await CreateTestArticulo("SPEC-001", "Producto Especial", 1000m);
            var inventario = new Inventario
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 25,
                Ubicacion = "Almacén #1 - Sección A/B (Principal)",
                UltimaModificacionPor = 1
            };
            _context.Inventarios.Add(inventario);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetInventoryByLocation("Almacén #1 - Sección A/B (Principal)");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var inventarios = Assert.IsAssignableFrom<IEnumerable<Inventario>>(okResult.Value).ToList();
            Assert.Single(inventarios);
            Assert.Equal(25, inventarios[0].Cantidad);
        }

        [Fact]
        public async Task GetAllInventory_RetornaTodosLosInventarios()
        {
            // Act
            var result = await _controller.GetAllInventory();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var inventarios = Assert.IsAssignableFrom<IEnumerable<Inventario>>(okResult.Value).ToList();
            
            Assert.True(inventarios.Count >= 4); // Al menos los 4 seeded
            
            // Verificar contra base de datos
            var inventariosDB = await _context.Inventarios.CountAsync();
            Assert.Equal(inventariosDB, inventarios.Count);
        }

        [Fact]
        public async Task GetAllInventory_IncluyeTodasLasRelaciones()
        {
            // Act
            var result = await _controller.GetAllInventory();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var inventarios = Assert.IsAssignableFrom<IEnumerable<Inventario>>(okResult.Value).ToList();
            
            Assert.NotEmpty(inventarios);
            Assert.All(inventarios, i => 
            {
                Assert.NotNull(i.Articulo);
                Assert.NotNull(i.UsuarioModificador);
            });
        }

        [Fact]
        public async Task GetAllInventory_VerificaCantidadesTotales()
        {
            // Act
            var result = await _controller.GetAllInventory();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var inventarios = Assert.IsAssignableFrom<IEnumerable<Inventario>>(okResult.Value).ToList();
            
            var totalCantidad = inventarios.Sum(i => i.Cantidad);
            Assert.True(totalCantidad > 0);
            
            // Sumar cantidades por artículo
            var cantidadArticulo1 = inventarios.Where(i => i.IdArticulo == 1).Sum(i => i.Cantidad);
            Assert.Equal(80, cantidadArticulo1); // 50 + 30 de Almacen A y B
        }

        [Fact]
        public async Task GetAllInventory_DespuesDeAgregarNuevo_LoIncluye()
        {
            // Arrange
            var resultInicial = await _controller.GetAllInventory();
            var okResultInicial = Assert.IsType<OkObjectResult>(resultInicial.Result);
            var inventariosIniciales = Assert.IsAssignableFrom<IEnumerable<Inventario>>(okResultInicial.Value).ToList();
            var countInicial = inventariosIniciales.Count;

            // Agregar nuevo inventario
            var articulo = await CreateTestArticulo("NEW-001", "Nuevo Producto", 1500m);
            var nuevoInventario = new Inventario
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 50,
                Ubicacion = "Almacen D",
                UltimaModificacionPor = 1
            };
            _context.Inventarios.Add(nuevoInventario);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetAllInventory();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var inventarios = Assert.IsAssignableFrom<IEnumerable<Inventario>>(okResult.Value).ToList();
            Assert.Equal(countInicial + 1, inventarios.Count);
        }

        [Fact]
        public async Task GetInventoryByProduct_VariasUbicaciones_SumaCorrectamente()
        {
            // Act
            var result = await _controller.GetInventoryByProduct(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var inventarios = Assert.IsAssignableFrom<IEnumerable<Inventario>>(okResult.Value).ToList();
            
            var totalStock = inventarios.Sum(i => i.Cantidad);
            Assert.Equal(80, totalStock);
            
            // Verificar ubicaciones diferentes
            var ubicaciones = inventarios.Select(i => i.Ubicacion).Distinct().ToList();
            Assert.Equal(2, ubicaciones.Count);
        }

        [Fact]
        public async Task GetInventoryByLocation_MultiplesUbicaciones_RetornaCorrectamente()
        {
            // Act - Ejecutar consultas secuenciales (evita problemas de DbContext concurrente)
            var task1 = await _controller.GetInventoryByLocation("Almacen A");
            var task2 = await _controller.GetInventoryByLocation("Almacen B");
            var task3 = await _controller.GetInventoryByLocation("Almacen C");

            // Assert
            var result1 = Assert.IsType<OkObjectResult>(task1.Result);
            var result2 = Assert.IsType<OkObjectResult>(task2.Result);
            var result3 = Assert.IsType<OkObjectResult>(task3.Result);
            
            var inv1 = Assert.IsAssignableFrom<IEnumerable<Inventario>>(result1.Value).ToList();
            var inv2 = Assert.IsAssignableFrom<IEnumerable<Inventario>>(result2.Value).ToList();
            var inv3 = Assert.IsAssignableFrom<IEnumerable<Inventario>>(result3.Value).ToList();
            
            Assert.Equal(2, inv1.Count);
            Assert.Single(inv2);
            Assert.Single(inv3);
        }
    }
}
