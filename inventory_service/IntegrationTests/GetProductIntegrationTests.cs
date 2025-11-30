using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using inventory_service.Models;

namespace inventory_service.IntegrationTests
{
    /// <summary>
    /// Tests de integración para obtener un producto específico por ID.
    /// Verifica relaciones (includes) y casos de error con base de datos real.
    /// </summary>
    public class GetProductIntegrationTests : IntegrationTestBase
    {
        public GetProductIntegrationTests(DatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task GetProduct_ProductoExistente_RetornaProductoDeLaBaseDeDatos()
        {
            // Arrange
            var articulo = await CreateTestArticulo("GET-SKU-001", "Laptop Dell Precision", 32000.00m);

            // Act
            var result = await _controller.GetProduct(articulo.IdArticulo);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var producto = Assert.IsType<Articulo>(okResult.Value);
            
            Assert.Equal(articulo.IdArticulo, producto.IdArticulo);
            Assert.Equal("GET-SKU-001", producto.Sku);
            Assert.Equal("Laptop Dell Precision", producto.Nombre);
            Assert.Equal(32000.00m, producto.PrecioCosto);
            
            // Verificar que coincide con lo guardado en la BD
            var productoEnBD = await _context.Articulos
                .FirstOrDefaultAsync(a => a.IdArticulo == articulo.IdArticulo);
            Assert.NotNull(productoEnBD);
            Assert.Equal(producto.Sku, productoEnBD.Sku);
        }

        [Fact]
        public async Task GetProduct_ProductoNoExistente_RetornaNotFound()
        {
            // Act
            var result = await _controller.GetProduct(99999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            dynamic mensaje = notFoundResult.Value!;
            Assert.Contains("no encontrado", mensaje.message.ToString());
        }

        [Fact]
        public async Task GetProduct_ConInventarioAsociado_IncluyeRelaciones()
        {
            // Arrange
            var articulo = await CreateTestArticulo("GET-SKU-002", "Monitor Samsung 27\"", 4500.00m);
            
            // Crear inventario asociado
            var inventario = new Inventario
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 50,
                Ubicacion = "Almacen B",
                UltimaModificacionPor = 1
            };
            _context.Inventarios.Add(inventario);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetProduct(articulo.IdArticulo);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var producto = Assert.IsType<Articulo>(okResult.Value);
            
            Assert.NotNull(producto.Inventarios);
            Assert.Single(producto.Inventarios);
            Assert.Equal(50, producto.Inventarios.First().Cantidad);
            Assert.Equal("Almacen B", producto.Inventarios.First().Ubicacion);
        }

        [Fact]
        public async Task GetProduct_ConMultiplesInventarios_RetornaTodos()
        {
            // Arrange
            var articulo = await CreateTestArticulo("GET-SKU-003", "Teclado Mecánico", 2200.00m);
            
            // Crear múltiples inventarios en diferentes ubicaciones
            var inventarios = new[]
            {
                new Inventario
                {
                    IdArticulo = articulo.IdArticulo,
                    Cantidad = 30,
                    Ubicacion = "Almacen A",
                    UltimaModificacionPor = 1
                },
                new Inventario
                {
                    IdArticulo = articulo.IdArticulo,
                    Cantidad = 45,
                    Ubicacion = "Almacen B",
                    UltimaModificacionPor = 1
                },
                new Inventario
                {
                    IdArticulo = articulo.IdArticulo,
                    Cantidad = 20,
                    Ubicacion = "Tienda Centro",
                    UltimaModificacionPor = 2
                }
            };
            _context.Inventarios.AddRange(inventarios);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetProduct(articulo.IdArticulo);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var producto = Assert.IsType<Articulo>(okResult.Value);
            
            Assert.NotNull(producto.Inventarios);
            Assert.Equal(3, producto.Inventarios.Count);
            Assert.Equal(95, producto.Inventarios.Sum(i => i.Cantidad)); // Total: 30+45+20
        }

        [Fact]
        public async Task GetProduct_SinInventario_RetornaProductoConListaVacia()
        {
            // Arrange
            var articulo = await CreateTestArticulo("GET-SKU-004", "Mouse Inalámbrico", 599.00m);

            // Act
            var result = await _controller.GetProduct(articulo.IdArticulo);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var producto = Assert.IsType<Articulo>(okResult.Value);
            
            Assert.NotNull(producto.Inventarios);
            Assert.Empty(producto.Inventarios);
        }

        [Fact]
        public async Task GetProduct_IDNegativo_RetornaNotFound()
        {
            // Act
            var result = await _controller.GetProduct(-1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetProduct_IDCero_RetornaNotFound()
        {
            // Act
            var result = await _controller.GetProduct(0);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetProduct_VerificaIntegridadDeDatos_TodosLosCampos()
        {
            // Arrange
            var articulo = new Articulo
            {
                Sku = "GET-SKU-FULL",
                Nombre = "Producto Completo con Áccéntos",
                Descripcion = "Descripción detallada con símbolos @#$%",
                PrecioCosto = 12345.67m
            };
            _context.Articulos.Add(articulo);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetProduct(articulo.IdArticulo);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var producto = Assert.IsType<Articulo>(okResult.Value);
            
            Assert.Equal("GET-SKU-FULL", producto.Sku);
            Assert.Equal("Producto Completo con Áccéntos", producto.Nombre);
            Assert.Equal("Descripción detallada con símbolos @#$%", producto.Descripcion);
            Assert.Equal(12345.67m, producto.PrecioCosto);
        }

        [Fact]
        public async Task GetProduct_DespuesDeActualizar_RetornaDatosActualizados()
        {
            // Arrange
            var articulo = await CreateTestArticulo("GET-SKU-UPD", "Nombre Original", 1000.00m);
            
            // Actualizar directamente en la base de datos
            articulo.Nombre = "Nombre Actualizado";
            articulo.PrecioCosto = 1500.00m;
            _context.Articulos.Update(articulo);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetProduct(articulo.IdArticulo);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var producto = Assert.IsType<Articulo>(okResult.Value);
            
            Assert.Equal("Nombre Actualizado", producto.Nombre);
            Assert.Equal(1500.00m, producto.PrecioCosto);
        }

        [Fact]
        public async Task GetProduct_ConInventarioYAuditoriaCompleta_VerificaRelaciones()
        {
            // Arrange
            var articulo = await CreateTestArticulo("GET-SKU-AUD", "Producto con Auditoría", 3000.00m);
            
            // Crear inventario con auditoría completa
            var inventario = new Inventario
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 100,
                Ubicacion = "Almacen Principal",
                UltimaModificacionPor = 2, // Usuario gestor
                UltimaActualizacion = System.DateTime.Now
            };
            _context.Inventarios.Add(inventario);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetProduct(articulo.IdArticulo);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var producto = Assert.IsType<Articulo>(okResult.Value);
            
            var inv = producto.Inventarios.First();
            Assert.Equal(100, inv.Cantidad);
            Assert.Equal("Almacen Principal", inv.Ubicacion);
            Assert.Equal(2, inv.UltimaModificacionPor);
            // UltimaActualizacion es DateTime (value type), siempre tiene valor - no necesita Assert.NotNull
        }

        [Fact]
        public async Task GetProduct_ProductoRecienCreado_EsAccesibleInmediatamente()
        {
            // Arrange - Crear y Act - Obtener en secuencia
            var articulo = await CreateTestArticulo("GET-SKU-NEW", "Producto Recién Creado", 999.00m);
            
            // Act
            var result = await _controller.GetProduct(articulo.IdArticulo);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var producto = Assert.IsType<Articulo>(okResult.Value);
            
            Assert.Equal(articulo.IdArticulo, producto.IdArticulo);
            Assert.Equal("GET-SKU-NEW", producto.Sku);
        }
    }
}
