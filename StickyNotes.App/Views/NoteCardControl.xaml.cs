using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using StickyNotes.App.Helpers;
using ColorHelper = StickyNotes.App.Helpers.ColorHelper;
using StickyNotes.App.Services;
using StickyNotes.Core.Enums;
using StickyNotes.Core.Models;
using StickyNotes.Data.Repositories;
using Windows.Graphics;

namespace StickyNotes.App.Views;

public sealed partial class NoteCardControl : UserControl
{
    private Note? _note;
    private INoteRepository? _repository;
    private readonly DispatcherTimer _debounceTimer;
    private double _userManualHeight = 0;
    private bool _isResizing = false;
    private double _resizeStartY;
    private double _resizeStartHeight;

    public Note? Note => _note;

    public event EventHandler<Note>? DeleteRequested;
    public event EventHandler<Note>? DuplicateRequested;
    public event EventHandler<Note>? FloatRequested;
    public event EventHandler<Note>? CardFocused;
    public event EventHandler<Note>? NoteUpdated;

    public NoteCardControl()
    {
        InitializeComponent();

        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _debounceTimer.Tick += DebounceTimer_Tick;
    }

    public void Initialize(Note note, INoteRepository repository, bool isReadOnly = false)
    {
        _note = note;
        _repository = repository;

        TxtTitle.Text = _note.Title ?? string.Empty;
        TxtTitle.IsReadOnly = isReadOnly;
        EditorBox.IsReadOnly = isReadOnly;
        TxtTitle.GotFocus += (s, e) => { if (_note != null) CardFocused?.Invoke(this, _note); };

        // Cargar contenido enriquecido o plano
        if (!string.IsNullOrEmpty(_note.Content))
        {
            try
            {
                EditorBox.Document.SetText(TextSetOptions.FormatRtf, _note.Content);
            }
            catch
            {
                EditorBox.Document.SetText(TextSetOptions.None, _note.Content);
            }
        }
        else
        {
            EditorBox.Document.SetText(TextSetOptions.None, string.Empty);
        }

        // Colocar el cursor al inicio del texto
        EditorBox.Document.Selection.SetRange(0, 0);

        // Aplicar paleta cromática
        ApplyColor(_note.Color);

        // Actualizar icono de sincronización
        IconSync.Glyph = _note.SyncStatus == SyncStatus.Synced ? "\uE753" : "\uE898";
        ToolTipService.SetToolTip(BadgeSync, _note.SyncStatus == SyncStatus.Synced ? "Sincronizado con Google Drive" : "Guardado local en SQLite");

        // Ajustar altura inicial progresiva
        AutoFitHeight();
    }

    public void ApplyColor(NoteColor color)
    {
        if (_note != null)
        {
            _note.Color = color;
        }

        var palette = ColorHelper.GetPalette(color);
        CardRoot.Background = palette.BodyBrush;
        CardRoot.BorderBrush = palette.BorderBrush;
        HeaderBar.Background = palette.HeaderBrush;
        FooterBar.BorderBrush = palette.BorderBrush;

        TxtTitle.Foreground = palette.ForegroundBrush;
        EditorBox.Foreground = palette.ForegroundBrush;
        EditorBox.Background = palette.BodyBrush;

        try
        {
            var sel = EditorBox.Document.Selection;
            EditorBox.Document.GetText(TextGetOptions.None, out var allText);
            if (!string.IsNullOrEmpty(allText))
            {
                sel.SetRange(0, allText.Length);
                sel.CharacterFormat.ForegroundColor = ((SolidColorBrush)palette.ForegroundBrush).Color;
                sel.CharacterFormat.BackgroundColor = Colors.Transparent;
                sel.SetRange(0, 0);
            }
            else
            {
                sel.CharacterFormat.ForegroundColor = ((SolidColorBrush)palette.ForegroundBrush).Color;
                sel.CharacterFormat.BackgroundColor = Colors.Transparent;
                sel.SetRange(0, 0);
            }
        }
        catch { }

        IconSync.Foreground = palette.ForegroundBrush;
        IconMore.Foreground = palette.ForegroundBrush;
        IconDelete.Foreground = palette.ForegroundBrush;

        IconBold.Foreground = palette.ForegroundBrush;
        IconItalic.Foreground = palette.ForegroundBrush;
        IconUnderline.Foreground = palette.ForegroundBrush;
        IconStrikethrough.Foreground = palette.ForegroundBrush;
        IconChecklist.Foreground = palette.ForegroundBrush;
    }

    public void FocusEditor()
    {
        EditorBox.Focus(FocusState.Programmatic);
    }

    public void HighlightCard()
    {
        CardRoot.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
        CardRoot.BorderThickness = new Thickness(2);
    }

    public void UnhighlightCard()
    {
        if (_note != null)
        {
            var palette = ColorHelper.GetPalette(_note.Color);
            CardRoot.BorderBrush = palette.BorderBrush;
            CardRoot.BorderThickness = new Thickness(1);
        }
    }

    private int CalculateContentLineCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 1;

        var cleanText = text.TrimEnd('\r', '\n');
        if (string.IsNullOrEmpty(cleanText))
            return 1;

        var rawLines = cleanText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        int totalLines = 0;

        foreach (var line in rawLines)
        {
            if (line.Length == 0)
            {
                totalLines += 1;
            }
            else
            {
                // En ancho de ~260px con fuente Segoe UI 13pt caben aprox 36 caracteres por línea
                totalLines += Math.Max(1, (int)Math.Ceiling(line.Length / 36.0));
            }
        }

        if (text.EndsWith('\r') || text.EndsWith('\n'))
        {
            totalLines += 1;
        }

        return Math.Max(1, totalLines);
    }

    public void AutoFitHeight()
    {
        if (_note == null) return;

        EditorBox.Document.GetText(TextGetOptions.None, out var text);

        var lineCount = CalculateContentLineCount(text);
        var textHeight = lineCount * 20.0;

        // Chrome: Cabecera (38px) + Footer (34px) + Grip (12px) + Padding y márgenes (20px) = 104px
        var totalNeeded = textHeight + 104.0;

        // Altura mínima base compacta (200px), o la definida manualmente por el usuario si la ajustó
        var minHeight = _userManualHeight > 180 ? _userManualHeight : 200.0;
        var maxHeight = 750.0;

        var targetHeight = Math.Clamp(totalNeeded, minHeight, maxHeight);
        CardRoot.Height = targetHeight;
        _note.Height = targetHeight;
    }

    private void TxtTitle_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_note == null) return;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void EditorBox_TextChanged(object sender, RoutedEventArgs e)
    {
        if (_note == null) return;

        AutoFitHeight();

        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void EditorBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (_note != null)
        {
            CardFocused?.Invoke(this, _note);
        }
    }

    private void CardRoot_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_note != null)
        {
            CardFocused?.Invoke(this, _note);
        }
    }

    private async void DebounceTimer_Tick(object? sender, object e)
    {
        _debounceTimer.Stop();
        if (_note == null || _repository == null) return;

        _note.Title = string.IsNullOrWhiteSpace(TxtTitle.Text) ? null : TxtTitle.Text.Trim();
        EditorBox.Document.GetText(TextGetOptions.FormatRtf, out var rtf);
        _note.Content = rtf;
        _note.Height = CardRoot.Height;

        await _repository.UpdateAsync(_note);
        NoteUpdated?.Invoke(this, _note);
    }

    private async void SetColor_Click(object sender, RoutedEventArgs e)
    {
        if (_note != null && _repository != null && sender is FrameworkElement fe && Enum.TryParse<NoteColor>(fe.Tag?.ToString(), out var color))
        {
            ApplyColor(color);
            await _repository.UpdateAsync(_note);
            NoteUpdated?.Invoke(this, _note);
        }
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_note != null)
        {
            DeleteRequested?.Invoke(this, _note);
        }
    }

    private void BtnDuplicateSingleNote_Click(object sender, RoutedEventArgs e)
    {
        if (_note != null)
        {
            DuplicateRequested?.Invoke(this, _note);
        }
    }

    private void BtnFloatSingleNote_Click(object sender, RoutedEventArgs e)
    {
        if (_note != null)
        {
            FloatRequested?.Invoke(this, _note);
        }
    }

    private async void BtnExportActiveMarkdown_Click(object sender, RoutedEventArgs e)
    {
        if (_note == null) return;
        var window = AppManager.Instance.SideNotes;
        if (window != null)
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            await ExportHelper.ExportNoteToMarkdownAsync(_note, hWnd);
        }
    }

    private async void SetNoteSize_Click(object sender, RoutedEventArgs e)
    {
        if (_note == null || _repository == null || sender is not FrameworkElement fe) return;
        if (double.TryParse(fe.Tag?.ToString(), out var targetSize))
        {
            if (targetSize <= 0)
            {
                _userManualHeight = 0;
                AutoFitHeight();
            }
            else
            {
                _userManualHeight = targetSize;
                CardRoot.Height = targetSize;
                _note.Height = targetSize;
            }

            await _repository.UpdateAsync(_note);
            NoteUpdated?.Invoke(this, _note);
        }
    }

    #region Formato Rápido

    private void BtnBold_Click(object sender, RoutedEventArgs e) =>
        EditorBox.Document.Selection.CharacterFormat.Bold = FormatEffect.Toggle;

    private void BtnItalic_Click(object sender, RoutedEventArgs e) =>
        EditorBox.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;

    private void BtnUnderline_Click(object sender, RoutedEventArgs e) =>
        EditorBox.Document.Selection.CharacterFormat.Underline =
            EditorBox.Document.Selection.CharacterFormat.Underline == UnderlineType.None ? UnderlineType.Single : UnderlineType.None;

    private void BtnStrikethrough_Click(object sender, RoutedEventArgs e) =>
        EditorBox.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.Toggle;

    private void BtnChecklist_Click(object sender, RoutedEventArgs e) =>
        EditorBox.Document.Selection.TypeText("☑ ");

    #endregion

    #region Redimensionamiento Manual

    private void ResizeGrip_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isResizing = true;
        var pt = e.GetCurrentPoint(this);
        _resizeStartY = pt.Position.Y;
        _resizeStartHeight = CardRoot.ActualHeight > 0 ? CardRoot.ActualHeight : CardRoot.Height;
        (sender as UIElement)?.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void ResizeGrip_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isResizing) return;

        var pt = e.GetCurrentPoint(this);
        var deltaY = pt.Position.Y - _resizeStartY;
        var newHeight = Math.Clamp(_resizeStartHeight + deltaY, 180, 800);

        _userManualHeight = newHeight;
        CardRoot.Height = newHeight;
        if (_note != null)
        {
            _note.Height = newHeight;
        }
        e.Handled = true;
    }

    private async void ResizeGrip_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isResizing)
        {
            _isResizing = false;
            (sender as UIElement)?.ReleasePointerCapture(e.Pointer);
            e.Handled = true;

            if (_note != null && _repository != null)
            {
                await _repository.UpdateAsync(_note);
                NoteUpdated?.Invoke(this, _note);
            }
        }
    }

    #endregion
}
