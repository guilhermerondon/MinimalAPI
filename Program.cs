using MinimmalAPI.Infraestrtutra.DB;
using MinimalAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Dominio.Interfaces;
using MinimalAPI.Dominio.Servicos;
using Microsoft.AspNetCore.Mvc;
using MinimalAPI.Dominio.ModelViews;
using MinimalAPI.Dominio.Entidades;
using MinimalAPI.Dominio.Enuns;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MinimalAPI;
using MinimalAPI.Configurations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.OpenApi.Models;
using System.Security.Cryptography.Xml;
using System.Net;
using Microsoft.AspNetCore.Authorization;

#region Builder
var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
var key = jwtSettings?.SecretKey ?? "12345612345612345612345612345612"; // Use uma chave padrão se não estiver configurada

// Configuração de autenticação
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings?.Issuer,
        ValidAudience = jwtSettings?.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();

builder.Services.AddDbContext<DbContexto>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConexaoPadrao")));
builder.Services.AddEndpointsApiExplorer();



builder.Services.AddSwaggerGen(options =>{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme{
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        In = ParameterLocation.Header,
        Description = "Insira seu Token JWT aqui: "
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
        
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string [] {}
        }
    });
});


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

#endregion



#region Veiculos
ErrosDeValidacao validaDTO(VeiculoDTO veiculoDTO)
{
    var validacao = new ErrosDeValidacao();

    if (string.IsNullOrEmpty(veiculoDTO.Nome))
        validacao.Mensagens.Add("O nome não pode ser vazio");

    if (string.IsNullOrEmpty(veiculoDTO.Marca))
        validacao.Mensagens.Add("A marca não pode ficar em branco");

    if (veiculoDTO.Ano < 1950)
        validacao.Mensagens.Add("Veículo muito antigo para cadastro");

    return validacao; 
}

app.MapPost("/veiculos", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
{
    var validacao = validaDTO(veiculoDTO);
    if (validacao.Mensagens.Count > 0)
        return Results.BadRequest(validacao);

    var veiculo = new Veiculo
    {
        Nome = veiculoDTO.Nome,
        Marca = veiculoDTO.Marca,
        Ano = veiculoDTO.Ano
    };

    veiculoServico.Incluir(veiculo);

    return Results.Created($"/veiculo/{veiculo.Id}", veiculo);
}).RequireAuthorization()
  .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"})
  .RequireAuthorization(new AuthorizeAttribute { Roles = "Editor"})
  .WithTags("Administrador");


app.MapGet("/veiculos", (IVeiculoServico veiculoServico, [FromQuery] int pagina, [FromQuery] string? nome = null, [FromQuery] string? marca = null) =>
{
    var veiculos = veiculoServico.Todos(pagina, nome, marca);
    return Results.Ok(veiculos);
}).RequireAuthorization().WithTags("Veiculo");



app.MapGet("/veiculo/{id}", (IVeiculoServico veiculoServico, [FromRoute] int id) =>
{
    var veiculo = veiculoServico.BuscaPorId(id);
    if (veiculo == null) 
        return Results.NotFound();
    return Results.Ok(veiculo);
}).RequireAuthorization()
  .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"})
  .RequireAuthorization(new AuthorizeAttribute { Roles = "Editor"})
  .WithTags("Administrador");



app.MapPut("/veiculo/{id}", (IVeiculoServico veiculoServico, VeiculoDTO veiculoDTO, [FromRoute] int id) =>
{
    var veiculo = veiculoServico.BuscaPorId(id);
    if (veiculo == null);


    var validacao = validaDTO(veiculoDTO);
    if (validacao.Mensagens.Count > 0)
        return Results.BadRequest(validacao);

    veiculo.Nome = veiculoDTO.Nome;
    veiculo.Marca = veiculoDTO.Marca;
    veiculo.Ano = veiculoDTO.Ano;

    veiculoServico.Atualizar(veiculo);

    return Results.Ok(veiculo);
}).RequireAuthorization()
  .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"})
  .WithTags("Administrador");



app.MapDelete("/veiculo/{id}", (IVeiculoServico veiculoServico, [FromRoute] int id, [FromQuery] string? nome = null, [FromQuery] string? marca = null) => {
    var veiculo = veiculoServico.BuscaPorId(id);
    if(veiculo == null) return Results.NotFound();
    veiculoServico.Apagar(veiculo);
    return Results.NoContent();
}).RequireAuthorization()
  .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"})
  .WithTags("Administrador");
#endregion



#region Home
app.MapGet("/", () => Results.Json(new Home())).AllowAnonymous().WithTags("Home");
#endregion

#region Administradores             
string GerarTokenJwt(Administrador administrador)
{
    // Certifique-se de que a chave não está vazia
    if (string.IsNullOrEmpty(key))
        throw new InvalidOperationException("A chave JWT não pode estar vazia.");

    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>()
    {
        new Claim(ClaimTypes.Email, administrador.Email),
        new Claim("Perfil", administrador.Perfil.ToString()), // Certifique-se de que Perfil é uma string ou use ToString()
        new Claim(ClaimTypes.Role, administrador.Perfil)
    };

    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.UtcNow.AddDays(1), // Use UTC para evitar problemas de fuso horário
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}




// Rota para login de administrador
app.MapPost("/administradores/Login", ([FromBody]LoginDTO loginDTO, IAdministradorServico administradorServico) => {
    var administrador = administradorServico.Login(loginDTO);
    if (administrador != null){

        string token = GerarTokenJwt(administrador);
        return Results.Ok(new AdministradorLogado
        {
            Email = administrador.Email,
            Perfil = administrador.Perfil,
            Token = token
        });
    }
    else
        return Results.Unauthorized();
}).AllowAnonymous().WithTags("Administrador");

// Rota para obter todos os administradores com paginação
app.MapGet("/administradores", ([FromQuery] int? pagina, IAdministradorServico administradorServico) => {
    var adms = new List<AdministradorModelView>();
    var administradores = administradorServico.Todos(pagina);
    foreach(var adm in administradores)
    {
        adms.Add(new AdministradorModelView{
            Id = adm.Id,
            Email = adm.Email,
            Perfil = adm.Perfil
        });
    }
    return Results.Ok(adms);
}).RequireAuthorization()
  .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"})
  .WithTags("Administrador");

// Rota para obter um administrador por ID
app.MapGet("/administradores/{id}", (IAdministradorServico administradorServico, [FromRoute] int id) =>
{
    var administrador = administradorServico.BuscaPorId(id); // Nome corrigido para consistência
    if (administrador == null) 
        return Results.NotFound();

    return Results.Ok(new AdministradorModelView {
        Id = administrador.Id,
        Email = administrador.Email,
        Perfil = administrador.Perfil
    });
}).RequireAuthorization()
  .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"})
  .WithTags("Administrador");

// Rota para cadastrar um novo administrador
app.MapPost("/administradores/Cadastrar", ([FromBody] AdministradorDTO administradorDTO, IAdministradorServico administradorServico) => {
    var validacao = new ErrosDeValidacao{
        Mensagens = new List<string>()
    };
    if (string.IsNullOrEmpty(administradorDTO.Email))
        validacao.Mensagens.Add("Email não pode ser vazio");
    if (string.IsNullOrEmpty(administradorDTO.Senha))
        validacao.Mensagens.Add("Senha não pode ser vazia");
    if (administradorDTO.Perfil == null)
        validacao.Mensagens.Add("Perfil não pode ser vazio");

    if (validacao.Mensagens.Count > 0)
        return Results.BadRequest(validacao);

    var administrador = new Administrador{
        Email = administradorDTO.Email,
        Senha = administradorDTO.Senha,
        Perfil = administradorDTO.Perfil?.ToString() ?? Perfil.Editor.ToString()
    };
    administradorServico.Incluir(administrador);

    return Results.Created($"/administrador/{administrador.Id}", new AdministradorModelView{
        Id = administrador.Id,
        Email = administrador.Email,
        Perfil = administrador.Perfil
    });
}).RequireAuthorization()
  .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"})
  .WithTags("Administrador");
#endregion



#region App
app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthentication();

app.Run();
#endregion
