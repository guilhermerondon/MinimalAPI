using Microsoft.EntityFrameworkCore;
using MinimalAPI.Dominio.Entidades;

namespace MinimmalAPI.Infraestrtutra.DB
{
public class DbContexto : DbContext
{
    private readonly IConfiguration _configurationAppSettings;

    public DbContexto(DbContextOptions<DbContexto> options, IConfiguration configurationAppSettings)
        : base(options)
    {
        _configurationAppSettings = configurationAppSettings;
    }

     public DbSet<Veiculo> Veiculos { get; set; } = default!;
    public DbSet<Administrador> Administradores { get; set; } = default!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Administrador>().HasData(
                new Administrador{
                    Id = 1,
                    Email = "administrador@teste.com",
                    Senha = "12345612345612345612345612345612",
                    Perfil = "Adm"
                }
            );
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var stringConexao = _configurationAppSettings.GetConnectionString("ConexaoPadrao");
            if (!string.IsNullOrEmpty(stringConexao))
            {
                optionsBuilder.UseSqlServer(stringConexao);
            }
        }
    } 
  }
}