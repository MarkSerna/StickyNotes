using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using StickyNotes.App.Helpers;
using ColorHelper = StickyNotes.App.Helpers.ColorHelper;
using StickyNotes.Core.Enums;
using StickyNotes.Core.Models;
using StickyNotes.Data.Repositories;
using StickyNotes.App.Services;
using Windows.Graphics;
using WinRT.Interop;

namespace StickyNotes.App.Views;

public sealed partial class NoteWindow : Window
{
    private readonly Note _note;
    private readonly INoteRepository _repository;
    private readonly AppWindow _appWindow;
    private readonly OverlappedPresenter _presenter;
    private readonly DispatcherTimer _debounceTimer;

    public Note Note => _note;

    public NoteWindow(Note note, INoteRepository repository)
    {
        InitializeComponent();

        _note = note;
        _repository = repository;

        // Configuración de AppWindow WinUI 3
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        _presenter = (_appWindow.Presenter as OverlappedPresenter)!;

        // Quitar la barra de título estándar de Windows para look idéntico a Notas Rápidas
        _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        _appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
        _appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        // Restaurar posición, dimensiones y AlwaysOnTop
        RestoreWindowBounds();

        // Configurar timer de debounce de 500ms para auto-guardado en SQLite
        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _debounceTimer.Tick += DebounceTimer_Tick;

        // Cargar datos en los controles
        LoadNoteData();

        // Aplicar paleta de color
        ApplyNoteColor(_note.Color);

        // Guardar coordenadas cuando se mueva o redimensione la ventana
        _appWindow.Changed += AppWindow_Changed;
    }

    private void RestoreWindowBounds()
    {
        _presenter.IsAlwaysOnTop = _note.IsAlwaysOnTop;
        BtnPinTop.IsChecked = _note.IsAlwaysOnTop;

        var x = (int)_note.PositionX;
        var y = (int)_note.PositionY;
        var width = (int)Math.Max(260, _note.Width);
        var height = (int)Math.Max(220, _note.Height);

        _appWindow.MoveAndResize(new RectInt32(x, y, width, height));
    }

    private void LoadNoteData()
    {
        TxtTitle.Text = _note.Title ?? string.Empty;

        // Carga contenido enriquecido RTF si existe, de lo contrario texto plano
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
    }

    private void ApplyNoteColor(NoteColor color)
    {
        var palette = ColorHelper.GetPalette(color);
        RootGrid.Background = palette.BodyBrush;
        RootGrid.BorderBrush = palette.BorderBrush;
        AppTitleBar.Background = palette.HeaderBrush;
        TxtTitle.Foreground = palette.ForegroundBrush;
        EditorBox.Foreground = palette.ForegroundBrush;
        EditorBox.Background = palette.BodyBrush;
        TxtSyncStatus.Foreground = palette.ForegroundBrush;

        try
        {
            // Asegurar que el documento no mantenga fondos RTF heredados (elimina fondo de carácter)
            var sel = EditorBox.Document.Selection;
            sel.SetRange(0, 0);
            sel.Expand(TextRangeUnit.Story);
            sel.CharacterFormat.BackgroundColor = ((Microsoft.UI.Xaml.Media.SolidColorBrush)palette.BodyBrush).Color;
            sel.CharacterFormat.ForegroundColor = ((Microsoft.UI.Xaml.Media.SolidColorBrush)palette.ForegroundBrush).Color;
            sel.SetRange(0, 0);
        }
        catch
        {
            // No crítico; si la limpieza falla, seguir sin bloquear la UI
        }
    }

    #region Auto-guardado Debounce

    private void TxtTitle_TextChanged(object sender, TextChangedEventArgs e)
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void EditorBox_TextChanged(object sender, RoutedEventArgs e)
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private async void DebounceTimer_Tick(object? sender, object e)
    {
        _debounceTimer.Stop();

        _note.Title = string.IsNullOrWhiteSpace(TxtTitle.Text) ? null : TxtTitle.Text.Trim();

        // Extrae el contenido en formato RTF para preservar negrita, cursiva, etc.
        EditorBox.Document.GetText(TextGetOptions.FormatRtf, out var rtfContent);
        _note.Content = rtfContent;

        TxtSyncStatus.Text = "Guardando...";
        await _repository.UpdateAsync(_note);

        TxtSyncStatus.Text = _note.SyncStatus == SyncStatus.Synced ? "Sincronizado" : "Guardado local";
    }

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidPositionChange || args.DidSizeChange)
        {
            _note.PositionX = _appWindow.Position.X;
            _note.PositionY = _appWindow.Position.Y;
            _note.Width = _appWindow.Size.Width;
            _note.Height = _appWindow.Size.Height;

            _debounceTimer.Stop();
            _debounceTimer.Start();
        }
    }

    #endregion

    #region Formato de Texto Enriquecido

    private void BtnBold_Click(object sender, RoutedEventArgs e)
    {
        var selection = EditorBox.Document.Selection;
        selection.CharacterFormat.Bold = FormatEffect.Toggle;
    }

    private void BtnItalic_Click(object sender, RoutedEventArgs e)
    {
        var selection = EditorBox.Document.Selection;
        selection.CharacterFormat.Italic = FormatEffect.Toggle;
    }

    private void BtnUnderline_Click(object sender, RoutedEventArgs e)
    {
        var selection = EditorBox.Document.Selection;
        selection.CharacterFormat.Underline = selection.CharacterFormat.Underline == UnderlineType.None 
            ? UnderlineType.Single 
            : UnderlineType.None;
    }

    private void BtnStrikethrough_Click(object sender, RoutedEventArgs e)
    {
        var selection = EditorBox.Document.Selection;
        selection.CharacterFormat.Strikethrough = FormatEffect.Toggle;
    }

    private void BtnBullets_Click(object sender, RoutedEventArgs e)
    {
        var selection = EditorBox.Document.Selection;
        selection.ParagraphFormat.ListType = selection.ParagraphFormat.ListType == MarkerType.Bullet 
            ? MarkerType.None 
            : MarkerType.Bullet;
    }

    #endregion

    #region Acciones de Cabecera

    private async void ColorItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && Enum.TryParse<NoteColor>(item.Tag?.ToString(), out var newColor))
        {
            _note.Color = newColor;
            ApplyNoteColor(newColor);
            await _repository.UpdateAsync(_note);
        }
    }

    private async void BtnPinTop_Click(object sender, RoutedEventArgs e)
    {
        _note.IsAlwaysOnTop = BtnPinTop.IsChecked ?? false;
        _presenter.IsAlwaysOnTop = _note.IsAlwaysOnTop;
        await _repository.UpdateAsync(_note);
    }

    private async void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        var hWnd = WindowNative.GetWindowHandle(this);
        await ExportHelper.ExportNoteToMarkdownAsync(_note, hWnd);
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        await _repository.SoftDeleteAsync(_note.Id);
        this.Close();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void BtnNewNote_Click(object sender, RoutedEventArgs e)
    {
        // Dispara evento para que AppManager cree y abra una nueva nota junto a esta
        AppManager.Instance.CreateAndOpenNewNote(_note.PositionX + 30, _note.PositionY + 30);
    }

    #endregion
}