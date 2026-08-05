using Barbearia.Api.Data;
using Barbearia.Api.Models;
using Barbearia.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddDbContext<BarbeariaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("BarbeariaDb")));

builder.Services.AddScoped<AgendamentoService>();

const string PoliticaFrontEnd = "PoliticaFrontEnd";
builder.Services.AddCors(options =>
{
    options.AddPolicy(PoliticaFrontEnd, policy =>
    {
        policy.WithOrigins("http://localhost:5057", "https://localhost:7248", "https://alanjoao123.github.io")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(PoliticaFrontEnd);

app.MapGet("/hello", () => "Olá, mundo! A API da Barbearia está no ar! 💈");

app.MapGet("/barbeiros", async (BarbeariaDbContext contexto) =>
    await contexto.Barbeiros.ToListAsync());

app.MapGet("/clientes", async (BarbeariaDbContext contexto) =>
    await contexto.Clientes.ToListAsync());

var agendamentos = app.MapGroup("/agendamentos");

agendamentos.MapGet("/", async (BarbeariaDbContext contexto) =>
    await contexto.Agendamentos
        .Include(a => a.Cliente)
        .Include(a => a.Barbeiro)
        .Where(a => a.Status == StatusAgendamento.Confirmado)
        .OrderBy(a => a.Data).ThenBy(a => a.Hora)
        .ToListAsync());

agendamentos.MapGet("/horarios-disponiveis", async (int barbeiroId, DateOnly data, AgendamentoService service) =>
{
    var horarios = await service.ListarHorariosDisponiveisAsync(barbeiroId, data);
    return Results.Ok(horarios);
});

agendamentos.MapPost("/", async (CriarAgendamentoRequest pedido, AgendamentoService service) =>
{
    try
    {
        var agendamento = await service.CriarAgendamentoAsync(pedido.ClienteId, pedido.BarbeiroId, pedido.Data, pedido.Hora);
        return Results.Created($"/agendamentos/{agendamento.Id}", agendamento);
    }
    catch (InvalidOperationException erro)
    {
        return Results.BadRequest(new { mensagem = erro.Message });
    }
});

agendamentos.MapPost("/{id:int}/cancelar", async (int id, AgendamentoService service) =>
{
    try
    {
        await service.CancelarAgendamentoAsync(id);
        return Results.NoContent();
    }
    catch (InvalidOperationException erro)
    {
        return Results.BadRequest(new { mensagem = erro.Message });
    }
});

app.Run();

record CriarAgendamentoRequest(int ClienteId, int BarbeiroId, DateOnly Data, TimeOnly Hora);
