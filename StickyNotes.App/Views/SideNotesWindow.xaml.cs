using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StickyNotes.App.Helpers;
using ColorHelper = StickyNotes.App.Helpers.ColorHelper;
using StickyNotes.Core.Enums;
using StickyNotes.Core.Models;
using StickyNotes.Data.Repositories;
using StickyNotes.Sync.Services;
using Windows.Graphics;
using WinRT.Interop;
using StickyNotes.App.Services;

namespace StickyNotes.App.Views;

public sealed partial class SideNotesWindow : Window
{
    private readonly INoteRepository _repository;
    private readonly AppWindow _appWindow;
    private readonly OverlappedPresenter _presenter;
    private readonly DispatcherTimer _autoHideTimer;
    private readonly DispatcherTimer _debounceTimer;

    private Note? _currentNote;
    private bool _isPinned = false;
    private bool _isRightEdge = true;
    private bool _isExpanded = true;
    private bool _isTrashMode = false;
    private const int ExpandedWidth = 400;
    private const int CollapsedWidth = 14;

    private readonly List<Note> _allLoadedNotes = new();
    public ObservableCollection<Note> Notes { get; } = new();

    public SideNotesWindow(INoteRepository repository)
    {
        InitializeComponent();
        _repository = repository;

        // Configuración de AppWindow WinUI 3 y Win32
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        _presenter = (_appWindow.Presenter as OverlappedPresenter)!;

        // Panel sin bordes ni barra estándar, siempre en primer plano
        _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        _presenter.IsAlwaysOnTop = true;
        _presenter.IsResizable = false;
        _presenter.SetBorderAndTitleBar(false, false);

        // Ajustar al área de trabajo del monitor principal
        PositionToMonitorEdge();

        // Timer de auto-ocultado al retirar el mouse (300ms de gracia)
        _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _autoHideTimer.Tick += (s, e) =>
        {
            _autoHideTimer.Stop();
            if (!_isPinned && _isExpanded)
            {
                SetExpanded(false);
            }
        };

        // Timer de debounce de 500ms para persistencia en SQLite
        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _debounceTimer.Tick += DebounceTimer_Tick;

        // Cargar notas desde SQLite
        _ = LoadNotesAsync();
    }

    private void PositionToMonitorEdge()
    {
        var workArea = MonitorHelper.GetPrimaryMonitorWorkArea();
        var width = _isExpanded ? ExpandedWidth : CollapsedWidth;
        var height = workArea.Height;
        var y = workArea.Y;
        var x = _isRightEdge ? (workArea.X + workArea.Width - width) : workArea.X;

        _appWindow.MoveAndResize(new RectInt32(x, y, width, height));
    }

    private void SetExpanded(bool expand)
    {
        _isExpanded = expand;
        ExpandedPanel.Visibility = expand ? Visibility.Visible : Visibility.Collapsed;
        PeekingHandle.Visibility = expand ? Visibility.Collapsed : Visibility.Visible;
        PositionToMonitorEdge();
    }

    private async Task LoadNotesAsync()
    {
        var items = _isTrashMode 
            ? await _repository.GetTrashNotesAsync() 
            : await _repository.GetActiveNotesAsync();

        _allLoadedNotes.Clear();
        _allLoadedNotes.AddRange(items);

        ApplyFilter(SearchBox?.Text);
    }

    private void ApplyFilter(string? query)
    {
        Notes.Clear();
        var filter = query?.Trim();

        var matched = string.IsNullOrWhiteSpace(filter)
            ? _allLoadedNotes
            : _allLoadedNotes.Where(n =>
                (!string.IsNullOrEmpty(n.Title) && n.Title.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(n.Content) && n.Content.Contains(filter, StringComparison.OrdinalIgnoreCase)));

        foreach (var note in matched)
        {
            Notes.Add(note);
        }

        NotesTabList.ItemsSource = Notes;
        if (Notes.Any())
        {
            NotesTabList.SelectedIndex = 0;
        }
        else
        {
            _currentNote = null;
            TxtActiveNoteTitle.Text = string.Empty;
            ActiveNoteEditor.Document.SetText(TextSetOptions.None, string.Empty);
        }
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        ApplyFilter(sender.Text);
    }

    private void NotesTabList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NotesTabList.SelectedItem is Note selectedNote)
        {
            _currentNote = selectedNote;
            TxtActiveNoteTitle.Text = selectedNote.Title ?? string.Empty;

            try
            {
                ActiveNoteEditor.Document.SetText(TextSetOptions.FormatRtf, selectedNote.Content ?? string.Empty);
            }
            catch
            {
                ActiveNoteEditor.Document.SetText(TextSetOptions.None, selectedNote.Content ?? string.Empty);
            }

            var palette = ColorHelper.GetPalette(selectedNote.Color);
            ActiveNoteContainer.Background = palette.BodyBrush;
            NoteHeaderBar.Background = palette.HeaderBrush;
            TxtActiveNoteTitle.Foreground = palette.ForegroundBrush;

            // En modo papelera el editor es de solo lectura
            ActiveNoteEditor.IsReadOnly = _isTrashMode;
            TxtActiveNoteTitle.IsReadOnly = _isTrashMode;
        }
    }

    #region Auto-ocultado y Hover

    private void PeekingHandle_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!_isExpanded)
        {
            SetExpanded(true);
        }
    }

    private void ExpandedPanel_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _autoHideTimer.Stop();
    }

    private void ExpandedPanel_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!_isPinned)
        {
            _autoHideTimer.Start();
        }
    }

    private void BtnPin_Click(object sender, RoutedEventArgs e)
    {
        _isPinned = BtnPin.IsChecked ?? false;
        if (_isPinned)
        {
            _autoHideTimer.Stop();
        }
    }

    private void BtnCollapse_Click(object sender, RoutedEventArgs e)
    {
        SetExpanded(false);
    }

    private void BtnToggleEdge_Click(object sender, RoutedEventArgs e)
    {
        _isRightEdge = !_isRightEdge;
        PositionToMonitorEdge();
    }

    #endregion

    #region Edición y Debounce de Nota

    private void TxtActiveNoteTitle_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_currentNote == null || _isTrashMode) return;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void ActiveNoteEditor_TextChanged(object sender, RoutedEventArgs e)
    {
        if (_currentNote == null || _isTrashMode) return;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private async void DebounceTimer_Tick(object? sender, object e)
    {
        _debounceTimer.Stop();
        if (_currentNote == null || _isTrashMode) return;

        _currentNote.Title = string.IsNullOrWhiteSpace(TxtActiveNoteTitle.Text) ? null : TxtActiveNoteTitle.Text.Trim();
        ActiveNoteEditor.Document.GetText(TextGetOptions.FormatRtf, out var rtf);
        _currentNote.Content = rtf;

        await _repository.UpdateAsync(_currentNote);

        var index = Notes.IndexOf(_currentNote);
        if (index >= 0)
        {
            Notes[index] = _currentNote;
            NotesTabList.SelectedIndex = index;
        }
    }

    #endregion

    #region Acciones de Papelera y Modos

    private async void BtnToggleTrash_Click(object sender, RoutedEventArgs e)
    {
        _isTrashMode = BtnToggleTrash.IsChecked ?? false;

        TxtHeaderTitle.Text = _isTrashMode ? "Papelera de Reciclaje" : "Notas Rápidas";
        BtnRestoreNote.Visibility = _isTrashMode ? Visibility.Visible : Visibility.Collapsed;
        BtnEmptyTrash.Visibility = _isTrashMode ? Visibility.Visible : Visibility.Collapsed;
        BtnNoteMenu.Visibility = _isTrashMode ? Visibility.Collapsed : Visibility.Visible;
        FormatBar.Visibility = _isTrashMode ? Visibility.Collapsed : Visibility.Visible;

        await LoadNotesAsync();
    }

    private async void BtnRestoreNote_Click(object sender, RoutedEventArgs e)
    {
        if (_currentNote == null) return;
        await _repository.RestoreFromTrashAsync(_currentNote.Id);
        _allLoadedNotes.Remove(_currentNote);
        Notes.Remove(_currentNote);
        _currentNote = Notes.FirstOrDefault();
        if (_currentNote != null)
        {
            NotesTabList.SelectedItem = _currentNote;
        }
    }

    private async void BtnEmptyTrash_Click(object sender, RoutedEventArgs e)
    {
        var trashNotes = await _repository.GetTrashNotesAsync();
        foreach (var note in trashNotes)
        {
            await _repository.PermanentDeleteAsync(note.Id);
        }
        await LoadNotesAsync();
    }

    #endregion

    #region Opciones de la Nota Activa (Menú)

    private async void SetColor_Click(object sender, RoutedEventArgs e)
    {
        if (_currentNote != null && sender is MenuFlyoutItem item && Enum.TryParse<NoteColor>(item.Tag?.ToString(), out var color))
        {
            _currentNote.Color = color;
            var palette = ColorHelper.GetPalette(color);
            ActiveNoteContainer.Background = palette.BodyBrush;
            NoteHeaderBar.Background = palette.HeaderBrush;
            TxtActiveNoteTitle.Foreground = palette.ForegroundBrush;

            await _repository.UpdateAsync(_currentNote);
        }
    }

    private async void BtnDeleteActiveNote_Click(object sender, RoutedEventArgs e)
    {
        if (_currentNote == null) return;
        await _repository.SoftDeleteAsync(_currentNote.Id);
        _allLoadedNotes.Remove(_currentNote);
        Notes.Remove(_currentNote);
        _currentNote = Notes.FirstOrDefault();
        if (_currentNote != null)
        {
            NotesTabList.SelectedItem = _currentNote;
        }
    }

    private async void BtnDuplicateSingleNote_Click(object sender, RoutedEventArgs e)
    {
        if (_currentNote == null) return;
        var duplicate = new Note
        {
            Title = _currentNote.Title != null ? $"{_currentNote.Title} (copia)" : "Copia de nota",
            Content = _currentNote.Content,
            Color = _currentNote.Color,
            PositionX = _currentNote.PositionX + 30,
            PositionY = _currentNote.PositionY + 30
        };

        var created = await _repository.CreateAsync(duplicate);
        _allLoadedNotes.Insert(0, created);
        Notes.Insert(0, created);
        NotesTabList.SelectedIndex = 0;
    }

    private void BtnFloatSingleNote_Click(object sender, RoutedEventArgs e)
    {
        if (_currentNote == null) return;
        AppManager.Instance.OpenNoteAsFloating(_currentNote);
    }

    private void BtnDetachAll_Click(object sender, RoutedEventArgs e)
    {
        AppManager.Instance.SwitchToFloatingMode();
        this.Close();
    }

    private async void BtnNewNote_Click(object sender, RoutedEventArgs e)
    {
        if (_isTrashMode)
        {
            BtnToggleTrash.IsChecked = false;
            await Task.Run(() => { });
            BtnToggleTrash_Click(BtnToggleTrash, new RoutedEventArgs());
        }

        var newNote = new Note
        {
            Title = "Nueva nota",
            Content = string.Empty,
            Color = NoteColor.Yellow
        };

        var created = await _repository.CreateAsync(newNote);
        _allLoadedNotes.Insert(0, created);
        Notes.Insert(0, created);
        NotesTabList.SelectedIndex = 0;
    }

    private async void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        var syncService = App.Services.GetRequiredService<GoogleDriveSyncService>();
        var syncScheduler = App.Services.GetRequiredService<SyncScheduler>();
        var dialog = new SettingsDialog(syncService, syncScheduler)
        {
            XamlRoot = this.Content.XamlRoot
        };
        await dialog.ShowAsync();
    }

    #endregion

    #region Exportación a Markdown y JSON

    private async void BtnExportActiveMarkdown_Click(object sender, RoutedEventArgs e)
    {
        if (_currentNote == null) return;
        var hWnd = WindowNative.GetWindowHandle(this);
        await ExportHelper.ExportNoteToMarkdownAsync(_currentNote, hWnd);
    }

    private async void BtnExportAllJson_Click(object sender, RoutedEventArgs e)
    {
        var hWnd = WindowNative.GetWindowHandle(this);
        await ExportHelper.ExportAllNotesToJsonAsync(_allLoadedNotes, hWnd);
    }

    #endregion

    #region Formato Rápido

    private void BtnBold_Click(object sender, RoutedEventArgs e) =>
        ActiveNoteEditor.Document.Selection.CharacterFormat.Bold = FormatEffect.Toggle;

    private void BtnItalic_Click(object sender, RoutedEventArgs e) =>
        ActiveNoteEditor.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;

    private void BtnUnderline_Click(object sender, RoutedEventArgs e) =>
        ActiveNoteEditor.Document.Selection.CharacterFormat.Underline =
            ActiveNoteEditor.Document.Selection.CharacterFormat.Underline == UnderlineType.None ? UnderlineType.Single : UnderlineType.None;

    private void BtnBullets_Click(object sender, RoutedEventArgs e) =>
        ActiveNoteEditor.Document.Selection.ParagraphFormat.ListType =
            ActiveNoteEditor.Document.Selection.ParagraphFormat.ListType == MarkerType.Bullet ? MarkerType.None : MarkerType.Bullet;

    #endregion
}