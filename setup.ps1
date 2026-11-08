$ErrorActionPreference = "Stop"

Write-Host "==> Creating solution CalBot.sln"
dotnet new sln -n CalBot --force

Write-Host "==> Adding projects"
foreach ($p in @("CalBot.Core","CalBot.Data","CalBot.Holidays","CalBot.Web","CalBot.Cli","CalBot.Tests")) 
{
    dotnet sln CalBot.sln add "p/$p.csproj"
}

Write-Host "==> Restoring"
dotnet restore

Write-Host ""
Write-Host "Ready. Next:"
Write-Host "  docker compose up -d"
Write-Host "  cd CalBot.Data; dotnet ef migrations add InitialCreate; dotnet ef database update"
Write-Host "  cd CalBot.Web; dotnet user-secrets set `"Auth:Password`" `"1234`"; dotnet run"