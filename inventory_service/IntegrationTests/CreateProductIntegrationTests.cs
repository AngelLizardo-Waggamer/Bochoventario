using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using inventory_service.Controllers;
using inventory_service.Models;

namespace inventory_service.IntegrationTests
{
    /// <summary>
    /// Tests de integración para la creación de productos.
    /// Verifica el funcionamiento real contra una base de datos MySQL en Docker.
    /// </summary>
    public class CreateProductIntegrationTests : IntegrationTestBase
    {
        [Fact]
        public async Task CreateProduct_ComoAdministrador_GuardaEnBaseDeDatosReal()
        {
            // Arrange
            SetupUserClaims(1, 1, "admin");
            var request = new CreateProductRequest
            {
                Articulo = new Articulo
                {
                    Sku = "INT-SKU-001",
                    Nombre = "Laptop HP ProBook",
                    Descripcion = "Laptop para desarrollo",
                    PrecioCosto = 18500.00m
                }
            };

            // Act
            var result = await _controller.CreateProduct(request);

            // Assert - Verificar respuesta
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var producto = Assert.IsType<Articulo>(createdResult.Value);
            Assert.Equal("INT-SKU-001", producto.Sku);
            Assert.Equal("Laptop HP ProBook", producto.Nombre);
            Assert.True(producto.IdArticulo > 0);

            // Assert - Verificar que realmente se guardó en la base de datos
            var productoEnBD = await _context.Articulos
                .FirstOrDefaultAsync(a => a.Sku == "INT-SKU-001");
            Assert.NotNull(productoEnBD);
            Assert.Equal("Laptop HP ProBook", productoEnBD.Nombre);
            Assert.Equal(18500.00m, productoEnBD.PrecioCosto);
        }

        [Fact]
        public async Task CreateProduct_ComoGestor_GuardaEnBaseDeDatosReal()
        {
            // Arrange
            SetupUserClaims(2, 2, "gestor");
            var request = new CreateProductRequest
            {
                Articulo = new Articulo
                {
                    Sku = "INT-SKU-002",
                    Nombre = "Mouse Logitech MX Master 3",
                    Descripcion = "Mouse inalámbrico profesional",
                    PrecioCosto = 1499.00m
                }
            };

            // Act
            var result = await _controller.CreateProduct(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var producto = Assert.IsType<Articulo>(createdResult.Value);
            
            // Verificar en base de datos
            var productoEnBD = await _context.Articulos
                .FirstOrDefaultAsync(a => a.Sku == "INT-SKU-002");
            Assert.NotNull(productoEnBD);
            Assert.Equal("Mouse Logitech MX Master 3", productoEnBD.Nombre);
        }

        [Fact]
        public async Task CreateProduct_SinAutenticacion_NoGuardaEnBaseDeDatos()
        {
            // Arrange
            ClearUserClaims(); // Sin autenticación
            var request = new CreateProductRequest
            {
                Articulo = new Articulo
                {
                    Sku = "INT-SKU-003",
                    Nombre = "Producto Sin Auth",
                    Descripcion = "No debería crearse",
                    PrecioCosto = 500.00m
                }
            };

            // Act
            var result = await _controller.CreateProduct(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            
            // Verificar que NO se guardó en la base de datos
            var productoEnBD = await _context.Articulos
                .FirstOrDefaultAsync(a => a.Sku == "INT-SKU-003");
            Assert.Null(productoEnBD);
        }

        [Fact]
        public async Task CreateProduct_ComoLector_NoGuardaEnBaseDeDatos()
        {
            // Arrange
            SetupUserClaims(3, 3, "lector");
            var request = new CreateProductRequest
            {
                Articulo = new Articulo
                {
                    Sku = "INT-SKU-004",
                    Nombre = "Producto Lector",
                    Descripcion = "Usuario sin permisos",
                    PrecioCosto = 750.00m
                }
            };

            // Act
            var result = await _controller.CreateProduct(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            dynamic mensaje = unauthorizedResult.Value!;
            Assert.Contains("no tiene permisos suficientes", mensaje.message.ToString());
            
            // Verificar que NO se guardó en la base de datos
            var productoEnBD = await _context.Articulos
                .FirstOrDefaultAsync(a => a.Sku == "INT-SKU-004");
            Assert.Null(productoEnBD);
        }

        [Fact]
        public async Task CreateProduct_SKUDuplicado_RetornaConflictYNoCreaDuplicado()
        {
            // Arrange - Crear producto inicial
            SetupUserClaims(1, 1, "admin");
            await CreateTestArticulo("INT-SKU-DUP", "Producto Original", 1000.00m);

            // Intentar crear otro con el mismo SKU
            var request = new CreateProductRequest
            {
                Articulo = new Articulo
                {
                    Sku = "INT-SKU-DUP",
                    Nombre = "Producto Duplicado",
                    Descripcion = "No debería crearse",
                    PrecioCosto = 2000.00m
                }
            };

            // Act
            var result = await _controller.CreateProduct(request);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
            dynamic mensaje = conflictResult.Value!;
            Assert.Contains("Ya existe un producto con el SKU", mensaje.message.ToString());

            // Verificar que solo existe UN producto con ese SKU
            var productosConSKU = await _context.Articulos
                .Where(a => a.Sku == "INT-SKU-DUP")
                .ToListAsync();
            Assert.Single(productosConSKU);
            Assert.Equal("Producto Original", productosConSKU[0].Nombre);
        }

        [Fact]
        public async Task CreateProduct_ConRestriccionForeignKey_MantieneIntegridadReferencial()
        {
            // Arrange - Este test verifica que la base de datos real tiene las restricciones
            SetupUserClaims(1, 1, "admin");
            var articulo = await CreateTestArticulo("INT-SKU-FK", "Producto con Inventario", 1500.00m);

            // Crear un inventario asociado
            var inventario = new Inventario
            {
                IdArticulo = articulo.IdArticulo,
                Cantidad = 100,
                Ubicacion = "Almacen A",
                UltimaModificacionPor = 1
            };
            _context.Inventarios.Add(inventario);
            await _context.SaveChangesAsync();

            // Act - Verificar que el inventario se creó correctamente
            var inventarioEnBD = await _context.Inventarios
                .Include(i => i.Articulo)
                .FirstOrDefaultAsync(i => i.IdArticulo == articulo.IdArticulo);

            // Assert
            Assert.NotNull(inventarioEnBD);
            Assert.Equal(100, inventarioEnBD.Cantidad);
            Assert.Equal("Almacen A", inventarioEnBD.Ubicacion);
            Assert.NotNull(inventarioEnBD.Articulo);
            Assert.Equal("Producto con Inventario", inventarioEnBD.Articulo.Nombre);
        }

        [Fact]
        public async Task CreateProduct_MultiplesConcurrentes_TodosSeGuardanCorrectamente()
        {
            // Arrange
            SetupUserClaims(1, 1, "admin");
            var requests = new[]
            {
                new CreateProductRequest
                {
                    Articulo = new Articulo
                    {
                        Sku = "INT-SKU-CONC-1",
                        Nombre = "Producto Concurrente 1",
                        Descripcion = "Test de concurrencia",
                        PrecioCosto = 1000.00m
                    }
                },
                new CreateProductRequest
                {
                    Articulo = new Articulo
                    {
                        Sku = "INT-SKU-CONC-2",
                        Nombre = "Producto Concurrente 2",
                        Descripcion = "Test de concurrencia",
                        PrecioCosto = 2000.00m
                    }
                },
                new CreateProductRequest
                {
                    Articulo = new Articulo
                    {
                        Sku = "INT-SKU-CONC-3",
                        Nombre = "Producto Concurrente 3",
                        Descripcion = "Test de concurrencia",
                        PrecioCosto = 3000.00m
                    }
                }
            };

            // Act - Crear múltiples productos
            foreach (var request in requests)
            {
                var result = await _controller.CreateProduct(request);
                Assert.IsType<CreatedAtActionResult>(result.Result);
            }

            // Assert - Verificar que todos se guardaron
            var productosCreados = await _context.Articulos
                .Where(a => a.Sku.StartsWith("INT-SKU-CONC"))
                .ToListAsync();
            Assert.Equal(3, productosCreados.Count);
        }

        [Fact]
        public async Task CreateProduct_ConCaracteresEspeciales_SeGuardaCorrectamente()
        {
            // Arrange
            SetupUserClaims(1, 1, "admin");
            var request = new CreateProductRequest
            {
                Articulo = new Articulo
                {
                    Sku = "INT-SKU-SPECIAL",
                    Nombre = "Producto con áccéntos y ñ",
                    Descripcion = "Descripción con símbolos: @#$%&*()[]{}|<>",
                    PrecioCosto = 999.99m
                }
            };

            // Act
            var result = await _controller.CreateProduct(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            
            // Verificar en base de datos con caracteres especiales
            var productoEnBD = await _context.Articulos
                .FirstOrDefaultAsync(a => a.Sku == "INT-SKU-SPECIAL");
            Assert.NotNull(productoEnBD);
            Assert.Equal("Producto con áccéntos y ñ", productoEnBD.Nombre);
            Assert.Contains("@#$%&*", productoEnBD.Descripcion);
        }
    }
}
