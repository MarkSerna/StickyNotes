using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using StickyNotes.Sync.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace StickyNotes.App.Views;

public sealed partial class SettingsDialog : ContentDialog
{
    private readonly GoogleDriveSyncService _syncService;
    private readonly SyncScheduler _syncScheduler;
    private readonly IntPtr _windowHandle;

    public SettingsDialog(GoogleDriveSyncService syncService, SyncScheduler syncScheduler, IntPtr windowHandle = default)
    {
        InitializeComponent();
        _syncService = syncService;
        _syncScheduler = syncScheduler;
        _windowHandle = windowHandle;

        TxtClientId.Text = _syncService.ClientId ?? string.Empty;
        TxtClientSecret.Password = _syncService.ClientSecret ?? string.Empty;

        UpdateUiState();
    }

    private void UpdateUiState()
    {
        var isAuth = _syncService.IsAuthenticated;
        if (isAuth)
        {
            TxtAuthSubtitle.Text = "Conectado a Google Drive";
            TxtAuthSubtitle.Foreground = new SolidColorBrush(Microsoft.UI.Colors.ForestGreen);
            IconAuthStatus.Glyph = "\uE73E"; // Check
            IconAuthStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.ForestGreen);
            BtnConnectToggle.Content = "Desconectar";
        }
        else
        {
            TxtAuthSubtitle.Text = "No conectado";
            TxtAuthSubtitle.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray);
            IconAuthStatus.Glyph = "\uE753"; // Cloud
            IconAuthStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray);
            BtnConnectToggle.Content = "Conectar";
        }
    }

    private async void BtnImportJson_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.Downloads;
            picker.FileTypeFilter.Add(".json");

            var hWnd = _windowHandle != IntPtr.Zero ? _windowHandle : System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            InitializeWithWindow.Initialize(picker, hWnd);

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            var jsonText = await Windows.Storage.FileIO.ReadTextAsync(file);
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;

            JsonElement section = default;
            if (root.TryGetProperty("installed", out var installedSec))
            {
                section = installedSec;
            }
            else if (root.TryGetProperty("web", out var webSec))
            {
                section = webSec;
            }

            if (section.ValueKind == JsonValueKind.Object &&
                section.TryGetProperty("client_id", out var cidProp) &&
                section.TryGetProperty("client_secret", out var csecProp))
            {
                var cid = cidProp.GetString() ?? string.Empty;
                var csec = csecProp.GetString() ?? string.Empty;

                TxtClientId.Text = cid;
                TxtClientSecret.Password = csec;

                _syncService.UpdateCredentials(cid, csec);
                PersistCredentialsLocally(cid, csec);

                ShowMessage("¡Credenciales cargadas con éxito! Haz clic en 'Conectar' para autorizar tu cuenta.", false);
            }
            else
            {
                ShowMessage("El archivo JSON no contiene credenciales válidas de Google OAuth (debe contener 'installed' o 'web').", true);
            }
        }
        catch (Exception ex)
        {
            ShowMessage($"Error al importar el archivo JSON: {ex.Message}", true);
        }
    }

    private async void BtnConnectToggle_Click(object sender, RoutedEventArgs e)
    {
        var clientId = TxtClientId.Text.Trim();
        var clientSecret = TxtClientSecret.Password.Trim();

        if (string.IsNullOrWhiteSpace(clientId) || clientId.Contains("TU_CLIENT_ID") || string.IsNullOrWhiteSpace(clientSecret))
        {
            ShowMessage("Por favor ingresa un Client ID y Client Secret válidos de Google Cloud o pulsa 'Importar JSON'.", true);
            return;
        }

        if (_syncService.IsAuthenticated)
        {
            await _syncService.DisconnectAsync();
            UpdateUiState();
            ShowMessage("Sesión cerrada correctamente.", false);
        }
        else
        {
            _syncService.UpdateCredentials(clientId, clientSecret);
            PersistCredentialsLocally(clientId, clientSecret);
            ShowMessage("Abriendo navegador para iniciar sesión con Google...", false);
            BtnConnectToggle.IsEnabled = false;

            var ok = await _syncService.AuthenticateAsync();
            BtnConnectToggle.IsEnabled = true;

            if (ok)
            {
                UpdateUiState();
                ShowMessage("¡Conectado exitosamente con Google Drive!", false);
                _ = _syncScheduler.RequestImmediateSyncAsync();
            }
            else
            {
                ShowMessage("No se pudo completar la autenticación. Verifica que tu correo esté añadido como usuario de prueba en Google Cloud si la app está en modo Testing.", true);
            }
        }
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var clientId = TxtClientId.Text.Trim();
        var clientSecret = TxtClientSecret.Password.Trim();

        _syncService.UpdateCredentials(clientId, clientSecret);
        PersistCredentialsLocally(clientId, clientSecret);
    }

    private void PersistCredentialsLocally(string clientId, string clientSecret)
    {
        try
        {
            var localSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.local.json");
            var settings = new
            {
                GoogleDrive = new
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    ApplicationName = "StickyNotes Windows 11",
                    Scopes = new[] { "https://www.googleapis.com/auth/drive.appdata" }
                }
            };

            var updatedJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(localSettingsPath, updatedJson);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsDialog] Error al persistir appsettings.local.json: {ex.Message}");
        }
    }

    private void ShowMessage(string message, bool isError)
    {
        TxtStatusMessage.Text = message;
        TxtStatusMessage.Foreground = isError 
            ? new SolidColorBrush(Microsoft.UI.Colors.Crimson) 
            : new SolidColorBrush(Microsoft.UI.Colors.DarkGoldenrod);
        TxtStatusMessage.Visibility = Visibility.Visible;
    }
}
