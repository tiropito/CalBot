using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CalBot.Data;

/// <summary>
/// Точка входа для `dotnet ef` — чтобы миграции работали без запуска хоста.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CalBotContext>
{
public CalBotContext CreateDbContext(string[] args)
{
var conn = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
?? "Host=localhost;Port=5433;Database=calbot;Username=calbot;Password=calbot_dev";

var options = new DbContextOptionsBuilder<CalBotContext>()
.UseNpgsql(conn)
.Options;

return new CalBotContext(options);
}
}