using System.Collections.ObjectModel;
using System.Threading.Tasks;
using StickyNotes.Core.Models;
using StickyNotes.Data.Repositories;

namespace StickyNotes.App.ViewModels;

public class SideNotesViewModel
{
    private readonly INoteRepository _repository;

    public ObservableCollection<Note> Notes { get; } = new();

    public SideNotesViewModel(INoteRepository repository)
    {
        _repository = repository;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var items = await _repository.GetActiveNotesAsync();
        Notes.Clear();
        foreach (var n in items)
        {
            Notes.Add(n);
        }
    }
}
