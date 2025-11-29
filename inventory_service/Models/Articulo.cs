using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_service.Models;

[Table("Articulos")]
public class Articulo
{
    [Key]
    [Column("id_articulo")]
    public int IdArticulo { get; set; }

    [Required]
    [Column("sku")]
    [MaxLength(50)]
    public string Sku { get; set; } = string.Empty;

    [Required]
    [Column("nombre")]
    [MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Column("descripcion")]
    public string? Descripcion { get; set; }

    [Column("precio_costo")]
    public decimal PrecioCosto { get; set; } = 0.00m;

    // Relación: Un artículo puede tener muchos registros de inventario
    public ICollection<Inventario> Inventarios { get; set; } = new List<Inventario>();
}
