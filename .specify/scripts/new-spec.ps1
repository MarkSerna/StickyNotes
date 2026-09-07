# Script para inicializar una nueva especificación SDD en .specify/specs
param(
    [Parameter(Mandatory=$true)]
    [string]$Name
)

$rootDir = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$specsDir = Join-Path $rootDir ".specify\specs"
$templatesDir = Join-Path $rootDir ".specify\templates"

if (-not (Test-Path $specsDir)) {
    New-Item -ItemType Directory -Path $specsDir -Force | Out-Null
}

$existingSpecs = Get-ChildItem -Path $specsDir -Directory
$nextIndex = ($existingSpecs.Count + 1).ToString("000")
$slug = ($Name.ToLower() -replace '[^a-z0-9]+', '-').Trim('-')
$specFolder = Join-Path $specsDir "$nextIndex-$slug"

New-Item -ItemType Directory -Path $specFolder -Force | Out-Null

Copy-Item (Join-Path $templatesDir "spec-template.md") (Join-Path $specFolder "spec.md")
Copy-Item (Join-Path $templatesDir "plan-template.md") (Join-Path $specFolder "plan.md")
Copy-Item (Join-Path $templatesDir "tasks-template.md") (Join-Path $specFolder "tasks.md")

Write-Host "✅ Especificación creada exitosamente en: $specFolder" -ForegroundColor Green
Write-Host "Archivos generados:" -ForegroundColor Cyan
Write-Host "  - $specFolder\spec.md"
Write-Host "  - $specFolder\plan.md"
Write-Host "  - $specFolder\tasks.md"
