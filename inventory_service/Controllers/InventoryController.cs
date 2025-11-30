using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using inventory_service.Models;
using inventory_service.Data;
using System.Security.Claims;

namespace inventory_service.Controllers
{
    // DTOs para las peticiones (sin IdUsuario, se extrae del token)
    public class CreateProductRequest
    {
        public required Articulo Articulo { get; set; }
    }

    public class UpdateProductRequest
    {
        public required Articulo Articulo { get; set; }
    }

    // DTOs para gestión de inventario
    public class CreateInventoryRequest
    {
        public int IdArticulo { get; set; }
        public int Cantidad { get; set; }
        public required string Ubicacion { get; set; }
    }

    public class UpdateInventoryRequest
    {
        public int Cantidad { get; set; }
    }

    public class AdjustInventoryRequest
    {
        public int Ajuste { get; set; } // Positivo para entrada, negativo para salida
    }

    [Route("api/inventory")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InventoryController(AppDbContext context)
        {
            _context = context;
        }

        // Método helper para obtener el ID del usuario desde el token JWT
        private int? GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst("id_usuario");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }

        // Método helper para obtener el nombre de usuario desde el token JWT
        private string? GetUsernameFromToken()
        {
            var usernameClaim = User.FindFirst("nombre_usuario");
            return usernameClaim?.Value;
        }

        // Método helper para obtener el ID del rol del usuario desde el token JWT
        private int? GetRoleIdFromToken()
        {
            var roleIdClaim = User.FindFirst("id_rol");
            if (roleIdClaim != null && int.TryParse(roleIdClaim.Value, out int roleId))
            {
                return roleId;
            }
            return null;
        }

        // Método helper para validar permisos del usuario autenticado
        private (bool isValid, int? userId, string? errorMessage) ValidateUserPermissions()
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
            {
                return (false, null, "No se pudo obtener el ID del usuario del token JWT");
            }

            var username = GetUsernameFromToken();
            if (string.IsNullOrEmpty(username))
            {
                return (false, null, "No se pudo obtener el nombre de usuario del token JWT");
            }

            var roleId = GetRoleIdFromToken();
            if (roleId == null)
            {
                return (false, null, "No se pudo obtener el rol del usuario del token JWT");
            }

            // Validar que el rol sea Administrador (1) o Gestor (2)
            if (roleId.Value != 1 && roleId.Value != 2)
            {
                return (false, null, $"El usuario '{username}' no tiene permisos suficientes. Se requiere rol de Administrador o Gestor.");
            }

            return (true, userId, null);
        }

        // GET /api/inventory → lista con filtros ?q=&category=
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Articulo>>> GetProducts(
            [FromQuery] string? q,
            [FromQuery] string? category)
        {
            var query = _context.Articulos.AsQueryable();

            // Filtro por búsqueda general (nombre, SKU o descripción)
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(a => 
                    a.Nombre.Contains(q) || 
                    a.Sku.Contains(q) || 
                    (a.Descripcion != null && a.Descripcion.Contains(q)));
            }

            // Filtro por categoría (si se implementa en el futuro)
            if (!string.IsNullOrWhiteSpace(category))
            {
                // Por ahora, podemos filtrar por descripción que contenga la categoría
                query = query.Where(a => a.Descripcion != null && a.Descripcion.Contains(category));
            }

            var products = await query.ToListAsync();
            return Ok(products);
        }

        // GET /api/inventory/{id} → detalle
        [HttpGet("{id}")]
        public async Task<ActionResult<Articulo>> GetProduct(int id)
        {
            var articulo = await _context.Articulos
                .Include(a => a.Inventarios)
                .FirstOrDefaultAsync(a => a.IdArticulo == id);

            if (articulo == null)
            {
                return NotFound(new { message = $"Producto con ID {id} no encontrado" });
            }

            return Ok(articulo);
        }

        // POST /api/inventory → crear
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Articulo>> CreateProduct([FromBody] CreateProductRequest request)
        {
            // Validar permisos del usuario desde el token JWT
            var (isValid, userId, errorMessage) = ValidateUserPermissions();
            if (!isValid)
            {
                return Unauthorized(new { message = errorMessage });
            }

            // Validar que el SKU no exista
            if (await _context.Articulos.AnyAsync(a => a.Sku == request.Articulo.Sku))
            {
                return Conflict(new { message = $"Ya existe un producto con el SKU '{request.Articulo.Sku}'" });
            }

            _context.Articulos.Add(request.Articulo);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = request.Articulo.IdArticulo }, request.Articulo);
        }

        // PUT /api/inventory/{id} → actualizar
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            if (id != request.Articulo.IdArticulo)
            {
                return BadRequest(new { message = "El ID del producto no coincide" });
            }

            // Validar permisos del usuario desde el token JWT
            var (isValid, userId, errorMessage) = ValidateUserPermissions();
            if (!isValid)
            {
                return Unauthorized(new { message = errorMessage });
            }

            var existingArticulo = await _context.Articulos.FindAsync(id);
            if (existingArticulo == null)
            {
                return NotFound(new { message = $"Producto con ID {id} no encontrado" });
            }

            // Validar que el SKU no esté en uso por otro producto
            if (await _context.Articulos.AnyAsync(a => a.Sku == request.Articulo.Sku && a.IdArticulo != id))
            {
                return Conflict(new { message = $"El SKU '{request.Articulo.Sku}' ya está en uso por otro producto" });
            }

            // Actualizar los campos
            existingArticulo.Sku = request.Articulo.Sku;
            existingArticulo.Nombre = request.Articulo.Nombre;
            existingArticulo.Descripcion = request.Articulo.Descripcion;
            existingArticulo.PrecioCosto = request.Articulo.PrecioCosto;

            // Actualizar la fecha de modificación y usuario en los inventarios asociados
            var inventarios = await _context.Inventarios
                .Where(i => i.IdArticulo == id)
                .ToListAsync();
            
            foreach (var inventario in inventarios)
            {
                inventario.UltimaActualizacion = DateTime.Now;
                inventario.UltimaModificacionPor = userId!.Value;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Articulos.AnyAsync(a => a.IdArticulo == id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE /api/inventory/{id} → eliminar
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            // Validar permisos del usuario desde el token JWT
            var (isValid, userId, errorMessage) = ValidateUserPermissions();
            if (!isValid)
            {
                return Unauthorized(new { message = errorMessage });
            }

            var articulo = await _context.Articulos.FindAsync(id);
            if (articulo == null)
            {
                return NotFound(new { message = $"Producto con ID {id} no encontrado" });
            }

            _context.Articulos.Remove(articulo);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // === ENDPOINTS PARA GESTIÓN DE INVENTARIO ===

        // GET /api/inventory/stock/{idArticulo} → obtener inventario por artículo
        [HttpGet("stock/{idArticulo}")]
        public async Task<ActionResult<IEnumerable<Inventario>>> GetInventoryByProduct(int idArticulo)
        {
            var articulo = await _context.Articulos.FindAsync(idArticulo);
            if (articulo == null)
            {
                return NotFound(new { message = $"Producto con ID {idArticulo} no encontrado" });
            }

            var inventarios = await _context.Inventarios
                .Include(i => i.UsuarioModificador)
                .Where(i => i.IdArticulo == idArticulo)
                .ToListAsync();

            return Ok(inventarios);
        }

        // GET /api/inventory/stock/location/{ubicacion} → obtener inventario por ubicación
        [HttpGet("stock/location/{ubicacion}")]
        public async Task<ActionResult<IEnumerable<Inventario>>> GetInventoryByLocation(string ubicacion)
        {
            var inventarios = await _context.Inventarios
                .Include(i => i.Articulo)
                .Include(i => i.UsuarioModificador)
                .Where(i => i.Ubicacion == ubicacion)
                .ToListAsync();

            return Ok(inventarios);
        }

        // GET /api/inventory/stock → listar todo el inventario
        [HttpGet("stock")]
        public async Task<ActionResult<IEnumerable<Inventario>>> GetAllInventory()
        {
            var inventarios = await _context.Inventarios
                .Include(i => i.Articulo)
                .Include(i => i.UsuarioModificador)
                .ToListAsync();

            return Ok(inventarios);
        }

        // POST /api/inventory/stock → crear registro de inventario
        [HttpPost("stock")]
        [Authorize]
        public async Task<ActionResult<Inventario>> CreateInventory([FromBody] CreateInventoryRequest request)
        {
            // Validar permisos del usuario desde el token JWT
            var (isValid, userId, errorMessage) = ValidateUserPermissions();
            if (!isValid)
            {
                return Unauthorized(new { message = errorMessage });
            }

            // Validar que el artículo exista
            var articulo = await _context.Articulos.FindAsync(request.IdArticulo);
            if (articulo == null)
            {
                return NotFound(new { message = $"Producto con ID {request.IdArticulo} no encontrado" });
            }

            // Validar que no exista ya un registro para este artículo en esta ubicación
            if (await _context.Inventarios.AnyAsync(i => i.IdArticulo == request.IdArticulo && i.Ubicacion == request.Ubicacion))
            {
                return Conflict(new { message = $"Ya existe un registro de inventario para el artículo {articulo.Nombre} en la ubicación '{request.Ubicacion}'" });
            }

            var inventario = new Inventario
            {
                IdArticulo = request.IdArticulo,
                Cantidad = request.Cantidad,
                Ubicacion = request.Ubicacion,
                UltimaModificacionPor = userId!.Value,
                UltimaActualizacion = DateTime.Now
            };

            _context.Inventarios.Add(inventario);
            await _context.SaveChangesAsync();

            // Cargar las relaciones para la respuesta
            await _context.Entry(inventario).Reference(i => i.Articulo).LoadAsync();
            await _context.Entry(inventario).Reference(i => i.UsuarioModificador).LoadAsync();

            return CreatedAtAction(nameof(GetInventoryByProduct), new { idArticulo = inventario.IdArticulo }, inventario);
        }

        // PUT /api/inventory/stock/{id} → actualizar cantidad de inventario
        [HttpPut("stock/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateInventory(int id, [FromBody] UpdateInventoryRequest request)
        {
            // Validar permisos del usuario desde el token JWT
            var (isValid, userId, errorMessage) = ValidateUserPermissions();
            if (!isValid)
            {
                return Unauthorized(new { message = errorMessage });
            }

            var inventario = await _context.Inventarios.FindAsync(id);
            if (inventario == null)
            {
                return NotFound(new { message = $"Registro de inventario con ID {id} no encontrado" });
            }

            inventario.Cantidad = request.Cantidad;
            inventario.UltimaModificacionPor = userId!.Value;
            inventario.UltimaActualizacion = DateTime.Now;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH /api/inventory/stock/{id}/adjust → ajustar inventario (entrada/salida)
        [HttpPatch("stock/{id}/adjust")]
        [Authorize]
        public async Task<IActionResult> AdjustInventory(int id, [FromBody] AdjustInventoryRequest request)
        {
            // Validar permisos del usuario desde el token JWT
            var (isValid, userId, errorMessage) = ValidateUserPermissions();
            if (!isValid)
            {
                return Unauthorized(new { message = errorMessage });
            }

            var inventario = await _context.Inventarios.FindAsync(id);
            if (inventario == null)
            {
                return NotFound(new { message = $"Registro de inventario con ID {id} no encontrado" });
            }

            int nuevaCantidad = inventario.Cantidad + request.Ajuste;
            if (nuevaCantidad < 0)
            {
                return BadRequest(new { message = $"No hay suficiente stock. Stock actual: {inventario.Cantidad}, ajuste solicitado: {request.Ajuste}" });
            }

            inventario.Cantidad = nuevaCantidad;
            inventario.UltimaModificacionPor = userId!.Value;
            inventario.UltimaActualizacion = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Inventario ajustado. Nueva cantidad: {inventario.Cantidad}", cantidad = inventario.Cantidad });
        }

        // DELETE /api/inventory/stock/{id} → eliminar registro de inventario
        [HttpDelete("stock/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteInventory(int id)
        {
            // Validar permisos del usuario desde el token JWT
            var (isValid, userId, errorMessage) = ValidateUserPermissions();
            if (!isValid)
            {
                return Unauthorized(new { message = errorMessage });
            }

            var inventario = await _context.Inventarios.FindAsync(id);
            if (inventario == null)
            {
                return NotFound(new { message = $"Registro de inventario con ID {id} no encontrado" });
            }

            _context.Inventarios.Remove(inventario);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
