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

    public AgendamentoService(BarbeariaDbContext contexto)
    {
        _contexto = contexto;
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

        return novoAgendamento;
    }

    public async Task CancelarAgendamentoAsync(int agendamentoId)
    {
        var agendamento = await _contexto.Agendamentos.FindAsync(agendamentoId);

        if (agendamento is null)
        {
            throw new InvalidOperationException("Agendamento não encontrado.");
        }

        agendamento.Status = StatusAgendamento.Cancelado;
        await _contexto.SaveChangesAsync();
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
