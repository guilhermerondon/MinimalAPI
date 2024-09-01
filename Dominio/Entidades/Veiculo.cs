using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MinimalAPI.Dominio.Entidades;

public class Veiculo
{

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    
    public int Id { get; set;} = default !;

    [Required]
    [StringLength(255)]
    public string Nome { get; set;} = default !;

    [StringLength(55)]
    public string Marca { get; set;} = default !;
    
    public int Ano { get; set;} = default !;

}