using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace  MinimalAPI.Dominio.ModelViews;

public struct Home
{
    public string Mensagem { get => "Bem Vindo a API de veiculos - Minimal API"; }
    public string Doc { get => "/swagger"; }

}