using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace  MinimalAPI.Dominio.ModelViews;

public class ErrosDeValidacao
{
    public List<string> Mensagens { get; set; } = new List<string>();
}