using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using inventory_service.Models;

namespace inventory_service.IntegrationTests
{
    /// <summary>
    /// Tests de integración para la consulta de productos con filtros.
    /// Verifica búsquedas y filtros contra una base de datos MySQL real.
    /// </summary>
    public class GetProductsIntegrationTests : IntegrationTestBase
    {
        protected override async Task SeedDatabase()
        {
            await base.SeedDatabase();

            // Seed productos de prueba con diferentes características
            var articulos = new List<Articulo>
            {
                new Articulo
                {
                    Sku = "LAPTOP-001",
                    Nombre = "Laptop Dell XPS 15",
                    Descripcion = "Laptop profesional, categoria: electronica",
                    PrecioCosto = 25000.00m
                },
                new Articulo
                {
                    Sku = "LAPTOP-002",
                    Nombre = "Laptop HP EliteBook",
                    Descripcion = "Laptop empresarial, categoria: electronica",
                    PrecioCosto = 22000.00m
                },
                new Articulo
                {
                    Sku = "MOUSE-001",
                    Nombre = "Mouse Logitech MX Master",
                    Descripcion = "Mouse inalámbrico ergonómico, categoria: accesorios",
                    PrecioCosto = 1499.00m
                },
                new Articulo
                {
                    Sku = "TECLADO-001",
                    Nombre = "Teclado Mecánico RGB",
                    Descripcion = "Teclado gaming con iluminación, categoria: accesorios",
                    PrecioCosto = 2500.00m
                },
                new Articulo
                {
                    Sku = "MONITOR-001",
                    Nombre = "Monitor LG UltraWide 34\"",
                    Descripcion = "Monitor curvo para productividad, categoria: electronica",
                    PrecioCosto = 8500.00m
                }
            };

            _context.Articulos.AddRange(articulos);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetProducts_SinFiltros_RetornaTodosLosProductosDeLaBaseDeDatos()
        {
            // Act
            var result = await _controller.GetProducts(null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResult.Value);
            var listaProductos = productos.ToList();
            
            Assert.True(listaProductos.Count >= 5); // Al menos los 5 que seeded
            
            // Verificar que coincide con la base de datos
            var productosEnBD = await _context.Articulos.ToListAsync();
            Assert.Equal(productosEnBD.Count, listaProductos.Count);
        }

        [Fact]
        public async Task GetProducts_FiltroPorNombre_RetornaProductosCoincidentes()
        {
            // Act
            var result = await _controller.GetProducts("Laptop", null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResult.Value).ToList();
            
            Assert.Equal(2, productos.Count);
            Assert.All(productos, p => Assert.Contains("Laptop", p.Nombre));
            
            // Verificar contra la base de datos
            var productosEnBD = await _context.Articulos
                .Where(a => a.Nombre.Contains("Laptop"))
                .ToListAsync();
            Assert.Equal(productosEnBD.Count, productos.Count);
        }

        [Fact]
        public async Task GetProducts_FiltroPorSKU_RetornaProductoExacto()
        {
            // Act
            var result = await _controller.GetProducts("MOUSE-001", null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResult.Value).ToList();
            
            Assert.Single(productos);
            Assert.Equal("MOUSE-001", productos[0].Sku);
            Assert.Equal("Mouse Logitech MX Master", productos[0].Nombre);
        }

        [Fact]
        public async Task GetProducts_FiltroPorDescripcion_RetornaProductosCoincidentes()
        {
            // Act
            var result = await _controller.GetProducts("inalámbrico", null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResult.Value).ToList();
            
            Assert.Single(productos);
            Assert.Contains("inalámbrico", productos[0].Descripcion);
        }

        [Fact]
        public async Task GetProducts_FiltroPorCategoria_RetornaProductosDeEsaCategoria()
        {
            // Act
            var result = await _controller.GetProducts(null, "electronica");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResult.Value).ToList();
            
            Assert.Equal(3, productos.Count); // Laptop Dell, HP, Monitor LG
            Assert.All(productos, p => Assert.Contains("electronica", p.Descripcion));
        }

        [Fact]
        public async Task GetProducts_FiltroCategoriaAccesorios_RetornaAccesorios()
        {
            // Act
            var result = await _controller.GetProducts(null, "accesorios");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResult.Value).ToList();
            
            Assert.Equal(2, productos.Count); // Mouse y Teclado
            Assert.All(productos, p => Assert.Contains("accesorios", p.Descripcion));
        }

        [Fact]
        public async Task GetProducts_FiltrosCombinados_RetornaProductosQueCoinciden()
        {
            // Act
            var result = await _controller.GetProducts("Mouse", "accesorios");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResult.Value).ToList();
            
            Assert.Single(productos);
            Assert.Contains("Mouse", productos[0].Nombre);
            Assert.Contains("accesorios", productos[0].Descripcion);
        }

        [Fact]
        public async Task GetProducts_BusquedaConMinusculas_EsCaseInsensitive()
        {
            // Act
            var result = await _controller.GetProducts("laptop", null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResult.Value).ToList();
            
            // Debería encontrar "Laptop" aunque busquemos "laptop"
            Assert.Equal(2, productos.Count);
        }

        [Fact]
        public async Task GetProducts_SinResultados_RetornaListaVacia()
        {
            // Act
            var result = await _controller.GetProducts("ProductoInexistenteXYZ", null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResult.Value).ToList();
            
            Assert.Empty(productos);
        }

        [Fact]
        public async Task GetProducts_BusquedaParcialEnSKU_RetornaCoincidencias()
        {
            // Act
            var result = await _controller.GetProducts("LAPTOP", null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResult.Value).ToList();
            
            Assert.Equal(2, productos.Count);
            Assert.All(productos, p => Assert.Contains("LAPTOP", p.Sku));
        }

        [Fact]
        public async Task GetProducts_ConCaracteresEspeciales_ManejaCorrectamente()
        {
            // Arrange - Agregar producto con caracteres especiales
            await CreateTestArticulo("SPECIAL-001", "Producto 34\" Especial", 5000.00m);

            // Act
            var result = await _controller.GetProducts("34\"", null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResult.Value).ToList();
            
            Assert.True(productos.Count >= 1);
            Assert.Contains(productos, p => p.Nombre.Contains("34\""));
        }

        [Fact]
        public async Task GetProducts_OrdenDeLosResultados_MantieneOrdenDeBaseDeDatos()
        {
            // Act
            var result = await _controller.GetProducts(null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResult.Value).ToList();
            
            // Verificar que los IDs están en orden (el orden natural de la BD)
            var productosEnBD = await _context.Articulos.ToListAsync();
            Assert.Equal(productosEnBD.Select(p => p.IdArticulo), productos.Select(p => p.IdArticulo));
        }

        [Fact]
        public async Task GetProducts_DespuesDeCrearNuevoProducto_LoIncluyeEnResultados()
        {
            // Arrange - Contar productos iniciales
            var resultInicial = await _controller.GetProducts(null, null);
            var okResultInicial = Assert.IsType<OkObjectResult>(resultInicial.Result);
            var productosIniciales = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResultInicial.Value).ToList();
            var countInicial = productosIniciales.Count;

            // Crear un nuevo producto
            await CreateTestArticulo("NEW-PROD-001", "Producto Nuevo", 1500.00m);

            // Act
            var result = await _controller.GetProducts(null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsAssignableFrom<IEnumerable<Articulo>>(okResult.Value).ToList();
            
            Assert.Equal(countInicial + 1, productos.Count);
            Assert.Contains(productos, p => p.Sku == "NEW-PROD-001");
        }
    }
}
