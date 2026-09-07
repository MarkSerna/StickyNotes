using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
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

    private RectInt32? _currentWorkArea = null;
    private int? _customCollapsedY = null;

    private bool _isPointerDownOnHandle = false;
    private bool _isDraggingHandle = false;
    private (int X, int Y) _dragStartCursor;
    private int _dragStartWindowX;
    private int _dragStartWindowY;
    private readonly DispatcherTimer _hoverExpandTimer;

    private bool _isAnimating = false;
    private bool _animExpanding = false;
    private int _animStartWidth;
    private int _animTargetWidth;
    private int _animY;
    private int _animHeight;
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

        // Cargar configuración de posición previa guardada
        LoadWidgetConfig();

        // Timer de auto-expansión al pasar el mouse por el tirador (220ms de gracia para no interferir con arrastre)
        _hoverExpandTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(220) };
        _hoverExpandTimer.Tick += (s, e) =>
        {
            _hoverExpandTimer.Stop();
            if (!_isExpanded && !_isDraggingHandle && !_isPointerDownOnHandle)
            {
                SetExpanded(true);
            }
        };

        // Aplicar simetría visual y ajustar al monitor configurado
        ApplyEdgeVisuals();
        PositionToMonitorEdge();

        // Timer de auto-ocultado al retirar el mouse (350ms de gracia)
        _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _autoHideTimer.Tick += (s, e) =>
        {
            _autoHideTimer.Stop();
            if (!_isPinned && _isExpanded && !_isDraggingHandle)
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

    private RectInt32 GetActiveWorkArea()
    {
        if (_currentWorkArea.HasValue)
        {
            return _currentWorkArea.Value;
        }

        var hWnd = WindowNative.GetWindowHandle(this);
        return MonitorHelper.GetMonitorWorkAreaFromWindow(hWnd);
    }

    private void PositionToCollapsedEdge()
    {
        var workArea = GetActiveWorkArea();
        var y = _customCollapsedY.HasValue 
            ? Math.Clamp(_customCollapsedY.Value, workArea.Y + 10, workArea.Y + workArea.Height - _collapsedHeight - 10)
            : workArea.Y + (workArea.Height - _collapsedHeight) / 2;

        var x = _isRightEdge 
            ? (workArea.X + workArea.Width - CollapsedWidth) 
            : workArea.X;

        _appWindow.MoveAndResize(new RectInt32(x, y, CollapsedWidth, _collapsedHeight));
    }

    private void PositionToMonitorEdge()
    {
        if (!_isExpanded)
        {
            PositionToCollapsedEdge();
            return;
        }

        var workArea = GetActiveWorkArea();
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

        var workArea = GetActiveWorkArea();
        var fullHeight = workArea.Height;
        var y = workArea.Y;
        _animY = y;
        _animHeight = fullHeight;

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

            _animStartWidth = _appWindow.Size.Width;
            if (_animStartWidth <= CollapsedWidth || _appWindow.Size.Height != fullHeight)
            {
                _animStartWidth = CollapsedWidth;
                var startX = _isRightEdge ? (workArea.X + workArea.Width - CollapsedWidth) : workArea.X;
                _appWindow.MoveAndResize(new RectInt32(startX, y, CollapsedWidth, fullHeight));
            }
            _animTargetWidth = ExpandedWidth;
            _animDurationMs = 280.0;
        }
        else
        {
            _animStartWidth = _appWindow.Size.Width;
            _animTargetWidth = CollapsedWidth;
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

        var currentWidth = (int)Math.Round(_animStartWidth + (_animTargetWidth - _animStartWidth) * progress);
        currentWidth = Math.Clamp(currentWidth, CollapsedWidth, ExpandedWidth);

        var workArea = GetActiveWorkArea();
        var currentX = _isRightEdge 
            ? (workArea.X + workArea.Width - currentWidth) 
            : workArea.X;

        _appWindow.MoveAndResize(new RectInt32(currentX, _animY, currentWidth, _animHeight));

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
            else
            {
                var finalX = _isRightEdge ? (workArea.X + workArea.Width - ExpandedWidth) : workArea.X;
                _appWindow.MoveAndResize(new RectInt32(finalX, _animY, ExpandedWidth, _animHeight));
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

    #region Auto-ocultado, Hover y Arrastre (Drag & Drop) entre bordes y pantallas

    private void PeekingHandle_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (!_isExpanded && !_isDraggingHandle)
        {
            _hoverExpandTimer.Start();
        }
    }

    private void PeekingHandle_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _hoverExpandTimer.Stop();
    }

    private void PeekingHandle_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _hoverExpandTimer.Stop();
        _isPointerDownOnHandle = true;
        _isDraggingHandle = false;
        _dragStartCursor = MonitorHelper.GetCursorPosition();
        _dragStartWindowX = _appWindow.Position.X;
        _dragStartWindowY = _appWindow.Position.Y;
        PeekingHandle.CapturePointer(e.Pointer);
    }

    private void PeekingHandle_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isPointerDownOnHandle) return;

        var currentCursor = MonitorHelper.GetCursorPosition();
        var dx = currentCursor.X - _dragStartCursor.X;
        var dy = currentCursor.Y - _dragStartCursor.Y;

        if (!_isDraggingHandle && (Math.Abs(dx) > 6 || Math.Abs(dy) > 6))
        {
            _isDraggingHandle = true;
            PeekingHandle.Opacity = 0.85;
            PeekingHandle.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
        }

        if (_isDraggingHandle)
        {
            // Desplazar la ventana libremente por cualquier pantalla acompañando al cursor
            var newX = _dragStartWindowX + dx;
            var newY = _dragStartWindowY + dy;
            _appWindow.Move(new PointInt32(newX, newY));
        }
    }

    private void PeekingHandle_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isPointerDownOnHandle) return;
        _isPointerDownOnHandle = false;
        PeekingHandle.ReleasePointerCapture(e.Pointer);

        PeekingHandle.Opacity = 1.0;
        PeekingHandle.BorderBrush = new SolidColorBrush(ColorHelper.FromHex("#334155"));

        if (_isDraggingHandle)
        {
            _isDraggingHandle = false;

            // Detectar en qué monitor y en qué borde se soltó
            var cursor = MonitorHelper.GetCursorPosition();
            var targetWorkArea = MonitorHelper.GetMonitorWorkAreaFromPoint(cursor.X, cursor.Y);

            // Determinar si está más cerca del borde izquierdo o derecho
            bool snapToRight = (cursor.X - targetWorkArea.X) >= (targetWorkArea.Width / 2);

            _currentWorkArea = targetWorkArea;
            _isRightEdge = snapToRight;
            _customCollapsedY = Math.Clamp(cursor.Y - _collapsedHeight / 2, targetWorkArea.Y + 20, targetWorkArea.Y + targetWorkArea.Height - _collapsedHeight - 20);

            ApplyEdgeVisuals();
            PositionToCollapsedEdge();
            SaveWidgetConfig();
        }
        else
        {
            // Clic simple: abrir o cerrar
            SetExpanded(!_isExpanded);
        }
    }

    private void ExpandedPanel_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _autoHideTimer.Stop();
    }

    private void ExpandedPanel_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!_isPinned && !_isDraggingHandle)
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

    private void PositionFlyout_Opening(object? sender, object e)
    {
        if (MenuToggleEdge != null)
        {
            MenuToggleEdge.Text = _isRightEdge ? "Mover al borde izquierdo" : "Mover al borde derecho";
        }

        var monitors = MonitorHelper.GetAllMonitors();
        if (monitors.Count > 1)
        {
            MenuMonitorsSeparator.Visibility = Visibility.Visible;
            MenuMonitorsSubItem.Visibility = Visibility.Visible;
            MenuMonitorsSubItem.Items.Clear();

            var activeWorkArea = GetActiveWorkArea();

            foreach (var mon in monitors)
            {
                var isCurrent = mon.WorkArea.X == activeWorkArea.X && mon.WorkArea.Y == activeWorkArea.Y;
                var item = new MenuFlyoutItem
                {
                    Text = $"Pantalla {mon.Index} ({mon.Bounds.Width}x{mon.Bounds.Height})" + (isCurrent ? " (Actual)" : (mon.IsPrimary ? " (Principal)" : ""))
                };
                item.Click += (s, ev) =>
                {
                    MoveToMonitor(mon.WorkArea);
                };
                MenuMonitorsSubItem.Items.Add(item);
            }
        }
        else
        {
            MenuMonitorsSeparator.Visibility = Visibility.Collapsed;
            MenuMonitorsSubItem.Visibility = Visibility.Collapsed;
        }
    }

    private void MenuToggleEdge_Click(object sender, RoutedEventArgs e)
    {
        _isRightEdge = !_isRightEdge;
        ApplyEdgeVisuals();
        SaveWidgetConfig();
        PositionToMonitorEdge();
    }

    private void MoveToMonitor(RectInt32 targetWorkArea)
    {
        _currentWorkArea = targetWorkArea;
        SaveWidgetConfig();
        PositionToMonitorEdge();
    }

    private void ApplyEdgeVisuals()
    {
        if (_isRightEdge)
        {
            // Tirador colapsado: a la derecha, con bordes redondeados hacia la izquierda
            PeekingHandle.HorizontalAlignment = HorizontalAlignment.Right;
            PeekingHandle.CornerRadius = new CornerRadius(8, 0, 0, 8);
            PeekingHandle.BorderThickness = new Thickness(1, 1, 0, 1);

            // Panel expandido: anclado a la izquierda dentro del ancho animado para guiar la entrada desde el borde
            ExpandedPanel.HorizontalAlignment = HorizontalAlignment.Left;
            ExpandedPanel.BorderThickness = new Thickness(1, 0, 0, 0);

            Col0.Width = new GridLength(18);
            Col1.Width = new GridLength(1, GridUnitType.Star);
            Col2.Width = new GridLength(140);

            Grid.SetColumn(ExpandedPillsBorder, 0);
            ExpandedPillsBorder.BorderThickness = new Thickness(0, 0, 1, 0);

            Grid.SetColumn(ActiveNoteContainer, 1);

            Grid.SetColumn(NotesSidebarContainer, 2);
            NotesSidebarContainer.BorderThickness = new Thickness(1, 0, 0, 0);

            if (MenuToggleEdge != null)
            {
                MenuToggleEdge.Text = "Mover al borde izquierdo";
            }
        }
        else
        {
            // Tirador colapsado: a la izquierda, con bordes redondeados hacia la derecha
            PeekingHandle.HorizontalAlignment = HorizontalAlignment.Left;
            PeekingHandle.CornerRadius = new CornerRadius(0, 8, 8, 0);
            PeekingHandle.BorderThickness = new Thickness(0, 1, 1, 1);

            // Panel expandido: anclado a la derecha dentro del ancho animado para guiar la entrada desde el borde
            ExpandedPanel.HorizontalAlignment = HorizontalAlignment.Right;
            ExpandedPanel.BorderThickness = new Thickness(0, 0, 1, 0);

            Col0.Width = new GridLength(140);
            Col1.Width = new GridLength(1, GridUnitType.Star);
            Col2.Width = new GridLength(18);

            Grid.SetColumn(NotesSidebarContainer, 0);
            NotesSidebarContainer.BorderThickness = new Thickness(0, 0, 1, 0);

            Grid.SetColumn(ActiveNoteContainer, 1);

            Grid.SetColumn(ExpandedPillsBorder, 2);
            ExpandedPillsBorder.BorderThickness = new Thickness(1, 0, 0, 0);

            if (MenuToggleEdge != null)
            {
                MenuToggleEdge.Text = "Mover al borde derecho";
            }
        }
    }

    private void SaveWidgetConfig()
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folder = Path.Combine(appData, "StickyNotesApp");
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, "widget_position.json");

            var config = new
            {
                IsRightEdge = _isRightEdge,
                CustomY = _customCollapsedY,
                MonitorX = _currentWorkArea?.X,
                MonitorY = _currentWorkArea?.Y
            };
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(file, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SideNotes] Error al guardar config: {ex.Message}");
        }
    }

    private void LoadWidgetConfig()
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var file = Path.Combine(appData, "StickyNotesApp", "widget_position.json");
            if (File.Exists(file))
            {
                var json = File.ReadAllText(file);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("IsRightEdge", out var edgeProp))
                {
                    _isRightEdge = edgeProp.GetBoolean();
                }
                if (root.TryGetProperty("CustomY", out var yProp) && yProp.ValueKind == JsonValueKind.Number)
                {
                    _customCollapsedY = yProp.GetInt32();
                }
                if (root.TryGetProperty("MonitorX", out var mxProp) && mxProp.ValueKind == JsonValueKind.Number &&
                    root.TryGetProperty("MonitorY", out var myProp) && myProp.ValueKind == JsonValueKind.Number)
                {
                    _currentWorkArea = MonitorHelper.GetMonitorWorkAreaFromPoint(mxProp.GetInt32() + 50, myProp.GetInt32() + 50);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SideNotes] Error al cargar config: {ex.Message}");
        }
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