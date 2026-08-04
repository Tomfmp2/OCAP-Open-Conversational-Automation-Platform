# OCAP local without Docker (Windows PowerShell)
# Usage:  .\scripts\ocap-dev.ps1

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
if (-not $Root) { $Root = (Resolve-Path "$PSScriptRoot\..").Path }

Write-Host "OCAP local (sin Docker)" -ForegroundColor Cyan
Write-Host "Root: $Root"

$envFile = Join-Path $Root ".env"
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:UseInMemory = "true"

$apiCmd = "Set-Location '$Root'; if (Test-Path '$envFile') { Get-Content '$envFile' | ForEach-Object { `$l=`$_.Trim(); if (`$l -and -not `$l.StartsWith('#')) { `$i=`$l.IndexOf('='); if (`$i -gt 0) { Set-Item -Path Env:`$(`$l.Substring(0,`$i).Trim()) -Value `$l.Substring(`$i+1).Trim() } } } }; `$env:ASPNETCORE_ENVIRONMENT='Development'; if (-not `$env:UseInMemory) { `$env:UseInMemory='true' }; if ([string]::IsNullOrWhiteSpace(`$env:AiProviders__EnableMock) -and [string]::IsNullOrWhiteSpace(`$env:AiProviders__Gemini__ApiKey) -and [string]::IsNullOrWhiteSpace(`$env:AiProviders__OpenAI__ApiKey)) { `$env:AiProviders__EnableMock='true' }; Write-Host 'API http://localhost:5229' -ForegroundColor Green; Write-Host ('IA: Gemini=' + (-not [string]::IsNullOrWhiteSpace(`$env:AiProviders__Gemini__ApiKey)) + ' Mock=' + `$env:AiProviders__EnableMock); dotnet run --project src/Api/OCAP.Api --launch-profile http"

$feCmd = "Set-Location '$Root\frontend'; if (-not (Test-Path node_modules)) { npm.cmd install }; Write-Host 'Frontend http://localhost:3000' -ForegroundColor Green; npm.cmd run dev"

Start-Process powershell -ArgumentList "-NoExit", "-Command", $apiCmd
Start-Sleep -Seconds 2
Start-Process powershell -ArgumentList "-NoExit", "-Command", $feCmd

Write-Host ""
Write-Host "Se abrieron 2 ventanas: API (5229) y Frontend (3000)." -ForegroundColor Cyan
Write-Host "Login: admin@ocap.io / ChangeMe_Admin_2026!"
Write-Host "Prueba: http://localhost:3000/login -> Canales (WebChat) -> /channels/webchat"
Write-Host "Designer: http://localhost:3000/workflows/designer"
