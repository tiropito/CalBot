#!/usr/bin/env bash
set -euo pipefail

echo "==> Creating solution CalBot.sln"
dotnet new sln -n CalBot --force

echo "==> Adding projects"
for p in CalBot.Core CalBot.Data CalBot.Holidays CalBot.Web CalBot.Cli CalBot.Tests; do
dotnet sln CalBot.sln add "<span class="katex"><span class="katex-mathml"><math xmlns="http://www.w3.org/1998/Math/MathML"><semantics><mrow><mi>p</mi><mi mathvariant="normal">/</mi></mrow><annotation encoding="application/x-tex">p/</annotation></semantics></math></span><span class="katex-html" aria-hidden="true"><span class="base"><span class="strut" style="height:1em;vertical-align:-0.25em;"></span><span class="mord mathnormal">p</span><span class="mord">/</span></span></span></span>p.csproj"
done

echo "==> Restoring"
dotnet restore

echo
echo "Готово. Дальше:"
echo "  docker compose up -d"
echo "  cd CalBot.Data && dotnet ef migrations add InitialCreate && dotnet ef database update"
echo "  cd CalBot.Web && dotnet user-secrets set "Auth:Password" "твой_пароль" && dotnet run"