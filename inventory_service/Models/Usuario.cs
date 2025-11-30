using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_service.Models;

[Table("Usuarios")]
public class Usuario
{
    [Key]
    [Column("id_usuario")]
    public int IdUsuario { get; set; }

    [Required]
    [Column("id_rol")]
    public int IdRol { get; set; }

    [Required]
    [Column("nombre_usuario")]
    [MaxLength(50)]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required]
    [Column("password_hash")]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("nombre_completo")]
    [MaxLength(100)]
    public string? NombreCompleto { get; set; }

    [Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    // Navegación: Relación con Rol
    [ForeignKey("IdRol")]
    public Rol Rol { get; set; } = null!;

    // Relación: Un usuario puede modificar muchos registros de inventario
    public ICollection<Inventario> InventariosModificados { get; set; } = new List<Inventario>();
}
