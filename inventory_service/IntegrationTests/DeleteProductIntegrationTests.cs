using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using inventory_service.Models;

namespace inventory_service.IntegrationTests
{
    /// <summary>
    /// Tests de integración para la eliminación de productos.
    /// Verifica cascade deletes y validación de permisos con base de datos real.
    /// </summary>
    public class DeleteProductIntegrationTests : IntegrationTestBase
    {
        [Fact]
        public async Task DeleteProduct_ComoAdministrador_EliminaDeLaBaseDeDatos()
        {
            // Arrange
            var articulo = await CreateTestArticulo("DEL-SKU-001", "Producto a Eliminar", 1000.00m);
            SetupUserClaims(1, 1, "admin");

            // Act
            var result = await _controller.DeleteProduct(articulo.IdArticulo);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            // Verificar que fue eliminado de la base de datos
            var productoEnBD = await _context.Articulos.FindAsync(articulo.IdArticulo);
            Assert.Null(productoEnBD);
        }

        [Fact]
        public async Task DeleteProduct_ComoGestor_EliminaDeLaBaseDeDatos()
        {
            // Arrange
            var articulo = await CreateTestArticulo("DEL-SKU-002", "Producto Gestor", 2000.00m);
            SetupUserClaims(2, 2, "gestor");

            // Act
            var result = await _controller.DeleteProduct(articulo.IdArticulo);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            var productoEnBD = await _context.Articulos.FindAsync(articulo.IdArticulo);
            Assert.Null(productoEnBD);
        }

        [Fact]
        public async Task DeleteProduct_ComoLector_NoEliminaDeLaBaseDeDatos()
        {
            // Arrange
            var articulo = await CreateTestArticulo("DEL-SKU-003", "Producto Lector", 1500.00m);
            SetupUserClaims(3, 3, "lector");

            // Act
            var result = await _controller.DeleteProduct(articulo.IdArticulo);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            
            // Verificar que NO fue eliminado
            var productoEnBD = await _context.Articulos.FindAsync(articulo.IdArticulo);
            Assert.NotNull(productoEnBD);
            Assert.Equal("DEL-SKU-003", productoEnBD.Sku);
        }

        [Fact]
        public async Task DeleteProduct_SinAutenticacion_NoEliminaDeLaBaseDeDatos()
        {
            // Arrange
            var articulo = await CreateTestArticulo("DEL-SKU-004", "Producto Sin Auth", 1000.00m);
            ClearUserClaims();

            // Act
            var result = await _controller.DeleteProduct(articulo.IdArticulo);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
            
            // Verificar que sigue existiendo
            var productoEnBD = await _context.Articulos.FindAsync(articulo.IdArticulo);
            Assert.NotNull(productoEnBD);
        }

        [Fact]
        public async Task DeleteProduct_ProductoNoExistente_RetornaNotFound()
        {
            // Arrange
            SetupUserClaims(1, 1, "admin");

            // Act
            var result = await _controller.DeleteProduct(99999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            dynamic mensaje = notFoundResult.Value!;
            Assert.Contains("no encontrado", mensaje.message.ToString());
        }

        [Fact]
        public async Task DeleteProduct_ConInventarioAsociado_EliminaAmbosPorCascade()
        {
            // Arrange
            var articulo = await CreateTestArticulo("DEL-SKU-CASCADE", "Producto con Inventario", 3000.00m);
            
            // Crear inventario asociado
            var inventario = new Inventario
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 100,
                Ubicacion = "Almacen A",
                UltimaModificacionPor = 1
            };
            _context.Inventarios.Add(inventario);
            await _context.SaveChangesAsync();
            
            // Verificar que el inventario existe
            var inventarioAntes = await _context.Inventarios
                .FirstOrDefaultAsync(i => i.IdArticulo == articulo.IdArticulo);
            Assert.NotNull(inventarioAntes);
            
            SetupUserClaims(1, 1, "admin");

            // Act
            var result = await _controller.DeleteProduct(articulo.IdArticulo);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            // Verificar que el artículo fue eliminado
            var articuloEnBD = await _context.Articulos.FindAsync(articulo.IdArticulo);
            Assert.Null(articuloEnBD);
            
            // Verificar que el inventario también fue eliminado (CASCADE)
            var inventarioEnBD = await _context.Inventarios
                .FirstOrDefaultAsync(i => i.IdArticulo == articulo.IdArticulo);
            Assert.Null(inventarioEnBD);
        }

        [Fact]
        public async Task DeleteProduct_ConMultiplesInventarios_EliminaTodos()
        {
            // Arrange
            var articulo = await CreateTestArticulo("DEL-SKU-MULTI", "Producto Multi-Inventario", 2500.00m);
            
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
            
            SetupUserClaims(1, 1, "admin");

            // Act
            var result = await _controller.DeleteProduct(articulo.IdArticulo);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            // Verificar que todos los inventarios fueron eliminados
            var inventariosEnBD = await _context.Inventarios
                .Where(i => i.IdArticulo == articulo.IdArticulo)
                .ToListAsync();
            Assert.Empty(inventariosEnBD);
        }

        [Fact]
        public async Task DeleteProduct_VerificaNoAfectaOtrosProductos()
        {
            // Arrange
            var articulo1 = await CreateTestArticulo("DEL-SKU-KEEP-1", "Producto a Mantener 1", 1000.00m);
            var articulo2 = await CreateTestArticulo("DEL-SKU-DELETE", "Producto a Eliminar", 2000.00m);
            var articulo3 = await CreateTestArticulo("DEL-SKU-KEEP-2", "Producto a Mantener 2", 3000.00m);
            
            SetupUserClaims(1, 1, "admin");

            // Act - Eliminar solo el producto del medio
            var result = await _controller.DeleteProduct(articulo2.IdArticulo);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            // Verificar que articulo2 fue eliminado
            var articulo2EnBD = await _context.Articulos.FindAsync(articulo2.IdArticulo);
            Assert.Null(articulo2EnBD);
            
            // Verificar que los otros productos siguen existiendo
            var articulo1EnBD = await _context.Articulos.FindAsync(articulo1.IdArticulo);
            var articulo3EnBD = await _context.Articulos.FindAsync(articulo3.IdArticulo);
            Assert.NotNull(articulo1EnBD);
            Assert.NotNull(articulo3EnBD);
            Assert.Equal("DEL-SKU-KEEP-1", articulo1EnBD.Sku);
            Assert.Equal("DEL-SKU-KEEP-2", articulo3EnBD.Sku);
        }

        [Fact]
        public async Task DeleteProduct_IDNegativo_RetornaNotFound()
        {
            // Arrange
            SetupUserClaims(1, 1, "admin");

            // Act
            var result = await _controller.DeleteProduct(-1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteProduct_IDCero_RetornaNotFound()
        {
            // Arrange
            SetupUserClaims(1, 1, "admin");

            // Act
            var result = await _controller.DeleteProduct(0);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteProduct_DespuesDeEliminar_NoEsAccesible()
        {
            // Arrange
            var articulo = await CreateTestArticulo("DEL-SKU-VERIFY", "Producto a Verificar", 1500.00m);
            SetupUserClaims(1, 1, "admin");
            
            // Verificar que existe antes de eliminar
            var articuloAntes = await _context.Articulos.FindAsync(articulo.IdArticulo);
            Assert.NotNull(articuloAntes);

            // Act - Eliminar
            await _controller.DeleteProduct(articulo.IdArticulo);

            // Assert - Intentar obtenerlo
            var resultGet = await _controller.GetProduct(articulo.IdArticulo);
            Assert.IsType<NotFoundObjectResult>(resultGet.Result);
        }

        [Fact]
        public async Task DeleteProduct_MultiplesConcurrentes_TodosSeEliminanCorrectamente()
        {
            // Arrange
            var articulo1 = await CreateTestArticulo("DEL-CONC-1", "Concurrente 1", 1000.00m);
            var articulo2 = await CreateTestArticulo("DEL-CONC-2", "Concurrente 2", 2000.00m);
            var articulo3 = await CreateTestArticulo("DEL-CONC-3", "Concurrente 3", 3000.00m);
            
            SetupUserClaims(1, 1, "admin");

            // Act - Eliminar todos
            await _controller.DeleteProduct(articulo1.IdArticulo);
            await _controller.DeleteProduct(articulo2.IdArticulo);
            await _controller.DeleteProduct(articulo3.IdArticulo);

            // Assert - Verificar que todos fueron eliminados
            Assert.Null(await _context.Articulos.FindAsync(articulo1.IdArticulo));
            Assert.Null(await _context.Articulos.FindAsync(articulo2.IdArticulo));
            Assert.Null(await _context.Articulos.FindAsync(articulo3.IdArticulo));
        }

        [Fact]
        public async Task DeleteProduct_IntentarEliminarDosveces_SegundaVezRetornaNotFound()
        {
            // Arrange
            var articulo = await CreateTestArticulo("DEL-SKU-TWICE", "Eliminar Dos Veces", 1000.00m);
            SetupUserClaims(1, 1, "admin");

            // Act - Primera eliminación
            var result1 = await _controller.DeleteProduct(articulo.IdArticulo);
            Assert.IsType<NoContentResult>(result1);

            // Act - Segunda eliminación (ya no existe)
            var result2 = await _controller.DeleteProduct(articulo.IdArticulo);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result2);
        }

        [Fact]
        public async Task DeleteProduct_ConInventarioYReferenciaAUsuario_EliminaCorrectamente()
        {
            // Arrange
            var articulo = await CreateTestArticulo("DEL-SKU-USER-REF", "Producto con Usuario", 2000.00m);
            
            // Crear inventario con referencia a usuario
            var inventario = new Inventario
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 75,
                Ubicacion = "Almacen Principal",
                UltimaModificacionPor = 2 // Usuario gestor
            };
            _context.Inventarios.Add(inventario);
            await _context.SaveChangesAsync();
            
            SetupUserClaims(1, 1, "admin");

            // Act
            var result = await _controller.DeleteProduct(articulo.IdArticulo);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            // Verificar que el artículo fue eliminado
            Assert.Null(await _context.Articulos.FindAsync(articulo.IdArticulo));
            
            // Verificar que el inventario también fue eliminado
            var inventarioEnBD = await _context.Inventarios
                .FirstOrDefaultAsync(i => i.IdArticulo == articulo.IdArticulo);
            Assert.Null(inventarioEnBD);
            
            // Verificar que el usuario sigue existiendo (no se afecta)
            var usuario = await _context.Usuarios.FindAsync(2);
            Assert.NotNull(usuario);
        }

        [Fact]
        public async Task DeleteProduct_ReduceElConteoTotal_Correctamente()
        {
            // Arrange
            var countInicial = await _context.Articulos.CountAsync();
            var articulo = await CreateTestArticulo("DEL-SKU-COUNT", "Producto para Contar", 1000.00m);
            
            var countDespuesCrear = await _context.Articulos.CountAsync();
            Assert.Equal(countInicial + 1, countDespuesCrear);
            
            SetupUserClaims(1, 1, "admin");

            // Act
            var result = await _controller.DeleteProduct(articulo.IdArticulo);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            var countFinal = await _context.Articulos.CountAsync();
            Assert.Equal(countInicial, countFinal);
        }
    }
}
