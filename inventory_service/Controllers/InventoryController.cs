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

        // GET /api/products → lista con filtros ?q=&category=
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

        // GET /api/products/{id} → detalle
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

        // POST /api/products → crear
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

        // PUT /api/products/{id} → actualizar
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

        // DELETE /api/products/{id} → eliminar
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
    }
}
