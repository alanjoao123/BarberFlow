using System.Text.Json.Serialization;

namespace Barbearia.Web.Models;

public record BarbeiroDto(int Id, string Nome);

public record ClienteDto(int Id, string Nome);

public record AgendamentoDto(int Id, DateOnly Data, TimeOnly Hora, string Status, ClienteDto Cliente, BarbeiroDto Barbeiro);

public record ErroDto([property: JsonPropertyName("mensagem")] string Mensagem);
