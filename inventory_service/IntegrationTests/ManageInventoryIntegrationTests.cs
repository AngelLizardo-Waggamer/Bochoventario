using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using inventory_service.Controllers;
using inventory_service.Models;

namespace inventory_service.IntegrationTests
{
    /// <summary>
    /// Tests de integración para la creación, actualización y eliminación de inventario (stock).
    /// Verifica operaciones CRUD contra una base de datos MySQL real.
    /// </summary>
    public class ManageInventoryIntegrationTests : IntegrationTestBase
    {
        public ManageInventoryIntegrationTests(DatabaseFixture fixture) : base(fixture) { }

        // === TESTS DE CREACIÓN ===

        [Fact]
        public async Task CreateInventory_ComoAdministrador_GuardaEnBaseDeDatos()
        {
            // Arrange
            var articulo = await CreateTestArticulo("CREATE-INV-001", "Producto Test", 1000m);
            SetupUserClaims(1, 1, "admin");

            var request = new CreateInventoryRequest
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 100,
                Ubicacion = "Almacen Test"
            };

            // Act
            var result = await _controller.CreateInventory(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var inventario = Assert.IsType<Inventario>(createdResult.Value);
            
            Assert.Equal(100, inventario.Cantidad);
            Assert.Equal("Almacen Test", inventario.Ubicacion);
            
            // Verificar en base de datos
            var inventarioDB = await _context.Inventarios
                .FirstOrDefaultAsync(i => i.IdArticulo == articulo.IdArticulo);
            Assert.NotNull(inventarioDB);
            Assert.Equal(100, inventarioDB.Cantidad);
        }

        [Fact]
        public async Task CreateInventory_ComoGestor_GuardaCorrectamente()
        {
            // Arrange
            var articulo = await CreateTestArticulo("CREATE-INV-002", "Producto Gestor", 2000m);
            SetupUserClaims(2, 2, "gestor");

            var request = new CreateInventoryRequest
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 50,
                Ubicacion = "Almacen B"
            };

            // Act
            var result = await _controller.CreateInventory(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var inventario = Assert.IsType<Inventario>(createdResult.Value);
            Assert.Equal(2, inventario.UltimaModificacionPor);
        }

        [Fact]
        public async Task CreateInventory_ArticuloNoExiste_RetornaNotFound()
        {
            // Arrange
            SetupUserClaims(1, 1, "admin");

            var request = new CreateInventoryRequest
            {
                IdArticulo = 99999,
                Cantidad = 50,
                Ubicacion = "Almacen Test"
            };

            // Act
            var result = await _controller.CreateInventory(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            
            // Verificar que no se guardó nada
            var count = await _context.Inventarios.CountAsync();
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task CreateInventory_InventarioDuplicado_RetornaConflict()
        {
            // Arrange
            var articulo = await CreateTestArticulo("DUP-INV-001", "Producto Duplicado", 1000m);
            SetupUserClaims(1, 1, "admin");

            // Crear primer inventario
            var inventario1 = new Inventario
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 50,
                Ubicacion = "Almacen Unico",
                UltimaModificacionPor = 1
            };
            _context.Inventarios.Add(inventario1);
            await _context.SaveChangesAsync();

            // Intentar crear duplicado
            var request = new CreateInventoryRequest
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 100,
                Ubicacion = "Almacen Unico"
            };

            // Act
            var result = await _controller.CreateInventory(request);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
            
            // Verificar que solo existe uno
            var inventarios = await _context.Inventarios
                .Where(i => i.IdArticulo == articulo.IdArticulo)
                .ToListAsync();
            Assert.Single(inventarios);
        }

        [Fact]
        public async Task CreateInventory_MismoArticuloDiferentesUbicaciones_CreaTodos()
        {
            // Arrange
            var articulo = await CreateTestArticulo("MULTI-LOC-001", "Producto Multi-Ubicación", 1500m);
            SetupUserClaims(1, 1, "admin");

            var request1 = new CreateInventoryRequest
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 50,
                Ubicacion = "Almacen A"
            };

            var request2 = new CreateInventoryRequest
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 30,
                Ubicacion = "Almacen B"
            };

            // Act
            await _controller.CreateInventory(request1);
            await _controller.CreateInventory(request2);

            // Assert
            var inventarios = await _context.Inventarios
                .Where(i => i.IdArticulo == articulo.IdArticulo)
                .ToListAsync();
            Assert.Equal(2, inventarios.Count);
            Assert.Equal(80, inventarios.Sum(i => i.Cantidad));
        }

        // === TESTS DE ACTUALIZACIÓN ===

        [Fact]
        public async Task UpdateInventory_ComoAdministrador_ActualizaEnBaseDeDatos()
        {
            // Arrange
            var articulo = await CreateTestArticulo("UPD-INV-001", "Producto Actualizar", 1000m);
            var inventario = new Inventario
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 50,
                Ubicacion = "Almacen Original",
                UltimaModificacionPor = 2
            };
            _context.Inventarios.Add(inventario);
            await _context.SaveChangesAsync();

            SetupUserClaims(1, 1, "admin");
            var request = new UpdateInventoryRequest { Cantidad = 100 };

            // Act
            var result = await _controller.UpdateInventory(inventario.IdInventario, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            var updated = await _context.Inventarios.FindAsync(inventario.IdInventario);
            Assert.Equal(100, updated!.Cantidad);
            Assert.Equal(1, updated.UltimaModificacionPor);
        }

        [Fact]
        public async Task UpdateInventory_CantidadCero_ActualizaCorrectamente()
        {
            // Arrange
            var articulo = await CreateTestArticulo("UPD-ZERO-001", "Producto Cero", 1000m);
            var inventario = new Inventario
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 50,
                Ubicacion = "Almacen Test",
                UltimaModificacionPor = 1
            };
            _context.Inventarios.Add(inventario);
            await _context.SaveChangesAsync();

            SetupUserClaims(1, 1, "admin");
            var request = new UpdateInventoryRequest { Cantidad = 0 };

            // Act
            var result = await _controller.UpdateInventory(inventario.IdInventario, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(0, (await _context.Inventarios.FindAsync(inventario.IdInventario))!.Cantidad);
        }

        // === TESTS DE AJUSTE ===

        [Fact]
        public async Task AdjustInventory_EntradaDeStock_IncrementaCorrectamente()
        {
            // Arrange
            var articulo = await CreateTestArticulo("ADJ-IN-001", "Producto Entrada", 1000m);
            var inventario = new Inventario
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 50,
                Ubicacion = "Almacen Test",
                UltimaModificacionPor = 1
            };
            _context.Inventarios.Add(inventario);
            await _context.SaveChangesAsync();

            SetupUserClaims(1, 1, "admin");
            var request = new AdjustInventoryRequest { Ajuste = 25 };

            // Act
            var result = await _controller.AdjustInventory(inventario.IdInventario, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic response = okResult.Value!;
            Assert.Equal(75, (int)response.cantidad);
            
            var updated = await _context.Inventarios.FindAsync(inventario.IdInventario);
            Assert.Equal(75, updated!.Cantidad);
        }

        [Fact]
        public async Task AdjustInventory_SalidaDeStock_DecrementaCorrectamente()
        {
            // Arrange
            var articulo = await CreateTestArticulo("ADJ-OUT-001", "Producto Salida", 1000m);
            var inventario = new Inventario
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 100,
                Ubicacion = "Almacen Test",
                UltimaModificacionPor = 1
            };
            _context.Inventarios.Add(inventario);
            await _context.SaveChangesAsync();

            SetupUserClaims(1, 1, "admin");
            var request = new AdjustInventoryRequest { Ajuste = -30 };

            // Act
            var result = await _controller.AdjustInventory(inventario.IdInventario, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(70, (await _context.Inventarios.FindAsync(inventario.IdInventario))!.Cantidad);
        }

        [Fact]
        public async Task AdjustInventory_SalidaMayorQueStock_RetornaBadRequest()
        {
            // Arrange
            var articulo = await CreateTestArticulo("ADJ-OVER-001", "Producto Over", 1000m);
            var inventario = new Inventario
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 50,
                Ubicacion = "Almacen Test",
                UltimaModificacionPor = 1
            };
            _context.Inventarios.Add(inventario);
            await _context.SaveChangesAsync();

            SetupUserClaims(1, 1, "admin");
            var request = new AdjustInventoryRequest { Ajuste = -100 };

            // Act
            var result = await _controller.AdjustInventory(inventario.IdInventario, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(50, (await _context.Inventarios.FindAsync(inventario.IdInventario))!.Cantidad);
        }

        [Fact]
        public async Task AdjustInventory_VariosAjustesSecuenciales_AcumulaCorrectamente()
        {
            // Arrange
            var articulo = await CreateTestArticulo("ADJ-SEQ-001", "Producto Secuencial", 1000m);
            var inventario = new Inventario
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 100,
                Ubicacion = "Almacen Test",
                UltimaModificacionPor = 1
            };
            _context.Inventarios.Add(inventario);
            await _context.SaveChangesAsync();

            SetupUserClaims(1, 1, "admin");

            // Act - Múltiples ajustes
            await _controller.AdjustInventory(inventario.IdInventario, new AdjustInventoryRequest { Ajuste = 20 });
            await _controller.AdjustInventory(inventario.IdInventario, new AdjustInventoryRequest { Ajuste = -10 });
            await _controller.AdjustInventory(inventario.IdInventario, new AdjustInventoryRequest { Ajuste = 5 });

            // Assert
            var final = await _context.Inventarios.FindAsync(inventario.IdInventario);
            Assert.Equal(115, final!.Cantidad); // 100 + 20 - 10 + 5
        }

        // === TESTS DE ELIMINACIÓN ===

        [Fact]
        public async Task DeleteInventory_ComoAdministrador_EliminaDeLaBaseDeDatos()
        {
            // Arrange
            var articulo = await CreateTestArticulo("DEL-INV-001", "Producto Eliminar", 1000m);
            var inventario = new Inventario
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 50,
                Ubicacion = "Almacen Test",
                UltimaModificacionPor = 1
            };
            _context.Inventarios.Add(inventario);
            await _context.SaveChangesAsync();

            SetupUserClaims(1, 1, "admin");

            // Act
            var result = await _controller.DeleteInventory(inventario.IdInventario);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            var deleted = await _context.Inventarios.FindAsync(inventario.IdInventario);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task DeleteInventory_NoEliminaArticulo_SoloInventario()
        {
            // Arrange
            var articulo = await CreateTestArticulo("DEL-ART-001", "Producto Preservar", 1000m);
            var inventario = new Inventario
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 50,
                Ubicacion = "Almacen Test",
                UltimaModificacionPor = 1
            };
            _context.Inventarios.Add(inventario);
            await _context.SaveChangesAsync();

            SetupUserClaims(1, 1, "admin");

            // Act
            var result = await _controller.DeleteInventory(inventario.IdInventario);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(await _context.Inventarios.FindAsync(inventario.IdInventario));
            Assert.NotNull(await _context.Articulos.FindAsync(articulo.IdArticulo));
        }

        [Fact]
        public async Task DeleteInventory_UnoDeVariosInventarios_SoloEliminaUno()
        {
            // Arrange
            var articulo = await CreateTestArticulo("DEL-MULTI-001", "Producto Multi", 1000m);
            var inventario1 = new Inventario
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 50,
                Ubicacion = "Almacen A",
                UltimaModificacionPor = 1
            };
            var inventario2 = new Inventario
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 30,
                Ubicacion = "Almacen B",
                UltimaModificacionPor = 1
            };
            _context.Inventarios.AddRange(inventario1, inventario2);
            await _context.SaveChangesAsync();

            SetupUserClaims(1, 1, "admin");

            // Act
            var result = await _controller.DeleteInventory(inventario1.IdInventario);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(await _context.Inventarios.FindAsync(inventario1.IdInventario));
            Assert.NotNull(await _context.Inventarios.FindAsync(inventario2.IdInventario));
        }

        [Fact]
        public async Task DeleteInventory_ComoLector_NoEliminaDeBaseDeDatos()
        {
            // Arrange
            var articulo = await CreateTestArticulo("DEL-LECTOR-001", "Producto Lector", 1000m);
            var inventario = new Inventario
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 50,
                Ubicacion = "Almacen Test",
                UltimaModificacionPor = 1
            };
            _context.Inventarios.Add(inventario);
            await _context.SaveChangesAsync();

            SetupUserClaims(3, 3, "lector");

            // Act
            var result = await _controller.DeleteInventory(inventario.IdInventario);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.NotNull(await _context.Inventarios.FindAsync(inventario.IdInventario));
        }

        [Fact]
        public async Task CreateUpdateDelete_CicloCompleto_FuncionaCorrectamente()
        {
            // Arrange - Crear
            var articulo = await CreateTestArticulo("CYCLE-001", "Producto Ciclo", 1000m);
            SetupUserClaims(1, 1, "admin");

            var createRequest = new CreateInventoryRequest
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 100,
                Ubicacion = "Almacen Ciclo"
            };

            var createResult = await _controller.CreateInventory(createRequest);
            var createdResult = Assert.IsType<CreatedAtActionResult>(createResult.Result);
            var inventario = Assert.IsType<Inventario>(createdResult.Value);

            // Act - Actualizar
            var updateRequest = new UpdateInventoryRequest { Cantidad = 150 };
            await _controller.UpdateInventory(inventario.IdInventario, updateRequest);
            
            var afterUpdate = await _context.Inventarios.FindAsync(inventario.IdInventario);
            Assert.Equal(150, afterUpdate!.Cantidad);

            // Act - Eliminar
            await _controller.DeleteInventory(inventario.IdInventario);

            // Assert - Verificar eliminación
            Assert.Null(await _context.Inventarios.FindAsync(inventario.IdInventario));
        }
    }
}
