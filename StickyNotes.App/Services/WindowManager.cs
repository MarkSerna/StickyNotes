using Microsoft.Extensions.DependencyInjection;
using StickyNotes.App.Views;

namespace StickyNotes.App.Services;

public class WindowManager
{
    private readonly IServiceScopeFactory _scopeFactory;
    private IServiceScope? _windowScope;
    private SideNotesWindow? _sideNotesWindow;

    public WindowManager(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void Initialize()
    {
        if (_sideNotesWindow == null)
        {
            _windowScope = _scopeFactory.CreateScope();
            _sideNotesWindow = _windowScope.ServiceProvider.GetRequiredService<SideNotesWindow>();
            _sideNotesWindow.Closed += SideNotesWindow_Closed;
        }

        _sideNotesWindow.Activate();
    }

    private void SideNotesWindow_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        if (_sideNotesWindow != null)
        {
            _sideNotesWindow.Closed -= SideNotesWindow_Closed;
            _sideNotesWindow = null;
        }

        _windowScope?.Dispose();
        _windowScope = null;
    }
}
