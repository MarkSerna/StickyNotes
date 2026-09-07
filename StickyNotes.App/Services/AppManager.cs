using System;
using StickyNotes.Core.Models;

namespace StickyNotes.App.Services;

public class AppManager
{
    public static AppManager Instance { get; } = new AppManager();
    private WindowManager? _windowManager;

    public Views.SideNotesWindow? SideNotes => _windowManager?.SideNotesWindow;

    private AppManager()
    {
    }

    public void RegisterWindowManager(WindowManager windowManager)
    {
        _windowManager = windowManager;
    }

    public void CreateAndOpenNewNote(double x, double y)
    {
        _ = _windowManager?.CreateAndOpenNewNoteFloatingAsync(x, y);
    }

    public void OpenNoteAsFloating(Note? note)
    {
        if (note != null)
        {
            _ = _windowManager?.OpenNoteAsFloatingAsync(note.Id);
        }
    }

    public void SwitchToFloatingMode()
    {
        _ = _windowManager?.ShowAllFloatingNotesAsync();
    }
}
