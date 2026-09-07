import React, { useState } from 'react';
import { 
  FolderTree, 
  Database, 
  Cloud, 
  ShieldCheck, 
  Layout, 
  Terminal, 
  CheckCircle2, 
  Layers, 
  Package, 
  Code2, 
  ArrowRight,
  Monitor
} from 'lucide-react';

export const ArchitectureView: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'solution' | 'tech_stack' | 'sync_engine' | 'db_schema'>('solution');

  return (
    <div className="bg-slate-900 border border-slate-800 rounded-xl p-5 shadow-xl text-slate-200">
      {/* Navigation tabs */}
      <div className="flex flex-wrap gap-2 border-b border-slate-800 pb-3 mb-5">
        <button
          onClick={() => setActiveTab('solution')}
          className={`px-3.5 py-1.5 rounded-lg text-xs font-medium flex items-center gap-2 transition-all ${
            activeTab === 'solution'
              ? 'bg-blue-600 text-white shadow-sm'
              : 'bg-slate-800/80 text-slate-400 hover:text-slate-200'
          }`}
        >
          <FolderTree className="w-4 h-4" />
          Estructura de la Solución .NET
        </button>

        <button
          onClick={() => setActiveTab('tech_stack')}
          className={`px-3.5 py-1.5 rounded-lg text-xs font-medium flex items-center gap-2 transition-all ${
            activeTab === 'tech_stack'
              ? 'bg-blue-600 text-white shadow-sm'
              : 'bg-slate-800/80 text-slate-400 hover:text-slate-200'
          }`}
        >
          <Layout className="w-4 h-4" />
          WinUI 3 vs WPF (Decisión de Stack)
        </button>

        <button
          onClick={() => setActiveTab('sync_engine')}
          className={`px-3.5 py-1.5 rounded-lg text-xs font-medium flex items-center gap-2 transition-all ${
            activeTab === 'sync_engine'
              ? 'bg-blue-600 text-white shadow-sm'
              : 'bg-slate-800/80 text-slate-400 hover:text-slate-200'
          }`}
        >
          <Cloud className="w-4 h-4" />
          Motor Sync Google Drive (appdata)
        </button>

        <button
          onClick={() => setActiveTab('db_schema')}
          className={`px-3.5 py-1.5 rounded-lg text-xs font-medium flex items-center gap-2 transition-all ${
            activeTab === 'db_schema'
              ? 'bg-blue-600 text-white shadow-sm'
              : 'bg-slate-800/80 text-slate-400 hover:text-slate-200'
          }`}
        >
          <Database className="w-4 h-4" />
          Esquema SQLite & EF Core
        </button>
      </div>

      {/* TAB CONTENT: Solution Structure */}
      {activeTab === 'solution' && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <h4 className="text-sm font-semibold text-slate-100 flex items-center gap-2">
              <FolderTree className="w-4 h-4 text-blue-400" />
              Solución Modular Clean Architecture (.NET 8)
            </h4>
            <span className="text-[11px] font-mono text-emerald-400 bg-emerald-950/60 border border-emerald-800 px-2 py-0.5 rounded">
              StickyNotes.sln
            </span>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* Project 1: App WinUI 3 */}
            <div className="bg-slate-950/70 border border-slate-800 rounded-lg p-3.5 flex flex-col gap-2">
              <div className="flex items-center justify-between">
                <span className="font-mono text-xs font-bold text-blue-400">1. StickyNotes.App (WinUI 3 / WinAppSDK)</span>
                <span className="text-[10px] bg-blue-950 text-blue-300 border border-blue-800 px-1.5 py-0.5 rounded">Presentación</span>
              </div>
              <p className="text-xs text-slate-400 leading-relaxed">
                Ventanas XAML sin bordes nativos, soporte TopMost, animaciones de hover con Composition APIs y bandeja de sistema.
              </p>
              <ul className="text-[11px] font-mono text-slate-300 space-y-1 bg-slate-900/90 p-2.5 rounded border border-slate-800/80">
                <li>├── Views/</li>
                <li className="pl-4 text-emerald-400">├── StickyNoteWindow.xaml (.cs) [Ventana flotante]</li>
                <li className="pl-4 text-cyan-400">├── SidePanelWindow.xaml (.cs) [Panel lateral]</li>
                <li className="pl-4">└── SettingsWindow.xaml (.cs) [Configuración]</li>
                <li>├── ViewModels/ (NoteViewModel, SidePanelViewModel)</li>
                <li>├── Services/ (TrayIconService, HotkeyService Win+Alt+N)</li>
                <li>└── App.xaml (.cs)</li>
              </ul>
            </div>

            {/* Project 2: Core Domain */}
            <div className="bg-slate-950/70 border border-slate-800 rounded-lg p-3.5 flex flex-col gap-2">
              <div className="flex items-center justify-between">
                <span className="font-mono text-xs font-bold text-indigo-400">2. StickyNotes.Core</span>
                <span className="text-[10px] bg-indigo-950 text-indigo-300 border border-indigo-800 px-1.5 py-0.5 rounded">Dominio</span>
              </div>
              <p className="text-xs text-slate-400 leading-relaxed">
                Entidades puras, contratos de repositorio, modelo de conflicto y lógica de sincronización agnóstica de UI.
              </p>
              <ul className="text-[11px] font-mono text-slate-300 space-y-1 bg-slate-900/90 p-2.5 rounded border border-slate-800/80">
                <li>├── Models/ (Note.cs, NoteColor.cs, SyncConflict.cs)</li>
                <li>├── Enums/ (DisplayMode.cs, SyncStatus.cs)</li>
                <li>├── Interfaces/</li>
                <li className="pl-4">├── INoteRepository.cs</li>
                <li className="pl-4">├── ISyncService.cs</li>
                <li className="pl-4">└── ICredentialStorage.cs</li>
                <li>└── DTOs/ (DriveNotePayload.cs)</li>
              </ul>
            </div>

            {/* Project 3: Data SQLite */}
            <div className="bg-slate-950/70 border border-slate-800 rounded-lg p-3.5 flex flex-col gap-2">
              <div className="flex items-center justify-between">
                <span className="font-mono text-xs font-bold text-amber-400">3. StickyNotes.Data (SQLite + EF Core)</span>
                <span className="text-[10px] bg-amber-950 text-amber-300 border border-amber-800 px-1.5 py-0.5 rounded">Persistencia</span>
              </div>
              <p className="text-xs text-slate-400 leading-relaxed">
                DbContext, migraciones automáticas, índices optimizados y cola local de cambios offline (Outbox).
              </p>
              <ul className="text-[11px] font-mono text-slate-300 space-y-1 bg-slate-900/90 p-2.5 rounded border border-slate-800/80">
                <li>├── Context/ (NotesDbContext.cs)</li>
                <li>├── Migrations/ (20260906_InitialCreate.cs)</li>
                <li>├── Repositories/ (SqliteNoteRepository.cs)</li>
                <li>└── Configurations/ (NoteEntityConfiguration.cs)</li>
              </ul>
            </div>

            {/* Project 4: Sync & Google Drive */}
            <div className="bg-slate-950/70 border border-slate-800 rounded-lg p-3.5 flex flex-col gap-2">
              <div className="flex items-center justify-between">
                <span className="font-mono text-xs font-bold text-emerald-400">4. StickyNotes.Sync.GoogleDrive</span>
                <span className="text-[10px] bg-emerald-950 text-emerald-300 border border-emerald-800 px-1.5 py-0.5 rounded">Integración Cloud</span>
              </div>
              <p className="text-xs text-slate-400 leading-relaxed">
                Google.Apis.Drive.v3 con scope <code className="text-blue-300">drive.appdata</code>, Windows Credential Manager y resolución de conflictos.
              </p>
              <ul className="text-[11px] font-mono text-slate-300 space-y-1 bg-slate-900/90 p-2.5 rounded border border-slate-800/80">
                <li>├── Services/ (GoogleDriveSyncService.cs, OAuthManager.cs)</li>
                <li>├── Security/ (WindowsCredentialManagerStorage.cs)</li>
                <li>├── Conflict/ (LastWriteWinsResolver.cs)</li>
                <li>└── Background/ (PeriodicSyncTimer.cs, NetworkWatcher.cs)</li>
              </ul>
            </div>
          </div>
        </div>
      )}

      {/* TAB CONTENT: Tech Stack WinUI 3 vs WPF */}
      {activeTab === 'tech_stack' && (
        <div className="space-y-4">
          <div className="bg-blue-950/40 border border-blue-800/70 rounded-lg p-4 text-xs text-blue-200">
            <h5 className="font-semibold text-sm text-blue-100 mb-1 flex items-center gap-2">
              <ShieldCheck className="w-4 h-4 text-blue-400" />
              Recomendación de Desarrollador Senior: WinUI 3 Desempaquetado (Unpackaged) con .NET 8
            </h5>
            <p className="leading-relaxed">
              WinUI 3 es la tecnología moderna nativa de Microsoft que usa la app oficial de Notas Rápidas en Windows 11 (Mica material, esquinas redondeadas Win32 directas, Fluent Design System). Para evitar dolores de cabeza con certificados de desarrollo de MSIX, configuramos <code className="bg-blue-900/70 px-1.5 py-0.5 rounded text-white font-mono">&lt;WindowsPackageType&gt;None&lt;/WindowsPackageType&gt;</code>. Esto permite compilar y ejecutar directamente desde F5 en Visual Studio y generar un instalador ligero con <strong>Inno Setup</strong> o MSIX según prefieras.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs">
            <div className="bg-slate-950/80 border border-slate-800 rounded-lg p-4 space-y-2">
              <div className="flex items-center gap-2 font-bold text-slate-200">
                <span className="w-2 h-2 rounded-full bg-emerald-400"></span>
                Opción A: WinUI 3 (Windows App SDK 1.5+) - ELEGIDA
              </div>
              <ul className="space-y-1.5 text-slate-400">
                <li className="flex items-start gap-1.5">
                  <CheckCircle2 className="w-3.5 h-3.5 text-emerald-400 mt-0.5 shrink-0" />
                  <span>UI idéntica a Notas Rápidas de Windows 11 (Fluent 2, RichEditBox nativo, tipografía Segoe UI Variable).</span>
                </li>
                <li className="flex items-start gap-1.5">
                  <CheckCircle2 className="w-3.5 h-3.5 text-emerald-400 mt-0.5 shrink-0" />
                  <span>Acceso directo a P/Invoke (SetWindowPos, HWND_TOPMOST, RegisterHotKey) sin limitaciones de sandbox.</span>
                </li>
                <li className="flex items-start gap-1.5">
                  <CheckCircle2 className="w-3.5 h-3.5 text-emerald-400 mt-0.5 shrink-0" />
                  <span>Desempaquetado (Unpackaged): ejecuta como un ejecutable .exe Win32 normal.</span>
                </li>
              </ul>
            </div>

            <div className="bg-slate-950/80 border border-slate-800 rounded-lg p-4 space-y-2">
              <div className="flex items-center gap-2 font-bold text-slate-300">
                <span className="w-2 h-2 rounded-full bg-amber-400"></span>
                Opción B: WPF con .NET 8 (Alternativa de Respaldo)
              </div>
              <ul className="space-y-1.5 text-slate-400">
                <li className="flex items-start gap-1.5">
                  <CheckCircle2 className="w-3.5 h-3.5 text-slate-400 mt-0.5 shrink-0" />
                  <span>Mayor madurez para ventanas transparentes complejas y soporte legacy en Windows 10 muy antiguo.</span>
                </li>
                <li className="flex items-start gap-1.5">
                  <CheckCircle2 className="w-3.5 h-3.5 text-slate-400 mt-0.5 shrink-0" />
                  <span>Requiere librerías de terceros (WPF-UI o ModernWpf) para replicar la estética visual exacta de Windows 11.</span>
                </li>
                <li className="flex items-start gap-1.5">
                  <CheckCircle2 className="w-3.5 h-3.5 text-slate-400 mt-0.5 shrink-0" />
                  <span>El motor de renderizado DirectX de WinUI 3 es más eficiente para animaciones de 60fps en el panel lateral.</span>
                </li>
              </ul>
            </div>
          </div>
        </div>
      )}

      {/* TAB CONTENT: Sync Engine Google Drive */}
      {activeTab === 'sync_engine' && (
        <div className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <div className="bg-slate-950/80 border border-slate-800 rounded-lg p-3 flex flex-col gap-1.5">
              <span className="text-xs font-semibold text-blue-400 flex items-center gap-1.5">
                <ShieldCheck className="w-4 h-4" />
                1. OAuth 2.0 + Credential Manager
              </span>
              <p className="text-[11px] text-slate-400">
                El refresh token nunca se guarda en archivos ni texto plano. Se delega al <code>Windows Credential Manager</code> mediante la API Win32 <code className="text-slate-300">CredWrite / CredRead</code> con target <code className="text-slate-300">StickyNotes.GoogleAuth</code>.
              </p>
            </div>

            <div className="bg-slate-950/80 border border-slate-800 rounded-lg p-3 flex flex-col gap-1.5">
              <span className="text-xs font-semibold text-emerald-400 flex items-center gap-1.5">
                <Cloud className="w-4 h-4" />
                2. Scope Invisible (drive.appdata)
              </span>
              <p className="text-[11px] text-slate-400">
                Usa <code className="text-emerald-300">https://www.googleapis.com/auth/drive.appdata</code>. Crea archivos JSON en la carpeta especial <code className="text-slate-300">appDataFolder</code>, inaccesibles y limpios para el usuario en su Drive habitual.
              </p>
            </div>

            <div className="bg-slate-950/80 border border-slate-800 rounded-lg p-3 flex flex-col gap-1.5">
              <span className="text-xs font-semibold text-amber-400 flex items-center gap-1.5">
                <Layers className="w-4 h-4" />
                3. Resolución de Conflictos
              </span>
              <p className="text-[11px] text-slate-400">
                Last-Write-Wins basado en <code className="text-amber-300">UpdatedAt (UTC)</code>. Si dos dispositivos modifican la misma nota offline simultáneamente, se clona una nota con sufijo <code className="text-slate-300">(conflicto)</code> para prevenir pérdida de datos.
              </p>
            </div>
          </div>

          <div className="bg-slate-950 border border-slate-800 rounded-lg p-3">
            <span className="text-xs font-mono font-semibold text-slate-300">Estructura del Payload JSON en Google Drive:</span>
            <pre className="text-[11px] font-mono text-emerald-400 bg-slate-900 p-3 rounded mt-2 overflow-x-auto">
{`{
  "id": "e3b0c442-98fc-1c14-9afb-4c8996fb9242",
  "title": "Reunión de sprint",
  "content": "Reunión de sprint\\n• Revisar sincronización con Drive\\n• Ajustar debounce",
  "color": "yellow",
  "positionX": 350.0,
  "positionY": 180.0,
  "monitor": 1,
  "createdAt": "2026-09-06T10:15:30Z",
  "updatedAt": "2026-09-06T11:00:12Z",
  "deletedAt": null,
  "deviceId": "DESKTOP-WIN11-MARCO",
  "schemaVersion": 1
}`}
            </pre>
          </div>
        </div>
      )}

      {/* TAB CONTENT: Database Schema */}
      {activeTab === 'db_schema' && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-slate-200">Tabla: <code>Notes</code> (SQLite vía EF Core)</span>
            <span className="text-[11px] text-slate-400 font-mono">%LocalAppData%\StickyNotes\notes.db</span>
          </div>

          <div className="overflow-x-auto border border-slate-800 rounded-lg">
            <table className="w-full text-[11px] text-left">
              <thead className="bg-slate-950 text-slate-300 uppercase tracking-wider font-mono">
                <tr>
                  <th className="py-2 px-3">Columna</th>
                  <th className="py-2 px-3">Tipo SQLite</th>
                  <th className="py-2 px-3">Tipo C#</th>
                  <th className="py-2 px-3">Restricción / Propósito</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800/80 font-mono text-slate-300">
                <tr className="bg-slate-900/40">
                  <td className="py-1.5 px-3 text-blue-400 font-bold">Id</td>
                  <td className="py-1.5 px-3">TEXT</td>
                  <td className="py-1.5 px-3">Guid / string</td>
                  <td className="py-1.5 px-3 text-slate-400 font-sans">PRIMARY KEY</td>
                </tr>
                <tr>
                  <td className="py-1.5 px-3 text-amber-300 font-semibold">Title</td>
                  <td className="py-1.5 px-3">TEXT</td>
                  <td className="py-1.5 px-3">string?</td>
                  <td className="py-1.5 px-3 text-slate-400 font-sans">Nullable / Título personalizado de la nota</td>
                </tr>
                <tr className="bg-slate-900/40">
                  <td className="py-1.5 px-3 text-slate-100">Content</td>
                  <td className="py-1.5 px-3">TEXT</td>
                  <td className="py-1.5 px-3">string</td>
                  <td className="py-1.5 px-3 text-slate-400 font-sans">Texto enriquecido / Markdown</td>
                </tr>
                <tr className="bg-slate-900/40">
                  <td className="py-1.5 px-3 text-slate-100">Color</td>
                  <td className="py-1.5 px-3">TEXT</td>
                  <td className="py-1.5 px-3">string / enum</td>
                  <td className="py-1.5 px-3 text-slate-400 font-sans">yellow, green, pink, purple, blue, gray</td>
                </tr>
                <tr>
                  <td className="py-1.5 px-3 text-slate-100">PositionX</td>
                  <td className="py-1.5 px-3">REAL</td>
                  <td className="py-1.5 px-3">double</td>
                  <td className="py-1.5 px-3 text-slate-400 font-sans">Coordenada X en píxeles de pantalla</td>
                </tr>
                <tr className="bg-slate-900/40">
                  <td className="py-1.5 px-3 text-slate-100">PositionY</td>
                  <td className="py-1.5 px-3">REAL</td>
                  <td className="py-1.5 px-3">double</td>
                  <td className="py-1.5 px-3 text-slate-400 font-sans">Coordenada Y en píxeles de pantalla</td>
                </tr>
                <tr>
                  <td className="py-1.5 px-3 text-slate-100">Monitor</td>
                  <td className="py-1.5 px-3">INTEGER</td>
                  <td className="py-1.5 px-3">int</td>
                  <td className="py-1.5 px-3 text-slate-400 font-sans">Índice o identificador de pantalla activa</td>
                </tr>
                <tr className="bg-slate-900/40">
                  <td className="py-1.5 px-3 text-slate-100">CreatedAt</td>
                  <td className="py-1.5 px-3">TEXT</td>
                  <td className="py-1.5 px-3">DateTime</td>
                  <td className="py-1.5 px-3 text-slate-400 font-sans">UTC timestamp de creación</td>
                </tr>
                <tr>
                  <td className="py-1.5 px-3 text-slate-100">UpdatedAt</td>
                  <td className="py-1.5 px-3">TEXT</td>
                  <td className="py-1.5 px-3">DateTime</td>
                  <td className="py-1.5 px-3 text-slate-400 font-sans">UTC timestamp (usado para Last-Write-Wins)</td>
                </tr>
                <tr className="bg-slate-900/40">
                  <td className="py-1.5 px-3 text-amber-300">DeletedAt</td>
                  <td className="py-1.5 px-3">TEXT</td>
                  <td className="py-1.5 px-3">DateTime?</td>
                  <td className="py-1.5 px-3 text-slate-400 font-sans">Nullable (Papelera de reciclaje temporal)</td>
                </tr>
                <tr>
                  <td className="py-1.5 px-3 text-slate-100">DeviceId</td>
                  <td className="py-1.5 px-3">TEXT</td>
                  <td className="py-1.5 px-3">string</td>
                  <td className="py-1.5 px-3 text-slate-400 font-sans">Identificador de máquina para detección de conflictos</td>
                </tr>
                <tr className="bg-slate-900/40">
                  <td className="py-1.5 px-3 text-emerald-400">SyncStatus</td>
                  <td className="py-1.5 px-3">INTEGER</td>
                  <td className="py-1.5 px-3">SyncStatus (enum)</td>
                  <td className="py-1.5 px-3 text-slate-400 font-sans">0: Synced, 1: PendingUpload, 2: Conflict</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
};
