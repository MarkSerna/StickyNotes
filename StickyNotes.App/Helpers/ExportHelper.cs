using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using StickyNotes.Core.Models;

namespace StickyNotes.App.Helpers;

public static class ExportHelper
{
    /// <summary>
    /// Exporta una nota individual a un archivo Markdown (.md) mediante FileSavePicker.
    /// </summary>
    public static async Task<bool> ExportNoteToMarkdownAsync(Note note, IntPtr windowHandle)
    {
        try
        {
            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("Documento Markdown (*.md)", new List<string> { ".md" });
            picker.FileTypeChoices.Add("Texto Plano (*.txt)", new List<string> { ".txt" });

            var rawTitle = string.IsNullOrWhiteSpace(note.Title) ? "Nota rápida" : note.Title;
            picker.SuggestedFileName = SanitizeFileName(rawTitle);

            InitializeWithWindow.Initialize(picker, windowHandle);

            var file = await picker.PickSaveFileAsync();
            if (file == null) return false;

            var sb = new StringBuilder();
            sb.AppendLine($"# {rawTitle}");
            sb.AppendLine();
            sb.AppendLine($"> Creado: {note.CreatedAt:yyyy-MM-dd HH:mm} | Color: {note.Color}");
            sb.AppendLine();

            var cleanBody = StripRtf(note.Content ?? string.Empty);
            sb.AppendLine(cleanBody);

            await FileIO.WriteTextAsync(file, sb.ToString());
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ExportHelper] Error al exportar Markdown: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Exporta todas las notas activas a un archivo JSON de respaldo.
    /// </summary>
    public static async Task<bool> ExportAllNotesToJsonAsync(IEnumerable<Note> notes, IntPtr windowHandle)
    {
        try
        {
            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("Archivo JSON de Respaldo (*.json)", new List<string> { ".json" });
            picker.SuggestedFileName = $"StickyNotes_Backup_{DateTime.Now:yyyyMMdd_HHmm}";

            InitializeWithWindow.Initialize(picker, windowHandle);

            var file = await picker.PickSaveFileAsync();
            if (file == null) return false;

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(notes, jsonOptions);

            await FileIO.WriteTextAsync(file, json);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ExportHelper] Error al exportar JSON: {ex.Message}");
            return false;
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "Nota" : sanitized;
    }

    public static string StripRtf(string rtf)
    {
        if (string.IsNullOrWhiteSpace(rtf)) return string.Empty;
        if (!rtf.TrimStart().StartsWith("{\\rtf", StringComparison.OrdinalIgnoreCase))
        {
            return rtf;
        }

        try
        {
            // Remover cabeceras de fuentes y colores de RTF
            var text = Regex.Replace(rtf, @"\{\*?\\[^{}]+}|[{}\\]|\\\n", " ");
            // Reemplazar saltos de línea \par y \line
            text = Regex.Replace(text, @"\\par[d]?", "\n");
            text = Regex.Replace(text, @"\\line", "\n");
            // Reemplazar códigos de control restantes
            text = Regex.Replace(text, @"\\[a-zA-Z0-9]+ ?", "");
            return text.Trim();
        }
        catch
        {
            return rtf;
        }
    }
}
