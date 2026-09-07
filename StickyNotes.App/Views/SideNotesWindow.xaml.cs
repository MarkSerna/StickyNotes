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
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
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
    private const int ExpandedWidth = 480;
    private const int CollapsedWidth = 16;
    private int _collapsedHeight = 84;

    private bool _isAnimating = false;
    private bool _animExpanding = false;
    private int _animStartX;
    private int _animTargetX;
    private int _animY;
    private double _animDurationMs;
    private readonly System.Diagnostics.Stopwatch _animStopwatch = new();

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

        // Timer de auto-ocultado al retirar el mouse (350ms de gracia)
        _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
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

    private void PositionToCollapsedEdge()
    {
        var workArea = MonitorHelper.GetPrimaryMonitorWorkArea();
        var y = workArea.Y + (workArea.Height - _collapsedHeight) / 2;
        var x = _isRightEdge ? (workArea.X + workArea.Width - CollapsedWidth) : workArea.X;
        _appWindow.MoveAndResize(new RectInt32(x, y, CollapsedWidth, _collapsedHeight));
    }

    private void PositionToMonitorEdge()
    {
        if (!_isExpanded)
        {
            PositionToCollapsedEdge();
            return;
        }

        var workArea = MonitorHelper.GetPrimaryMonitorWorkArea();
        var height = workArea.Height;
        var y = workArea.Y;
        var x = _isRightEdge ? (workArea.X + workArea.Width - ExpandedWidth) : workArea.X;
        _appWindow.MoveAndResize(new RectInt32(x, y, ExpandedWidth, height));
    }

    private void SetExpanded(bool expand, bool animate = true)
    {
        if (_isAnimating)
        {
            CompositionTarget.Rendering -= OnCompositionRendering;
            _isAnimating = false;
        }

        var workArea = MonitorHelper.GetPrimaryMonitorWorkArea();
        var fullHeight = workArea.Height;
        var y = workArea.Y;
        _animY = y;

        var closedX = _isRightEdge 
            ? (workArea.X + workArea.Width - CollapsedWidth) 
            : (workArea.X - ExpandedWidth + CollapsedWidth);
        var openedX = _isRightEdge 
            ? (workArea.X + workArea.Width - ExpandedWidth) 
            : workArea.X;

        if (!animate)
        {
            _isExpanded = expand;
            ExpandedPanel.Visibility = expand ? Visibility.Visible : Visibility.Collapsed;
            PeekingHandle.Visibility = expand ? Visibility.Collapsed : Visibility.Visible;

            if (expand)
            {
                _appWindow.MoveAndResize(new RectInt32(openedX, y, ExpandedWidth, fullHeight));
            }
            else
            {
                PositionToCollapsedEdge();
            }
            return;
        }

        _animExpanding = expand;
        _isExpanded = expand;

        if (expand)
        {
            ExpandedPanel.Visibility = Visibility.Visible;
            PeekingHandle.Visibility = Visibility.Collapsed;

            _animStartX = _appWindow.Position.X;
            // Asegurar que la ventana tenga el tamaño completo y esté en la posición de entrada inicial
            if (_animStartX == 0 || _appWindow.Size.Width != ExpandedWidth || _appWindow.Size.Height != fullHeight)
            {
                _animStartX = closedX;
                _appWindow.MoveAndResize(new RectInt32(closedX, y, ExpandedWidth, fullHeight));
            }
            _animTargetX = openedX;
            _animDurationMs = 280.0;
        }
        else
        {
            _animStartX = _appWindow.Position.X;
            _animTargetX = closedX;
            _animDurationMs = 220.0;
        }

        _animStopwatch.Restart();
        _isAnimating = true;
        CompositionTarget.Rendering += OnCompositionRendering;
    }

    private void OnCompositionRendering(object? sender, object e)
    {
        if (!_isAnimating) return;

        var elapsed = _animStopwatch.Elapsed.TotalMilliseconds;
        var t = Math.Min(1.0, elapsed / _animDurationMs);

        // Curvas Fluent Design:
        // Apertura: Quartic Ease-Out (arranque vivo y desaceleración sedosa)
        // Cierre: Cubic Ease-In (aceleración limpia de salida)
        var progress = _animExpanding
            ? 1.0 - Math.Pow(1.0 - t, 4)
            : Math.Pow(t, 3);

        var currentX = (int)Math.Round(_animStartX + (_animTargetX - _animStartX) * progress);
        _appWindow.Move(new PointInt32(currentX, _animY));

        if (t >= 1.0)
        {
            CompositionTarget.Rendering -= OnCompositionRendering;
            _isAnimating = false;

            if (!_animExpanding)
            {
                _isExpanded = false;
                ExpandedPanel.Visibility = Visibility.Collapsed;
                PeekingHandle.Visibility = Visibility.Visible;
                PositionToCollapsedEdge();
            }
        }
    }

    private void UpdatePeekingPills()
    {
        PeekingPillsStack.Children.Clear();
        ExpandedPillsStack.Children.Clear();

        var displayNotes = _allLoadedNotes.Take(6).ToList();
        foreach (var note in displayNotes)
        {
            var palette = ColorHelper.GetPalette(note.Color);

            var pill1 = new Border
            {
                Width = 4,
                Height = 16,
                CornerRadius = new CornerRadius(2),
                Background = palette.HeaderBrush,
                Margin = new Thickness(0, 1.5, 0, 1.5)
            };
            PeekingPillsStack.Children.Add(pill1);

            var pill2 = new Border
            {
                Width = 4,
                Height = 16,
                CornerRadius = new CornerRadius(2),
                Background = palette.HeaderBrush,
                Margin = new Thickness(0, 1.5, 0, 1.5)
            };
            ExpandedPillsStack.Children.Add(pill2);
        }

        var count = Math.Max(2, displayNotes.Count);
        _collapsedHeight = Math.Clamp(count * 20 + 18, 56, 160);
        PeekingHandle.Height = _collapsedHeight;

        if (!_isExpanded && !_animExpanding)
        {
            PositionToCollapsedEdge();
        }
    }

    private async Task LoadNotesAsync()
    {
        var items = _isTrashMode 
            ? await _repository.GetTrashNotesAsync() 
            : await _repository.GetActiveNotesAsync();

        _allLoadedNotes.Clear();
        _allLoadedNotes.AddRange(items);

        UpdatePeekingPills();
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

            ApplyActiveNoteColor(selectedNote.Color);

            // Graduar tamaño vertical predeterminado
            var targetHeight = selectedNote.Height > 100 ? selectedNote.Height : 340.0;
            ActiveNoteCard.VerticalAlignment = VerticalAlignment.Top;
            ActiveNoteCard.Height = Math.Min(targetHeight, Math.Max(260, ExpandedPanel.ActualHeight > 200 ? ExpandedPanel.ActualHeight - 24 : 700));

            // Estado de sincronización
            IconActiveSync.Glyph = selectedNote.SyncStatus == SyncStatus.Synced ? "\uE753" : "\uE898";

            // En modo papelera el editor es de solo lectura
            ActiveNoteEditor.IsReadOnly = _isTrashMode;
            TxtActiveNoteTitle.IsReadOnly = _isTrashMode;
        }
    }

    private void ApplyActiveNoteColor(NoteColor color)
    {
        var palette = ColorHelper.GetPalette(color);
        ActiveNoteCard.Background = palette.BodyBrush;
        ActiveNoteCard.BorderBrush = palette.BorderBrush;
        NoteHeaderBar.Background = palette.HeaderBrush;
        ActiveNoteFooter.BorderBrush = palette.BorderBrush;

        TxtActiveNoteTitle.Foreground = palette.ForegroundBrush;
        ActiveNoteEditor.Foreground = palette.ForegroundBrush;
        ActiveNoteEditor.Background = palette.BodyBrush;
        TxtActiveAutoSave.Foreground = palette.ForegroundBrush;

        try
        {
            var sel = ActiveNoteEditor.Document.Selection;
            ActiveNoteEditor.Document.GetText(TextGetOptions.None, out var allText);
            if (!string.IsNullOrEmpty(allText))
            {
                sel.SetRange(0, allText.Length);
                sel.CharacterFormat.ForegroundColor = ((SolidColorBrush)palette.ForegroundBrush).Color;
                sel.CharacterFormat.BackgroundColor = Colors.Transparent;
                sel.SetRange(allText.Length, allText.Length);
            }
            else
            {
                sel.CharacterFormat.ForegroundColor = ((SolidColorBrush)palette.ForegroundBrush).Color;
                sel.CharacterFormat.BackgroundColor = Colors.Transparent;
            }
        }
        catch { }

        IconActiveSync.Foreground = palette.ForegroundBrush;
        IconActiveMore.Foreground = palette.ForegroundBrush;
        IconActiveDelete.Foreground = palette.ForegroundBrush;

        IconActiveBold.Foreground = palette.ForegroundBrush;
        IconActiveItalic.Foreground = palette.ForegroundBrush;
        IconActiveUnderline.Foreground = palette.ForegroundBrush;
        IconActiveStrikethrough.Foreground = palette.ForegroundBrush;
        IconActiveChecklist.Foreground = palette.ForegroundBrush;
    }

    #region Auto-ocultado y Hover

    private void PeekingHandle_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (!_isExpanded)
        {
            SetExpanded(true);
        }
    }

    private void PeekingHandle_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!_isExpanded)
        {
            SetExpanded(true);
        }
    }

    private void ExpandedPanel_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _autoHideTimer.Stop();
    }

    private void ExpandedPanel_PointerExited(object sender, PointerRoutedEventArgs e)
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
        SetExpanded(_isExpanded, animate: false);
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
        // Nota: Al implementar INotifyPropertyChanged en Note, los cambios de título y
        // previsualización se propagan automáticamente al ListView sin parpadeos ni recreación de contenedores.
    }

    #endregion

    #region Acciones de Papelera y Modos

    private async void BtnToggleTrash_Click(object sender, RoutedEventArgs e)
    {
        _isTrashMode = BtnToggleTrash.IsChecked ?? false;
        await LoadNotesAsync();
    }

    #endregion

    #region Opciones de la Nota Activa (Menú)

    private async void SetColor_Click(object sender, RoutedEventArgs e)
    {
        if (_currentNote != null && sender is FrameworkElement fe && Enum.TryParse<NoteColor>(fe.Tag?.ToString(), out var color))
        {
            _currentNote.Color = color;
            ApplyActiveNoteColor(color);
            UpdatePeekingPills();

            await _repository.UpdateAsync(_currentNote);
        }
    }

    private async void BtnDeleteActiveNote_Click(object sender, RoutedEventArgs e)
    {
        if (_currentNote == null) return;
        await _repository.SoftDeleteAsync(_currentNote.Id);
        _allLoadedNotes.Remove(_currentNote);
        Notes.Remove(_currentNote);
        UpdatePeekingPills();

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
        UpdatePeekingPills();
        NotesTabList.SelectedIndex = 0;
    }

    private void BtnFloatSingleNote_Click(object sender, RoutedEventArgs e)
    {
        if (_currentNote == null) return;
        AppManager.Instance.OpenNoteAsFloating(_currentNote);
    }

    private async void BtnNewNote_Click(object sender, RoutedEventArgs e)
    {
        if (_isTrashMode)
        {
            BtnToggleTrash.IsChecked = false;
            _isTrashMode = false;
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
        UpdatePeekingPills();
        NotesTabList.SelectedIndex = 0;
    }

    private async void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        var syncService = App.Services.GetRequiredService<GoogleDriveSyncService>();
        var syncScheduler = App.Services.GetRequiredService<SyncScheduler>();
        var hWnd = WindowNative.GetWindowHandle(this);
        var dialog = new SettingsDialog(syncService, syncScheduler, hWnd)
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

    #endregion

    #region Formato Rápido

    private void BtnBold_Click(object sender, RoutedEventArgs e) =>
        ActiveNoteEditor.Document.Selection.CharacterFormat.Bold = FormatEffect.Toggle;

    private void BtnItalic_Click(object sender, RoutedEventArgs e) =>
        ActiveNoteEditor.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;

    private void BtnUnderline_Click(object sender, RoutedEventArgs e) =>
        ActiveNoteEditor.Document.Selection.CharacterFormat.Underline =
            ActiveNoteEditor.Document.Selection.CharacterFormat.Underline == UnderlineType.None ? UnderlineType.Single : UnderlineType.None;

    private void BtnStrikethrough_Click(object sender, RoutedEventArgs e) =>
        ActiveNoteEditor.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.Toggle;

    private void BtnChecklist_Click(object sender, RoutedEventArgs e) =>
        ActiveNoteEditor.Document.Selection.TypeText("☑ ");

    #endregion

    #region Redimensionamiento y Graduación de Tamaño de Nota

    private bool _isResizingNote = false;
    private double _resizeStartY;
    private double _resizeStartHeight;

    private void ResizeGrip_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isResizingNote = true;
        var pt = e.GetCurrentPoint(ExpandedPanel);
        _resizeStartY = pt.Position.Y;
        _resizeStartHeight = ActiveNoteCard.ActualHeight > 0 ? ActiveNoteCard.ActualHeight : (ActiveNoteCard.Height > 0 ? ActiveNoteCard.Height : 340);
        (sender as UIElement)?.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void ResizeGrip_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isResizingNote) return;

        var pt = e.GetCurrentPoint(ExpandedPanel);
        var deltaY = pt.Position.Y - _resizeStartY;
        var maxHeight = Math.Max(260, ExpandedPanel.ActualHeight > 200 ? ExpandedPanel.ActualHeight - 24 : 800);
        var newHeight = Math.Clamp(_resizeStartHeight + deltaY, 180, maxHeight);

        ActiveNoteCard.VerticalAlignment = VerticalAlignment.Top;
        ActiveNoteCard.Height = newHeight;
        if (_currentNote != null)
        {
            _currentNote.Height = newHeight;
        }
        e.Handled = true;
    }

    private async void ResizeGrip_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isResizingNote)
        {
            _isResizingNote = false;
            (sender as UIElement)?.ReleasePointerCapture(e.Pointer);
            e.Handled = true;

            if (_currentNote != null)
            {
                await _repository.UpdateAsync(_currentNote);
            }
        }
    }

    private async void SetNoteSize_Click(object sender, RoutedEventArgs e)
    {
        if (_currentNote == null || sender is not FrameworkElement fe) return;
        if (double.TryParse(fe.Tag?.ToString(), out var targetSize))
        {
            if (targetSize <= 0)
            {
                // Pantalla completa
                ActiveNoteCard.VerticalAlignment = VerticalAlignment.Stretch;
                ActiveNoteCard.Height = double.NaN;
                _currentNote.Height = Math.Max(600, ExpandedPanel.ActualHeight - 24);
            }
            else
            {
                ActiveNoteCard.VerticalAlignment = VerticalAlignment.Top;
                ActiveNoteCard.Height = targetSize;
                _currentNote.Height = targetSize;
            }

            await _repository.UpdateAsync(_currentNote);
        }
    }

    #endregion
}