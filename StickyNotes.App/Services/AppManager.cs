using StickyNotes.Core.Models;

namespace StickyNotes.App.Services;

public class AppManager
{
    // Singleton sencillo para resolver referencias de código que esperan AppManager.Instance
    public static AppManager Instance { get; } = new AppManager();

    private AppManager()
    {
    }

    public void CreateAndOpenNewNote(double x, double y)
    {
        // Implementación mínima: en la versión completa crearía una nueva nota y la abriría.
    }

    public void OpenNoteAsFloating(Note? note)
    {
        // Implementación mínima
    }

    public void SwitchToFloatingMode()
    {
        // Implementación mínima
    }
}
