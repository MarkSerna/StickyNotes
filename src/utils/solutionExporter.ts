import JSZip from 'jszip';
import {
  GENERATED_STEP2_FILES,
  GENERATED_STEP3_FILES,
  GENERATED_STEP4_FILES,
  GENERATED_STEP5_FILES,
  GENERATED_STEP6_FILES,
  GENERATED_STEP7_FILES,
} from '../components/CodeGeneratedView';

export const SLN_CONTENT = `Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.8.34330.188
MinimumVisualStudioVersion = 10.0.40219.1
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "StickyNotes.Core", "StickyNotes.Core\\StickyNotes.Core.csproj", "{A1111111-1111-1111-1111-111111111111}"
EndProject
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "StickyNotes.Data", "StickyNotes.Data\\StickyNotes.Data.csproj", "{B2222222-2222-2222-2222-222222222222}"
EndProject
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "StickyNotes.Sync", "StickyNotes.Sync\\StickyNotes.Sync.csproj", "{C3333333-3333-3333-3333-333333333333}"
EndProject
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "StickyNotes.App", "StickyNotes.App\\StickyNotes.App.csproj", "{D4444444-4444-4444-4444-444444444444}"
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
`;

export const CSPROJ_CORE = `<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>StickyNotes.Core</RootNamespace>
  </PropertyGroup>
</Project>
`;

export const CSPROJ_DATA = `<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>StickyNotes.Data</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\\StickyNotes.Core\\StickyNotes.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.8" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.8">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
`;

export const CSPROJ_SYNC = `<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>StickyNotes.Sync</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\\StickyNotes.Core\\StickyNotes.Core.csproj" />
    <ProjectReference Include="..\\StickyNotes.Data\\StickyNotes.Data.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Google.Apis.Drive.v3" Version="1.68.0.3568" />
    <PackageReference Include="System.Security.Cryptography.ProtectedData" Version="8.0.0" />
  </ItemGroup>
</Project>
`;

export const CSPROJ_APP = `<Project Sdk="Microsoft.NET.Sdk">
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
    <ProjectReference Include="..\\StickyNotes.Core\\StickyNotes.Core.csproj" />
    <ProjectReference Include="..\\StickyNotes.Data\\StickyNotes.Data.csproj" />
    <ProjectReference Include="..\\StickyNotes.Sync\\StickyNotes.Sync.csproj" />
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
`;

export const APP_MANIFEST = `<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <assemblyIdentity version="1.0.0.0" name="StickyNotes.App"/>
  <compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
    <application>
      <!-- Windows 10 and Windows 11 -->
      <supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}" />
    </application>
  </compatibility>
  <application xmlns="urn:schemas-microsoft-com:asm.v3">
    <windowsSettings>
      <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
    </windowsSettings>
  </application>
</assembly>
`;

export const APP_XAML = `<Application
    x:Class="StickyNotes.App.App"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
`;

export const APP_XAML_CS = `using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using StickyNotes.App.Services;

namespace StickyNotes.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App(IServiceProvider services)
    {
        Services = services;
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var windowManager = Services.GetRequiredService<WindowManager>();
        windowManager.Initialize();
    }
}
`;

export async function generateAndDownloadSolutionZip(): Promise<void> {
  const zip = new JSZip();

  // Root Solution File
  zip.file('StickyNotes.sln', SLN_CONTENT);

  // Project Definition Files (.csproj)
  zip.file('StickyNotes.Core/StickyNotes.Core.csproj', CSPROJ_CORE);
  zip.file('StickyNotes.Data/StickyNotes.Data.csproj', CSPROJ_DATA);
  zip.file('StickyNotes.Sync/StickyNotes.Sync.csproj', CSPROJ_SYNC);
  zip.file('StickyNotes.App/StickyNotes.App.csproj', CSPROJ_APP);
  zip.file('StickyNotes.App/app.manifest', APP_MANIFEST);
  zip.file('StickyNotes.App/App.xaml', APP_XAML);
  zip.file('StickyNotes.App/App.xaml.cs', APP_XAML_CS);

  // All Code Files from Steps 2 to 7
  const allFiles = [
    ...GENERATED_STEP2_FILES,
    ...GENERATED_STEP3_FILES,
    ...GENERATED_STEP4_FILES,
    ...GENERATED_STEP5_FILES,
    ...GENERATED_STEP6_FILES,
    ...GENERATED_STEP7_FILES,
  ];

  allFiles.forEach((file) => {
    zip.file(file.path, file.content);
  });

  // Generate blob and trigger download
  const blob = await zip.generateAsync({ type: 'blob' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = 'StickyNotes-Visual-Studio-2022.zip';
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}
