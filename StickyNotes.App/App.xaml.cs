using System;
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
