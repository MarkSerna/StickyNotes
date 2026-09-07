using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
    private const int ExpandedWidth = 400;
    private const int CollapsedWidth = 14;

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
        var activeNotes = await _repository.GetActiveNotesAsync();
        Notes.Clear();
        foreach (var n in activeNotes)
        {
            Notes.Add(n);
        }

        NotesTabList.ItemsSource = Notes;
        if (Notes.Any())
        {
            NotesTabList.SelectedIndex = 0;
        }
    }

    private void NotesTabList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NotesTabList.SelectedItem is Note selectedNote)
        {
            _currentNote = selectedNote;
            TxtActiveNoteTitle.Text = selectedNote.Title ?? string.Empty;

            try
            {
                ActiveNoteEditor.Document.SetText(TextSetOptions.FormatRtf, selectedNote.Content);
            }
            catch
            {
                ActiveNoteEditor.Document.SetText(TextSetOptions.None, selectedNote.Content);
            }

            // Nota: la limpieza de fondos se aplica más abajo después de obtener la paleta

            var palette = ColorHelper.GetPalette(selectedNote.Color);
            ActiveNoteContainer.Background = palette.BodyBrush;
            NoteHeaderBar.Background = palette.HeaderBrush;
            TxtActiveNoteTitle.Foreground = palette.ForegroundBrush;
        }
    }

    #region Auto-ocultado y Hover

    private void PeekingHandle_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _autoHideTimer.Stop();
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
        if (_currentNote == null) return;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void ActiveNoteEditor_TextChanged(object sender, RoutedEventArgs e)
    {
        if (_currentNote == null) return;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private async void DebounceTimer_Tick(object? sender, object e)
    {
        _debounceTimer.Stop();
        if (_currentNote == null) return;

        _currentNote.Title = string.IsNullOrWhiteSpace(TxtActiveNoteTitle.Text) ? null : TxtActiveNoteTitle.Text.Trim();
        ActiveNoteEditor.Document.GetText(TextGetOptions.FormatRtf, out var rtf);
        _currentNote.Content = rtf;

        await _repository.UpdateAsync(_currentNote);

        // Refresca la vista de la pestaña seleccionada
        var index = Notes.IndexOf(_currentNote);
        if (index >= 0)
        {
            Notes[index] = _currentNote;
            NotesTabList.SelectedIndex = index;
        }
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
            try
            {
                var sel = ActiveNoteEditor.Document.Selection;
                sel.SetRange(0, 0);
                sel.Expand(TextRangeUnit.Story);
                sel.CharacterFormat.BackgroundColor = ((Microsoft.UI.Xaml.Media.SolidColorBrush)palette.BodyBrush).Color;
                sel.CharacterFormat.ForegroundColor = ((Microsoft.UI.Xaml.Media.SolidColorBrush)palette.ForegroundBrush).Color;
                sel.SetRange(0, 0);
            }
            catch { }

            await _repository.UpdateAsync(_currentNote);
        }
    }

    private async void BtnDeleteActiveNote_Click(object sender, RoutedEventArgs e)
    {
        if (_currentNote == null) return;
        await _repository.SoftDeleteAsync(_currentNote.Id);
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
        var newNote = new Note
        {
            Title = "Nueva nota",
            Content = string.Empty,
            Color = NoteColor.Yellow
        };

        var created = await _repository.CreateAsync(newNote);
        Notes.Insert(0, created);
        NotesTabList.SelectedIndex = 0;
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