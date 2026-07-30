using Barbearia.Api.Data;
using Barbearia.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Barbearia.Api.Services;

public class AgendamentoService
{
    private static readonly TimeOnly HoraAbertura = new(8, 0);
    private static readonly TimeOnly HoraFechamento = new(20, 0);
    private static readonly TimeSpan DuracaoAtendimento = TimeSpan.FromMinutes(45);
    private static readonly DayOfWeek[] DiasFechados = { DayOfWeek.Sunday, DayOfWeek.Monday };

    private readonly BarbeariaDbContext _contexto;
    private readonly ILogger<AgendamentoService> _logger;

    public AgendamentoService(BarbeariaDbContext contexto, ILogger<AgendamentoService> logger)
    {
        _contexto = contexto;
        _logger = logger;
    }

    public async Task<List<TimeOnly>> ListarHorariosDisponiveisAsync(int barbeiroId, DateOnly data)
    {
        if (DiasFechados.Contains(data.DayOfWeek))
        {
            return new List<TimeOnly>();
        }

        var todosOsHorarios = GerarTodosOsHorariosDoDia();

        var horariosJaOcupados = await _contexto.Agendamentos
            .Where(a => a.BarbeiroId == barbeiroId && a.Data == data && a.Status == StatusAgendamento.Confirmado)
            .Select(a => a.Hora)
            .ToListAsync();

        return todosOsHorarios
            .Where(horario => !horariosJaOcupados.Contains(horario))
            .ToList();
    }

    public async Task<Agendamento> CriarAgendamentoAsync(int clienteId, int barbeiroId, DateOnly data, TimeOnly hora)
    {
        var horariosDisponiveis = await ListarHorariosDisponiveisAsync(barbeiroId, data);

        if (!horariosDisponiveis.Contains(hora))
        {
            _logger.LogWarning(
                "Tentativa de agendamento recusada: barbeiro {BarbeiroId} já ocupado em {Data} às {Hora}",
                barbeiroId, data, hora);
            throw new InvalidOperationException("Esse horário não está disponível para esse barbeiro.");
        }

        var novoAgendamento = new Agendamento
        {
            ClienteId = clienteId,
            BarbeiroId = barbeiroId,
            Data = data,
            Hora = hora,
            Status = StatusAgendamento.Confirmado
        };

        _contexto.Agendamentos.Add(novoAgendamento);
        await _contexto.SaveChangesAsync();

        _logger.LogInformation(
            "Agendamento {AgendamentoId} criado: cliente {ClienteId}, barbeiro {BarbeiroId}, {Data} às {Hora}",
            novoAgendamento.Id, clienteId, barbeiroId, data, hora);

        return novoAgendamento;
    }

    public async Task CancelarAgendamentoAsync(int agendamentoId)
    {
        var agendamento = await _contexto.Agendamentos.FindAsync(agendamentoId);

        if (agendamento is null)
        {
            _logger.LogWarning("Tentativa de cancelar agendamento inexistente: Id {AgendamentoId}", agendamentoId);
            throw new InvalidOperationException("Agendamento não encontrado.");
        }

        agendamento.Status = StatusAgendamento.Cancelado;
        await _contexto.SaveChangesAsync();

        _logger.LogInformation("Agendamento {AgendamentoId} cancelado", agendamentoId);
    }

    private static List<TimeOnly> GerarTodosOsHorariosDoDia()
    {
        var horarios = new List<TimeOnly>();
        var horarioAtual = HoraAbertura;

        while (horarioAtual.Add(DuracaoAtendimento) <= HoraFechamento)
        {
            horarios.Add(horarioAtual);
            horarioAtual = horarioAtual.Add(DuracaoAtendimento);
        }

        return horarios;
    }
}
