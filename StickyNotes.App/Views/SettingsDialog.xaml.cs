using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using StickyNotes.Sync.Services;

namespace StickyNotes.App.Views;

public sealed partial class SettingsDialog : ContentDialog
{
    private readonly GoogleDriveSyncService _syncService;
    private readonly SyncScheduler _syncScheduler;

    public SettingsDialog(GoogleDriveSyncService syncService, SyncScheduler syncScheduler)
    {
        InitializeComponent();
        _syncService = syncService;
        _syncScheduler = syncScheduler;

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

    private async void BtnConnectToggle_Click(object sender, RoutedEventArgs e)
    {
        var clientId = TxtClientId.Text.Trim();
        var clientSecret = TxtClientSecret.Password.Trim();

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            ShowMessage("Por favor ingresa un Client ID y Client Secret válidos de Google Cloud.", true);
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
            ShowMessage("Iniciando navegador para autorizar cuenta de Google...", false);
            BtnConnectToggle.IsEnabled = false;

            var ok = await _syncService.AuthenticateAsync();
            BtnConnectToggle.IsEnabled = true;

            if (ok)
            {
                UpdateUiState();
                ShowMessage("¡Conectado exitosamente con Google Drive!", false);
                // Disparar sincronización inmediata inicial
                _ = _syncScheduler.RequestImmediateSyncAsync();
            }
            else
            {
                ShowMessage("No se pudo completar la autenticación. Revisa tus credenciales.", true);
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
            var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(appSettingsPath))
            {
                var json = File.ReadAllText(appSettingsPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Crear o actualizar la estructura
                var dict = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json);
                if (dict != null)
                {
                    dict["GoogleDrive"] = new
                    {
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    };

                    var updatedJson = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(appSettingsPath, updatedJson);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsDialog] Error al persistir appsettings.json: {ex.Message}");
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
