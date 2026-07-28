using Barbearia.Api.Data;
using Barbearia.Api.Models;
using Barbearia.Api.Services;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task ListarHorariosDisponiveis_DeveRetornarListaVazia_QuandoBarbeariaEstaFechada()
    {
        // Arrange
        using var contexto = CriarContextoDeTeste();
        var service = new AgendamentoService(contexto);
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
        var service = new AgendamentoService(contexto);
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

        var service = new AgendamentoService(contexto);

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
        var service = new AgendamentoService(contexto);
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

        var service = new AgendamentoService(contexto);

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

        var service = new AgendamentoService(contexto);

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
        var service = new AgendamentoService(contexto);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CancelarAgendamentoAsync(999));
    }
}
