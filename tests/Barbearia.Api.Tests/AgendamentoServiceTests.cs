using Barbearia.Api.Data;
using Barbearia.Api.Models;
using Barbearia.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Barbearia.Api.Tests;

public class AgendamentoServiceTests
{
    private static BarbeariaDbContext CriarContextoDeTeste()
    {
        var opcoes = new DbContextOptionsBuilder<BarbeariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BarbeariaDbContext(opcoes);
    }

    // Um "agora" fixo, sempre antes das datas de teste (2026-07-25/26) — assim os testes
    // nunca dependem da hora real do computador rodando o teste.
    private static readonly DateTimeOffset AgoraDeReferencia = new(2026, 7, 1, 6, 0, 0, TimeSpan.Zero);

    private static AgendamentoService CriarServiceDeTeste(BarbeariaDbContext contexto, TimeProvider? relogio = null)
    {
        return new AgendamentoService(contexto, NullLogger<AgendamentoService>.Instance, relogio ?? new RelogioFalso(AgoraDeReferencia));
    }

    private sealed class RelogioFalso : TimeProvider
    {
        private readonly DateTimeOffset _agora;
        public RelogioFalso(DateTimeOffset agora) => _agora = agora;
        public override DateTimeOffset GetUtcNow() => _agora;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    [Fact]
    public async Task ListarHorariosDisponiveis_DeveRetornarListaVazia_QuandoBarbeariaEstaFechada()
    {
        // Arrange
        using var contexto = CriarContextoDeTeste();
        var service = CriarServiceDeTeste(contexto);
        var domingo = new DateOnly(2026, 7, 26);

        // Act
        var horarios = await service.ListarHorariosDisponiveisAsync(barbeiroId: 1, domingo);

        // Assert
        Assert.Empty(horarios);
    }

    [Fact]
    public async Task ListarHorariosDisponiveis_DeveRetornar16Horarios_QuandoDiaAbertoSemAgendamentos()
    {
        // Arrange
        using var contexto = CriarContextoDeTeste();
        var service = CriarServiceDeTeste(contexto);
        var sabado = new DateOnly(2026, 7, 25);

        // Act
        var horarios = await service.ListarHorariosDisponiveisAsync(barbeiroId: 1, sabado);

        // Assert
        Assert.Equal(16, horarios.Count);
    }

    [Fact]
    public async Task ListarHorariosDisponiveis_NaoDeveIncluirHorarioJaOcupado()
    {
        // Arrange
        using var contexto = CriarContextoDeTeste();
        var sabado = new DateOnly(2026, 7, 25);
        var horarioOcupado = new TimeOnly(10, 15);

        contexto.Agendamentos.Add(new Agendamento
        {
            ClienteId = 1,
            BarbeiroId = 1,
            Data = sabado,
            Hora = horarioOcupado,
            Status = StatusAgendamento.Confirmado
        });
        await contexto.SaveChangesAsync();

        var service = CriarServiceDeTeste(contexto);

        // Act
        var horarios = await service.ListarHorariosDisponiveisAsync(barbeiroId: 1, sabado);

        // Assert
        Assert.DoesNotContain(horarioOcupado, horarios);
    }

    [Fact]
    public async Task CriarAgendamento_DeveCriarComSucesso_QuandoHorarioDisponivel()
    {
        // Arrange
        using var contexto = CriarContextoDeTeste();
        var service = CriarServiceDeTeste(contexto);
        var sabado = new DateOnly(2026, 7, 25);
        var horario = new TimeOnly(10, 15);

        // Act
        var agendamento = await service.CriarAgendamentoAsync(clienteId: 1, barbeiroId: 1, sabado, horario);

        // Assert
        Assert.Equal(StatusAgendamento.Confirmado, agendamento.Status);
        Assert.Equal(1, await contexto.Agendamentos.CountAsync());
    }

    [Fact]
    public async Task CriarAgendamento_DeveLancarErro_QuandoHorarioJaOcupado()
    {
        // Arrange
        using var contexto = CriarContextoDeTeste();
        var sabado = new DateOnly(2026, 7, 25);
        var horario = new TimeOnly(10, 15);

        contexto.Agendamentos.Add(new Agendamento
        {
            ClienteId = 1,
            BarbeiroId = 1,
            Data = sabado,
            Hora = horario,
            Status = StatusAgendamento.Confirmado
        });
        await contexto.SaveChangesAsync();

        var service = CriarServiceDeTeste(contexto);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CriarAgendamentoAsync(clienteId: 2, barbeiroId: 1, sabado, horario));
    }

    [Fact]
    public async Task CancelarAgendamento_DeveAlterarStatusParaCancelado()
    {
        // Arrange
        using var contexto = CriarContextoDeTeste();
        var agendamento = new Agendamento
        {
            ClienteId = 1,
            BarbeiroId = 1,
            Data = new DateOnly(2026, 7, 25),
            Hora = new TimeOnly(10, 15),
            Status = StatusAgendamento.Confirmado
        };
        contexto.Agendamentos.Add(agendamento);
        await contexto.SaveChangesAsync();

        var service = CriarServiceDeTeste(contexto);

        // Act
        await service.CancelarAgendamentoAsync(agendamento.Id);

        // Assert
        var agendamentoAtualizado = await contexto.Agendamentos.FindAsync(agendamento.Id);
        Assert.Equal(StatusAgendamento.Cancelado, agendamentoAtualizado!.Status);
    }

    [Fact]
    public async Task CancelarAgendamento_DeveLancarErro_QuandoAgendamentoNaoExiste()
    {
        // Arrange
        using var contexto = CriarContextoDeTeste();
        var service = CriarServiceDeTeste(contexto);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CancelarAgendamentoAsync(999));
    }

    [Fact]
    public async Task ListarHorariosDisponiveis_NaoDeveIncluirHorarioQueJaPassou_QuandoDataEHoje()
    {
        // Arrange
        using var contexto = CriarContextoDeTeste();
        var agora = new DateTimeOffset(2026, 8, 5, 10, 0, 0, TimeSpan.Zero); // 10h da manhã
        var relogioFalso = new RelogioFalso(agora);
        var service = CriarServiceDeTeste(contexto, relogioFalso);
        var hoje = DateOnly.FromDateTime(agora.DateTime);

        // Act
        var horarios = await service.ListarHorariosDisponiveisAsync(barbeiroId: 1, hoje);

        // Assert
        Assert.DoesNotContain(new TimeOnly(8, 0), horarios);
        Assert.DoesNotContain(new TimeOnly(9, 30), horarios);
        Assert.Contains(new TimeOnly(11, 0), horarios);
    }

    [Fact]
    public async Task ListarHorariosDisponiveis_DeveRetornarListaVazia_QuandoDataEDoPassado()
    {
        // Arrange
        using var contexto = CriarContextoDeTeste();
        var agora = new DateTimeOffset(2026, 8, 5, 10, 0, 0, TimeSpan.Zero);
        var relogioFalso = new RelogioFalso(agora);
        var service = CriarServiceDeTeste(contexto, relogioFalso);
        var ontem = DateOnly.FromDateTime(agora.DateTime).AddDays(-1);

        // Act
        var horarios = await service.ListarHorariosDisponiveisAsync(barbeiroId: 1, ontem);

        // Assert
        Assert.Empty(horarios);
    }
}
