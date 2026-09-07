using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Apis.Util.Store;

namespace StickyNotes.Sync.Security;

/// <summary>
/// Almacén seguro para tokens OAuth 2.0 que utiliza la Data Protection API (DPAPI) de Windows.
/// Los tokens se cifran con la clave vinculada a la cuenta del usuario de Windows actual.
/// </summary>
public class WindowsCredentialDataStore : IDataStore
{
    private readonly string _storageFolder;

    public WindowsCredentialDataStore(string appFolderName)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _storageFolder = Path.Combine(localAppData, appFolderName, "Tokens");
        Directory.CreateDirectory(_storageFolder);
    }

    public Task StoreAsync<T>(string key, T value)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be null or empty", nameof(key));

        var filePath = GetFilePath(key);
        var json = JsonSerializer.Serialize(value);
        var plainBytes = Encoding.UTF8.GetBytes(json);

        // Cifrado simétrico transparente protegido por la cuenta de Windows
        var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(filePath, encryptedBytes);

        return Task.CompletedTask;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be null or empty", nameof(key));

        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
        {
            return Task.FromResult<T?>(default);
        }

        try
        {
            var encryptedBytes = File.ReadAllBytes(filePath);
            var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(decryptedBytes);
            var value = JsonSerializer.Deserialize<T>(json);
            return Task.FromResult(value);
        }
        catch (CryptographicException)
        {
            // Clave cambiada o archivo corrupto -> invalidar credencial
            File.Delete(filePath);
            return Task.FromResult<T?>(default);
        }
    }

    public Task DeleteAsync<T>(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be null or empty", nameof(key));

        var filePath = GetFilePath(key);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        if (Directory.Exists(_storageFolder))
        {
            var files = Directory.GetFiles(_storageFolder);
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        return Task.CompletedTask;
    }

    private string GetFilePath(string key)
    {
        // Sanitizar el nombre de archivo
        var safeKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(key))
            .Replace('/', '_')
            .Replace('+', '-');
        return Path.Combine(_storageFolder, $"{safeKey}.dat");
    }
}