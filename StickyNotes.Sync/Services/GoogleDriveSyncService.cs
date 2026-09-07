using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.DependencyInjection;
using StickyNotes.Core.Enums;
using StickyNotes.Core.Models;
using StickyNotes.Data.Repositories;
using StickyNotes.Sync.Models;
using StickyNotes.Sync.Security;

namespace StickyNotes.Sync.Services;

public record SyncReport(int Uploaded, int Downloaded, int Deleted, bool IsSuccess, string? ErrorMessage = null);

public class GoogleDriveSyncService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private string _clientId;
    private string _clientSecret;
    private DriveService? _driveService;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public bool IsAuthenticated => _driveService != null;
    public string ClientId => _clientId;
    public string ClientSecret => _clientSecret;
    public event Action<bool>? AuthStatusChanged;

    public GoogleDriveSyncService(IServiceScopeFactory scopeFactory, string clientId, string clientSecret)
    {
        _scopeFactory = scopeFactory;
        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    public void UpdateCredentials(string clientId, string clientSecret)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _driveService = null;
        AuthStatusChanged?.Invoke(false);
    }

    public async Task DisconnectAsync()
    {
        try
        {
            var dataStore = new WindowsCredentialDataStore("StickyNotesApp");
            await dataStore.ClearAsync();
            _driveService = null;
            AuthStatusChanged?.Invoke(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GoogleDriveSync] Error al desconectar: {ex.Message}");
        }
    }

    /// <summary>
    /// Inicia el flujo OAuth 2.0 InstalledAppFlow y persiste tokens cifrados en DPAPI.
    /// Solo solicita acceso a drive.appdata (carpeta oculta sin acceso a archivos personales).
    /// </summary>
    public async Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_clientId) || string.IsNullOrWhiteSpace(_clientSecret) || _clientId.Contains("TU_CLIENT_ID"))
        {
            AuthStatusChanged?.Invoke(false);
            return false;
        }

        try
        {
            var secrets = new ClientSecrets
            {
                ClientId = _clientId,
                ClientSecret = _clientSecret
            };

            // Almacén cifrado con Windows DPAPI (CurrentUser)
            var dataStore = new WindowsCredentialDataStore("StickyNotesApp");

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                new[] { DriveService.ScopeConstants.DriveAppdata },
                "user",
                cancellationToken,
                dataStore);

            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "StickyNotes Windows 11"
            });

            AuthStatusChanged?.Invoke(true);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GoogleDriveSync] Error en autenticación: {ex}");
            AuthStatusChanged?.Invoke(false);
            return false;
        }
    }

    /// <summary>
    /// Ejecuta la sincronización bidireccional completa resolviendo conflictos por Last-Write-Wins (UTC).
    /// </summary>
    public async Task<SyncReport> SyncAllAsync(CancellationToken cancellationToken = default)
    {
        if (_driveService == null)
        {
            var ok = await AuthenticateAsync(cancellationToken);
            if (!ok || _driveService == null)
            {
                return new SyncReport(0, 0, 0, false, "Usuario no autenticado en Google Drive.");
            }
        }

        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var noteRepository = scope.ServiceProvider.GetRequiredService<INoteRepository>();

            int uploaded = 0;
            int downloaded = 0;
            int deleted = 0;

            // 1. Obtener archivos remotos de la carpeta oculta appDataFolder
            var remoteFiles = await ListRemoteNotesAsync(cancellationToken);
            var localNotes = await noteRepository.GetAllNotesIncludingDeletedAsync();
            var localNotesMap = localNotes.ToDictionary(n => n.Id);

            // 2. Procesar notas remotas hacia local
            foreach (var remoteFile in remoteFiles)
            {
                if (cancellationToken.IsCancellationRequested) break;

                // Extraer el NoteId guardado en appProperties
                if (!remoteFile.AppProperties.TryGetValue("noteId", out var noteIdStr) || !Guid.TryParse(noteIdStr, out var noteId))
                {
                    continue;
                }

                var remotePayload = await DownloadNotePayloadAsync(remoteFile.Id, cancellationToken);
                if (remotePayload == null) continue;

                if (!localNotesMap.TryGetValue(noteId, out var localNote))
                {
                    // Nota creada remotamente en otro equipo -> Descargar a SQLite local
                    if (!remotePayload.IsDeleted)
                    {
                        var newLocal = remotePayload.ToDomainModel();
                        newLocal.SyncStatus = SyncStatus.Synced;
                        await noteRepository.CreateAsync(newLocal);
                        downloaded++;
                    }
                }
                else
                {
                    // Ambas existen: Resolver por Last-Write-Wins (UTC)
                    if (remotePayload.UpdatedAt > localNote.UpdatedAt)
                    {
                        if (remotePayload.IsDeleted && localNote.DeletedAt == null)
                        {
                            await noteRepository.SoftDeleteAsync(localNote.Id);
                            deleted++;
                        }
                        else if (!remotePayload.IsDeleted)
                        {
                            remotePayload.ApplyTo(localNote);
                            localNote.SyncStatus = SyncStatus.Synced;
                            await noteRepository.UpdateAsync(localNote);
                            downloaded++;
                        }
                    }
                }
            }

            // 3. Procesar notas locales pendientes de subida a Google Drive
            var pendingLocal = await noteRepository.GetPendingSyncNotesAsync();
            foreach (var local in pendingLocal)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var existingRemote = remoteFiles.FirstOrDefault(f => 
                    f.AppProperties.TryGetValue("noteId", out var idStr) && idStr == local.Id.ToString());

                var payload = DriveNotePayload.FromDomainModel(local);

                if (local.DeletedAt != null)
                {
                    // Si fue borrada localmente y existe en Drive, eliminar o marcar borrada
                    if (existingRemote != null)
                    {
                        await _driveService.Files.Delete(existingRemote.Id).ExecuteAsync(cancellationToken);
                        deleted++;
                    }
                }
                else
                {
                    // Subir o actualizar en appDataFolder
                    if (existingRemote != null)
                    {
                        await UpdateRemoteFileAsync(existingRemote.Id, payload, cancellationToken);
                    }
                    else
                    {
                        await CreateRemoteFileAsync(payload, cancellationToken);
                    }
                    uploaded++;
                }

                await noteRepository.MarkAsSyncedAsync(local.Id);
            }

            return new SyncReport(uploaded, downloaded, deleted, true);
        }
        catch (Exception ex)
        {
            return new SyncReport(0, 0, 0, false, ex.Message);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    #region Operaciones REST Drive API

    private async Task<List<Google.Apis.Drive.v3.Data.File>> ListRemoteNotesAsync(CancellationToken ct)
    {
        var request = _driveService!.Files.List();
        request.Spaces = "appDataFolder";
        request.Fields = "files(id, name, modifiedTime, appProperties)";
        request.Q = "'appDataFolder' in parents and trashed = false";

        var result = await request.ExecuteAsync(ct);
        return result.Files?.ToList() ?? new List<Google.Apis.Drive.v3.Data.File>();
    }

    private async Task<DriveNotePayload?> DownloadNotePayloadAsync(string fileId, CancellationToken ct)
    {
        var request = _driveService!.Files.Get(fileId);
        using var stream = new MemoryStream();
        await request.DownloadAsync(stream, ct);
        stream.Position = 0;

        return await JsonSerializer.DeserializeAsync<DriveNotePayload>(stream, cancellationToken: ct);
    }

    private async Task CreateRemoteFileAsync(DriveNotePayload payload, CancellationToken ct)
    {
        var fileMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = $"note_{payload.Id}.json",
            Parents = new List<string> { "appDataFolder" },
            AppProperties = new Dictionary<string, string>
            {
                { "noteId", payload.Id.ToString() }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var request = _driveService!.Files.Create(fileMetadata, stream, "application/json");
        await request.UploadAsync(ct);
    }

    private async Task UpdateRemoteFileAsync(string fileId, DriveNotePayload payload, CancellationToken ct)
    {
        var fileMetadata = new Google.Apis.Drive.v3.Data.File();
        var json = JsonSerializer.Serialize(payload);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var request = _driveService!.Files.Update(fileMetadata, fileId, stream, "application/json");
        await request.UploadAsync(ct);
    }

    #endregion
}