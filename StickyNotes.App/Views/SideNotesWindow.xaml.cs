using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        if (File.Exists(iconPath))
        {
            _appWindow.SetIcon(iconPath);
        }

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

        // Timer de auto-ocultado al retirar el mouse (500ms de gracia con verificación física)
        _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _autoHideTimer.Tick += (s, e) =>
        {
            _autoHideTimer.Stop();
            if (_isPinned || !_isExpanded || _isDraggingHandle) return;

            // No cerrar si el cursor físico sigue dentro del área de la ventana
            if (IsCursorOverWindow()) return;

            // No cerrar si la ventana tiene el foco activo (ej. escribiendo o editando)
            var activeHWnd = GetForegroundWindow();
            var myHWnd = WindowNative.GetWindowHandle(this);
            if (activeHWnd == myHWnd && activeHWnd != IntPtr.Zero) return;

            SetExpanded(false);
        };

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
        NotesCardsStack.Children.Clear();
        var filter = query?.Trim();

        var matched = string.IsNullOrWhiteSpace(filter)
            ? _allLoadedNotes
            : _allLoadedNotes.Where(n =>
                (!string.IsNullOrEmpty(n.Title) && n.Title.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(n.Content) && n.Content.Contains(filter, StringComparison.OrdinalIgnoreCase)));

        foreach (var note in matched)
        {
            Notes.Add(note);
            var card = CreateNoteCardControl(note);
            NotesCardsStack.Children.Add(card);
        }

        NotesTabList.ItemsSource = Notes;
        if (Notes.Any())
        {
            NotesTabList.SelectedIndex = 0;
        }
        else
        {
            _currentNote = null;
        }
    }

    private NoteCardControl CreateNoteCardControl(Note note)
    {
        var card = new NoteCardControl();
        card.Initialize(note, _repository, _isTrashMode);

        card.CardFocused += (s, n) =>
        {
            _currentNote = n;
            if (NotesTabList.SelectedItem != n)
            {
                NotesTabList.SelectedItem = n;
            }
            HighlightSelectedCard(n.Id);
        };

        card.DeleteRequested += async (s, n) =>
        {
            await HandleDeleteNoteAsync(n);
        };

        card.DuplicateRequested += async (s, n) =>
        {
            await HandleDuplicateNoteAsync(n);
        };

        card.FloatRequested += (s, n) =>
        {
            AppManager.Instance.OpenNoteAsFloating(n);
        };

        card.NoteUpdated += (s, n) =>
        {
            UpdatePeekingPills();
        };

        return card;
    }

    private void HighlightSelectedCard(Guid noteId)
    {
        foreach (var child in NotesCardsStack.Children.OfType<NoteCardControl>())
        {
            if (child.Note?.Id == noteId)
            {
                child.HighlightCard();
            }
            else
            {
                child.UnhighlightCard();
            }
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
            HighlightSelectedCard(selectedNote.Id);

            var targetCard = NotesCardsStack.Children.OfType<NoteCardControl>()
                .FirstOrDefault(c => c.Note?.Id == selectedNote.Id);

            if (targetCard != null)
            {
                targetCard.StartBringIntoView();
                targetCard.FocusEditor();
            }
        }
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

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    private bool IsCursorOverWindow()
    {
        try
        {
            var (cursorX, cursorY) = MonitorHelper.GetCursorPosition();
            var pos = _appWindow.Position;
            var size = _appWindow.Size;

            return cursorX >= (pos.X - 6) && cursorX <= (pos.X + size.Width + 6) &&
                   cursorY >= (pos.Y - 6) && cursorY <= (pos.Y + size.Height + 6);
        }
        catch
        {
            return false;
        }
    }

    private void ExpandedPanel_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _autoHideTimer.Stop();
    }

    private void ExpandedPanel_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_autoHideTimer.IsEnabled)
        {
            _autoHideTimer.Stop();
        }
    }

    private void ExpandedPanel_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!_isPinned && !_isDraggingHandle && !IsCursorOverWindow())
        {
            _autoHideTimer.Stop();
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

            Grid.SetColumn(NotesScrollViewer, 1);

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

            Grid.SetColumn(NotesScrollViewer, 1);

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

    #region Acciones de Papelera y Modos

    private async void BtnToggleTrash_Click(object sender, RoutedEventArgs e)
    {
        _isTrashMode = BtnToggleTrash.IsChecked ?? false;
        await LoadNotesAsync();
    }

    #endregion

    #region Gestión y Operaciones de Notas Apiladas

    private async void BtnNewNote_Click(object sender, RoutedEventArgs e)
    {
        if (_isTrashMode)
        {
            BtnToggleTrash.IsChecked = false;
            _isTrashMode = false;
        }

        var newNote = new Note
        {
            Title = null,
            Content = string.Empty,
            Color = NoteColor.Yellow,
            Height = 200.0
        };

        var created = await _repository.CreateAsync(newNote);
        _allLoadedNotes.Insert(0, created);
        Notes.Insert(0, created);
        UpdatePeekingPills();

        var card = CreateNoteCardControl(created);
        NotesCardsStack.Children.Insert(0, card);

        NotesTabList.SelectedIndex = 0;
        _currentNote = created;
        HighlightSelectedCard(created.Id);
        card.StartBringIntoView();
        card.FocusEditor();
    }

    private async Task HandleDeleteNoteAsync(Note note)
    {
        await _repository.SoftDeleteAsync(note.Id);
        _allLoadedNotes.Remove(note);
        Notes.Remove(note);

        var cardToRemove = NotesCardsStack.Children.OfType<NoteCardControl>()
            .FirstOrDefault(c => c.Note?.Id == note.Id);
        if (cardToRemove != null)
        {
            NotesCardsStack.Children.Remove(cardToRemove);
        }

        UpdatePeekingPills();

        _currentNote = Notes.FirstOrDefault();
        if (_currentNote != null)
        {
            NotesTabList.SelectedItem = _currentNote;
            HighlightSelectedCard(_currentNote.Id);
        }
    }

    private async Task HandleDuplicateNoteAsync(Note note)
    {
        var duplicate = new Note
        {
            Title = note.Title != null ? $"{note.Title} (copia)" : "Copia de nota",
            Content = note.Content,
            Color = note.Color,
            PositionX = note.PositionX + 30,
            PositionY = note.PositionY + 30,
            Height = note.Height
        };

        var created = await _repository.CreateAsync(duplicate);
        _allLoadedNotes.Insert(0, created);
        Notes.Insert(0, created);
        UpdatePeekingPills();

        var card = CreateNoteCardControl(created);
        NotesCardsStack.Children.Insert(0, card);

        NotesTabList.SelectedIndex = 0;
        _currentNote = created;
        HighlightSelectedCard(created.Id);
        card.StartBringIntoView();
        card.FocusEditor();
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
}