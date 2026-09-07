# ==============================================================================
# Script de Generación Automática de la Solución de Visual Studio 2022
# Notas Rápidas Windows 11 (.NET 8 + WinUI 3 + Google Drive)
# ==============================================================================

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host " Generando Solución StickyNotes para Visual Studio 2022" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

$baseDir = $PSScriptRoot
if ([string]::IsNullOrEmpty($baseDir)) {
    $baseDir = Get-Location
}

# 1. Crear el archivo de Solución StickyNotes.sln
$slnPath = Join-Path $baseDir "StickyNotes.sln"
$slnContent = @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.8.34330.188
MinimumVisualStudioVersion = 10.0.40219.1
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "StickyNotes.Core", "StickyNotes.Core\StickyNotes.Core.csproj", "{A1111111-1111-1111-1111-111111111111}"
EndProject
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "StickyNotes.Data", "StickyNotes.Data\StickyNotes.Data.csproj", "{B2222222-2222-2222-2222-222222222222}"
EndProject
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "StickyNotes.Sync", "StickyNotes.Sync\StickyNotes.Sync.csproj", "{C3333333-3333-3333-3333-333333333333}"
EndProject
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "StickyNotes.App", "StickyNotes.App\StickyNotes.App.csproj", "{D4444444-4444-4444-4444-444444444444}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|x64 = Debug|x64
		Release|x64 = Release|x64
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{A1111111-1111-1111-1111-111111111111}.Debug|x64.ActiveCfg = Debug|Any CPU
		{A1111111-1111-1111-1111-111111111111}.Debug|x64.Build.0 = Debug|Any CPU
		{A1111111-1111-1111-1111-111111111111}.Release|x64.ActiveCfg = Release|Any CPU
		{A1111111-1111-1111-1111-111111111111}.Release|x64.Build.0 = Release|Any CPU
		{B2222222-2222-2222-2222-222222222222}.Debug|x64.ActiveCfg = Debug|Any CPU
		{B2222222-2222-2222-2222-222222222222}.Debug|x64.Build.0 = Debug|Any CPU
		{B2222222-2222-2222-2222-222222222222}.Release|x64.ActiveCfg = Release|Any CPU
		{B2222222-2222-2222-2222-222222222222}.Release|x64.Build.0 = Release|Any CPU
		{C3333333-3333-3333-3333-333333333333}.Debug|x64.ActiveCfg = Debug|Any CPU
		{C3333333-3333-3333-3333-333333333333}.Debug|x64.Build.0 = Debug|Any CPU
		{C3333333-3333-3333-3333-333333333333}.Release|x64.ActiveCfg = Release|Any CPU
		{C3333333-3333-3333-3333-333333333333}.Release|x64.Build.0 = Release|Any CPU
		{D4444444-4444-4444-4444-444444444444}.Debug|x64.ActiveCfg = Debug|x64
		{D4444444-4444-4444-4444-444444444444}.Debug|x64.Build.0 = Debug|x64
		{D4444444-4444-4444-4444-444444444444}.Release|x64.ActiveCfg = Release|x64
		{D4444444-4444-4444-4444-444444444444}.Release|x64.Build.0 = Release|x64
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
"@

Set-Content -Path $slnPath -Value $slnContent -Encoding UTF8
Write-Host "[OK] Creado: StickyNotes.sln" -ForegroundColor Green

# 2. Crear carpetas de los 4 proyectos
$projects = @("StickyNotes.Core", "StickyNotes.Data", "StickyNotes.Sync", "StickyNotes.App")
foreach ($p in $projects) {
    $dir = Join-Path $baseDir $p
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
}

# 3. Crear StickyNotes.Core.csproj
$coreCsproj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>StickyNotes.Core</RootNamespace>
  </PropertyGroup>
</Project>
"@
Set-Content -Path (Join-Path $baseDir "StickyNotes.Core\StickyNotes.Core.csproj") -Value $coreCsproj -Encoding UTF8

# 4. Crear StickyNotes.Data.csproj
$dataCsproj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>StickyNotes.Data</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\StickyNotes.Core\StickyNotes.Core.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.8" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.8">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
"@
Set-Content -Path (Join-Path $baseDir "StickyNotes.Data\StickyNotes.Data.csproj") -Value $dataCsproj -Encoding UTF8

# 5. Crear StickyNotes.Sync.csproj
$syncCsproj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>StickyNotes.Sync</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\StickyNotes.Core\StickyNotes.Core.csproj" />
    <ProjectReference Include="..\StickyNotes.Data\StickyNotes.Data.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Google.Apis.Drive.v3" Version="1.68.0.3568" />
    <PackageReference Include="System.Security.Cryptography.ProtectedData" Version="8.0.0" />
  </ItemGroup>
</Project>
"@
Set-Content -Path (Join-Path $baseDir "StickyNotes.Sync\StickyNotes.Sync.csproj") -Value $syncCsproj -Encoding UTF8

# 6. Crear StickyNotes.App.csproj
$appCsproj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <RootNamespace>StickyNotes.App</RootNamespace>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <Platforms>x64;ARM64</Platforms>
    <RuntimeIdentifiers>win-x64;win-arm64</RuntimeIdentifiers>
    <UseWinUI>true</UseWinUI>
    <WindowsPackageType>None</WindowsPackageType>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\StickyNotes.Core\StickyNotes.Core.csproj" />
    <ProjectReference Include="..\StickyNotes.Data\StickyNotes.Data.csproj" />
    <ProjectReference Include="..\StickyNotes.Sync\StickyNotes.Sync.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.5.240802000" />
    <PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.26100.1" />
    <PackageReference Include="H.NotifyIcon.WinUI" Version="2.1.0" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.8" />
  </ItemGroup>
  <ItemGroup>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
"@
Set-Content -Path (Join-Path $baseDir "StickyNotes.App\StickyNotes.App.csproj") -Value $appCsproj -Encoding UTF8

Write-Host ""
Write-Host "========================================================" -ForegroundColor Green
Write-Host " [EXITO] Archivos creados correctamente!" -ForegroundColor Green
Write-Host " Ya puedes volver a Visual Studio 2022 y abrir 'StickyNotes.sln'!" -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Green
