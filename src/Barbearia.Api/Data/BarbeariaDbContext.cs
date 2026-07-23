using Microsoft.EntityFrameworkCore;
using Barbearia.Api.Models;

namespace Barbearia.Api.Data;

public class BarbeariaDbContext : DbContext
{
    public BarbeariaDbContext(DbContextOptions<BarbeariaDbContext> options) : base(options)
    {
    }

    public DbSet<Cliente> Clientes { get; set; } = null!;
    public DbSet<Barbeiro> Barbeiros { get; set; } = null!;
    public DbSet<Agendamento> Agendamentos { get; set; } = null!;
}
