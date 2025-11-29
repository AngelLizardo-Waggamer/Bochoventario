using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_service.Models;

[Table("Roles")]
public class Rol
{
    [Key]
    [Column("id_rol")]
    public int IdRol { get; set; }

    [Required]
    [Column("nombre_rol")]
    [MaxLength(50)]
    public string NombreRol { get; set; } = string.Empty;

    // Relación: Un rol puede tener muchos usuarios
    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
