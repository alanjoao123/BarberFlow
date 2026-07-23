namespace Barbearia.Api.Models;

public class Agendamento
{
    public int Id { get; set; }

    public DateOnly Data { get; set; }
    public TimeOnly Hora { get; set; }
    public StatusAgendamento Status { get; set; } = StatusAgendamento.Confirmado;

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    public int BarbeiroId { get; set; }
    public Barbeiro Barbeiro { get; set; } = null!;
}
