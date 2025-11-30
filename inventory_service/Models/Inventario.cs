using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_service.Models;

[Table("Inventario")]
public class Inventario
{
    [Key]
    [Column("id_inventario")]
    public int IdInventario { get; set; }

    [Required]
    [Column("id_articulo")]
    public int IdArticulo { get; set; }

    [Required]
    [Column("cantidad")]
    public int Cantidad { get; set; } = 0;

    [Column("ubicacion")]
    [MaxLength(50)]
    public string? Ubicacion { get; set; }

    [Column("ultima_modificacion_por")]
    public int? UltimaModificacionPor { get; set; }

    [Column("ultima_actualizacion")]
    public DateTime UltimaActualizacion { get; set; } = DateTime.Now;

    // Navegación: Relación con Artículo
    [ForeignKey("IdArticulo")]
    public Articulo Articulo { get; set; } = null!;

    // Navegación: Relación con Usuario que hizo la última modificación
    [ForeignKey("UltimaModificacionPor")]
    public Usuario? UsuarioModificador { get; set; }
}
