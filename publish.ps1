#!/usr/bin/env pwsh
# Publishes TI4 Companion as a self-contained linux-x64 bundle for the server.
#
# Why this script exists (don't remove): the app is served as PLAIN STATIC FILES
# (UseBlazorFrameworkFiles + UseStaticFiles behind Apache), NOT via MapStaticAssets. With .NET 9+
# Blazor WASM fingerprinting that means the runtime import map in index.html is never populated, so
# the boot chain 404s and the page spins forever. Two things fix it:
#   1. <WasmFingerprintAssets>false</WasmFingerprintAssets> in Ti4Companion.Web.csproj keeps the
#      dotnet.* runtime files at stable names (resolve without an import map).
#   2. blazor.webassembly.js is STILL fingerprinted, so its real <hash> name must be written into
#      index.html (which ships with the `#[.{fingerprint}]` placeholder unresolved) — done below.
#
# Usage:  ./publish.ps1            (output in ./publish)
#         ./publish.ps1 -OutDir X  (custom output folder)
# Then upload to the server, e.g.:
#         scp -r ./publish/wwwroot root@SERVER:/opt/ti4companion/
#         scp -r ./publish/*       root@SERVER:/opt/ti4companion/   (first deploy: everything)
param([string]$OutDir = "publish")

$ErrorActionPreference = "Stop"
# Anchor a relative OutDir to the repo root. The .NET static IO APIs below ([IO.File]::ReadAllText)
# resolve relative paths against the PROCESS working directory — which in an elevated PowerShell is
# C:\WINDOWS\system32, NOT the shell's cd location (Set-Location doesn't move the process CWD).
if (-not [IO.Path]::IsPathRooted($OutDir)) { $OutDir = Join-Path $PSScriptRoot $OutDir }
$proj = Join-Path $PSScriptRoot "Ti4Companion.ApiService/Ti4Companion.ApiService.csproj"

dotnet publish $proj -c Release -r linux-x64 --self-contained true -o $OutDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# Bake the real blazor.webassembly.<hash>.js name into index.html (replaces the placeholder).
$fw = Join-Path $OutDir "wwwroot/_framework"
$boot = (Get-ChildItem (Join-Path $fw "blazor.webassembly.*.js") |
         Where-Object { $_.Name -notmatch '\.(gz|br)$' } | Select-Object -First 1).Name
if (-not $boot) { throw "blazor.webassembly.*.js not found in $fw" }

$idx = Join-Path $OutDir "wwwroot/index.html"
$html = [IO.File]::ReadAllText($idx)
$html = [regex]::Replace($html, '_framework/blazor\.webassembly[^"]*\.js', "_framework/$boot")
[IO.File]::WriteAllText($idx, $html, (New-Object System.Text.UTF8Encoding $false))

Write-Host "Boot script set to: _framework/$boot"
Write-Host "Publish ready in '$OutDir'."
