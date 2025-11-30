using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using inventory_service.Controllers;
using inventory_service.Models;

namespace inventory_service.IntegrationTests
{
    /// <summary>
    /// Tests de integración para la actualización de productos.
    /// Verifica modificaciones reales en la base de datos y manejo de concurrencia.
    /// </summary>
    public class UpdateProductIntegrationTests : IntegrationTestBase
    {
        [Fact]
        public async Task UpdateProduct_ComoAdministrador_ActualizaEnBaseDeDatos()
        {
            // Arrange
            var articulo = await CreateTestArticulo("UPD-SKU-001", "Nombre Original", 1000.00m);
            SetupUserClaims(1, 1, "admin");
            
            var request = new UpdateProductRequest
            {
                Articulo = new Articulo
                {
                    IdArticulo = articulo.IdArticulo,
                    Sku = "UPD-SKU-001-MOD",
                    Nombre = "Nombre Actualizado",
                    Descripcion = "Nueva descripción",
                    PrecioCosto = 1500.00m
                }
            };

            // Act
            var result = await _controller.UpdateProduct(articulo.IdArticulo, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            // Verificar en base de datos
            var productoEnBD = await _context.Articulos.FindAsync(articulo.IdArticulo);
            Assert.NotNull(productoEnBD);
            Assert.Equal("UPD-SKU-001-MOD", productoEnBD.Sku);
            Assert.Equal("Nombre Actualizado", productoEnBD.Nombre);
            Assert.Equal("Nueva descripción", productoEnBD.Descripcion);
            Assert.Equal(1500.00m, productoEnBD.PrecioCosto);
        }

        [Fact]
        public async Task UpdateProduct_ComoGestor_ActualizaEnBaseDeDatos()
        {
            // Arrange
            var articulo = await CreateTestArticulo("UPD-SKU-002", "Producto Gestor", 2000.00m);
            SetupUserClaims(2, 2, "gestor");
            
            var request = new UpdateProductRequest
            {
                Articulo = new Articulo
                {
                    IdArticulo = articulo.IdArticulo,
                    Sku = "UPD-SKU-002",
                    Nombre = "Producto Modificado por Gestor",
                    Descripcion = articulo.Descripcion,
                    PrecioCosto = 2500.00m
                }
            };

            // Act
            var result = await _controller.UpdateProduct(articulo.IdArticulo, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            var productoEnBD = await _context.Articulos.FindAsync(articulo.IdArticulo);
            Assert.Equal("Producto Modificado por Gestor", productoEnBD!.Nombre);
            Assert.Equal(2500.00m, productoEnBD.PrecioCosto);
        }

        [Fact]
        public async Task UpdateProduct_ComoLector_NoActualizaEnBaseDeDatos()
        {
            // Arrange
            var articulo = await CreateTestArticulo("UPD-SKU-003", "Producto Original", 1500.00m);
            SetupUserClaims(3, 3, "lector");
            
            var request = new UpdateProductRequest
            {
                Articulo = new Articulo
                {
                    IdArticulo = articulo.IdArticulo,
                    Sku = "UPD-SKU-003-NOPE",
                    Nombre = "No Debería Cambiar",
                    Descripcion = "No Debería Cambiar",
                    PrecioCosto = 9999.00m
                }
            };

            // Act
            var result = await _controller.UpdateProduct(articulo.IdArticulo, request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            
            // Verificar que NO cambió en la base de datos
            var productoEnBD = await _context.Articulos.FindAsync(articulo.IdArticulo);
            Assert.Equal("UPD-SKU-003", productoEnBD!.Sku);
            Assert.Equal("Producto Original", productoEnBD.Nombre);
            Assert.Equal(1500.00m, productoEnBD.PrecioCosto);
        }

        [Fact]
        public async Task UpdateProduct_SinAutenticacion_NoActualizaEnBaseDeDatos()
        {
            // Arrange
            var articulo = await CreateTestArticulo("UPD-SKU-004", "Producto Sin Auth", 1000.00m);
            ClearUserClaims();
            
            var request = new UpdateProductRequest
            {
                Articulo = new Articulo
                {
                    IdArticulo = articulo.IdArticulo,
                    Sku = "UPD-SKU-004",
                    Nombre = "No Cambiar",
                    Descripcion = articulo.Descripcion,
                    PrecioCosto = 5000.00m
                }
            };

            // Act
            var result = await _controller.UpdateProduct(articulo.IdArticulo, request);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
            
            // Verificar que no cambió
            var productoEnBD = await _context.Articulos.FindAsync(articulo.IdArticulo);
            Assert.Equal("Producto Sin Auth", productoEnBD!.Nombre);
            Assert.Equal(1000.00m, productoEnBD.PrecioCosto);
        }

        [Fact]
        public async Task UpdateProduct_ProductoNoExistente_RetornaNotFound()
        {
            // Arrange
            SetupUserClaims(1, 1, "admin");
            var request = new UpdateProductRequest
            {
                Articulo = new Articulo
                {
                    IdArticulo = 99999,
                    Sku = "INEXISTENTE",
                    Nombre = "No Existe",
                    Descripcion = "No Existe",
                    PrecioCosto = 1000.00m
                }
            };

            // Act
            var result = await _controller.UpdateProduct(99999, request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            dynamic mensaje = notFoundResult.Value!;
            Assert.Contains("no encontrado", mensaje.message.ToString());
        }

        [Fact]
        public async Task UpdateProduct_IDNoCoincide_RetornaBadRequest()
        {
            // Arrange
            var articulo = await CreateTestArticulo("UPD-SKU-005", "Producto Test", 1000.00m);
            SetupUserClaims(1, 1, "admin");
            
            var request = new UpdateProductRequest
            {
                Articulo = new Articulo
                {
                    IdArticulo = articulo.IdArticulo + 1, // ID diferente
                    Sku = "UPD-SKU-005",
                    Nombre = "Test",
                    Descripcion = "Test",
                    PrecioCosto = 1000.00m
                }
            };

            // Act
            var result = await _controller.UpdateProduct(articulo.IdArticulo, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            dynamic mensaje = badRequestResult.Value!;
            Assert.Contains("no coincide", mensaje.message.ToString());
        }

        [Fact]
        public async Task UpdateProduct_SKUDuplicado_RetornaConflict()
        {
            // Arrange
            var articulo1 = await CreateTestArticulo("UPD-SKU-DUP-1", "Producto 1", 1000.00m);
            var articulo2 = await CreateTestArticulo("UPD-SKU-DUP-2", "Producto 2", 2000.00m);
            SetupUserClaims(1, 1, "admin");
            
            // Intentar cambiar el SKU de articulo2 al SKU de articulo1
            var request = new UpdateProductRequest
            {
                Articulo = new Articulo
                {
                    IdArticulo = articulo2.IdArticulo,
                    Sku = "UPD-SKU-DUP-1", // SKU que ya existe en articulo1
                    Nombre = "Producto 2",
                    Descripcion = "Producto 2",
                    PrecioCosto = 2000.00m
                }
            };

            // Act
            var result = await _controller.UpdateProduct(articulo2.IdArticulo, request);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            dynamic mensaje = conflictResult.Value!;
            Assert.Contains("ya está en uso", mensaje.message.ToString());
            
            // Verificar que no cambió en la BD
            var productoEnBD = await _context.Articulos.FindAsync(articulo2.IdArticulo);
            Assert.Equal("UPD-SKU-DUP-2", productoEnBD!.Sku);
        }

        [Fact]
        public async Task UpdateProduct_MismoSKU_PermiteActualizacion()
        {
            // Arrange
            var articulo = await CreateTestArticulo("UPD-SKU-SAME", "Nombre Original", 1000.00m);
            SetupUserClaims(1, 1, "admin");
            
            var request = new UpdateProductRequest
            {
                Articulo = new Articulo
                {
                    IdArticulo = articulo.IdArticulo,
                    Sku = "UPD-SKU-SAME", // Mismo SKU
                    Nombre = "Nombre Actualizado",
                    Descripcion = "Nueva descripción",
                    PrecioCosto = 1500.00m
                }
            };

            // Act
            var result = await _controller.UpdateProduct(articulo.IdArticulo, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            var productoEnBD = await _context.Articulos.FindAsync(articulo.IdArticulo);
            Assert.Equal("UPD-SKU-SAME", productoEnBD!.Sku);
            Assert.Equal("Nombre Actualizado", productoEnBD.Nombre);
        }

        [Fact]
        public async Task UpdateProduct_ConInventarioAsociado_ActualizaAuditoria()
        {
            // Arrange
            var articulo = await CreateTestArticulo("UPD-SKU-INV", "Producto con Inventario", 2000.00m);
            
            // Crear inventario asociado
            var inventario = new Inventario
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 50,
                Ubicacion = "Almacen A",
                UltimaModificacionPor = 1
            };
            _context.Inventarios.Add(inventario);
            await _context.SaveChangesAsync();
            
            var fechaOriginal = inventario.UltimaActualizacion;
            
            // Esperar un momento para asegurar que la fecha cambie
            await Task.Delay(1000);
            
            SetupUserClaims(2, 2, "gestor");
            var request = new UpdateProductRequest
            {
                Articulo = new Articulo
                {
                    IdArticulo = articulo.IdArticulo,
                    Sku = "UPD-SKU-INV",
                    Nombre = "Producto Actualizado",
                    Descripcion = articulo.Descripcion,
                    PrecioCosto = 2500.00m
                }
            };

            // Act
            var result = await _controller.UpdateProduct(articulo.IdArticulo, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            // Verificar que la auditoría del inventario se actualizó
            var inventarioEnBD = await _context.Inventarios
                .FirstOrDefaultAsync(i => i.IdArticulo == articulo.IdArticulo);
            Assert.NotNull(inventarioEnBD);
            Assert.Equal(2, inventarioEnBD.UltimaModificacionPor); // Usuario gestor
            Assert.True(inventarioEnBD.UltimaActualizacion > fechaOriginal);
        }

        [Fact]
        public async Task UpdateProduct_MultiplesCampos_TodosSeActualizan()
        {
            // Arrange
            var articulo = await CreateTestArticulo("UPD-SKU-FULL", "Nombre Original", 1000.00m);
            SetupUserClaims(1, 1, "admin");
            
            var request = new UpdateProductRequest
            {
                Articulo = new Articulo
                {
                    IdArticulo = articulo.IdArticulo,
                    Sku = "UPD-SKU-FULL-NEW",
                    Nombre = "Nuevo Nombre Completo",
                    Descripcion = "Nueva descripción detallada con más información",
                    PrecioCosto = 5432.10m
                }
            };

            // Act
            var result = await _controller.UpdateProduct(articulo.IdArticulo, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            var productoEnBD = await _context.Articulos.FindAsync(articulo.IdArticulo);
            Assert.Equal("UPD-SKU-FULL-NEW", productoEnBD!.Sku);
            Assert.Equal("Nuevo Nombre Completo", productoEnBD.Nombre);
            Assert.Equal("Nueva descripción detallada con más información", productoEnBD.Descripcion);
            Assert.Equal(5432.10m, productoEnBD.PrecioCosto);
        }

        [Fact]
        public async Task UpdateProduct_ConCaracteresEspeciales_SeActualizaCorrectamente()
        {
            // Arrange
            var articulo = await CreateTestArticulo("UPD-SPECIAL", "Producto Normal", 1000.00m);
            SetupUserClaims(1, 1, "admin");
            
            var request = new UpdateProductRequest
            {
                Articulo = new Articulo
                {
                    IdArticulo = articulo.IdArticulo,
                    Sku = "UPD-SPECIAL",
                    Nombre = "Producto con áccéntos y ñ",
                    Descripcion = "Descripción: símbolos @#$%&*()",
                    PrecioCosto = 1234.56m
                }
            };

            // Act
            var result = await _controller.UpdateProduct(articulo.IdArticulo, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            var productoEnBD = await _context.Articulos.FindAsync(articulo.IdArticulo);
            Assert.Equal("Producto con áccéntos y ñ", productoEnBD!.Nombre);
            Assert.Contains("@#$%&*", productoEnBD.Descripcion);
        }

        [Fact]
        public async Task UpdateProduct_ActualizacionParcial_MantieneCamposNoModificados()
        {
            // Arrange
            var articulo = await CreateTestArticulo("UPD-PARTIAL", "Nombre Original", 1000.00m);
            articulo.Descripcion = "Descripción Original Importante";
            _context.Articulos.Update(articulo);
            await _context.SaveChangesAsync();
            
            SetupUserClaims(1, 1, "admin");
            var request = new UpdateProductRequest
            {
                Articulo = new Articulo
                {
                    IdArticulo = articulo.IdArticulo,
                    Sku = "UPD-PARTIAL",
                    Nombre = "Nombre Actualizado",
                    Descripcion = "Descripción Original Importante", // No cambia
                    PrecioCosto = 1000.00m // No cambia
                }
            };

            // Act
            var result = await _controller.UpdateProduct(articulo.IdArticulo, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            var productoEnBD = await _context.Articulos.FindAsync(articulo.IdArticulo);
            Assert.Equal("Nombre Actualizado", productoEnBD!.Nombre);
            Assert.Equal("Descripción Original Importante", productoEnBD.Descripcion);
            Assert.Equal(1000.00m, productoEnBD.PrecioCosto);
        }
    }
}
