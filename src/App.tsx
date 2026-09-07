import React, { useState } from 'react';
import { 
  AppWindow, 
  Layers, 
  Sidebar, 
  Cloud, 
  Database, 
  FolderTree, 
  ShieldCheck, 
  Sparkles,
  Command,
  FileCode2,
  ListTodo
} from 'lucide-react';
import { InteractiveStickyNotesSimulator } from './components/InteractiveStickyNotesSimulator';
import { ArchitectureView } from './components/ArchitectureView';
import { ProjectRoadmap } from './components/ProjectRoadmap';
import { CodeGeneratedView } from './components/CodeGeneratedView';

export default function App() {
  const [activeMainSection, setActiveMainSection] = useState<'simulator' | 'architecture' | 'code' | 'roadmap'>('code');

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col font-sans selection:bg-blue-600 selection:text-white">
      {/* Header with Windows 11 style Mica glass aesthetics */}
      <header className="border-b border-slate-800/80 bg-slate-900/70 backdrop-blur-md sticky top-0 z-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 py-3.5 flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-amber-300 via-amber-400 to-yellow-500 shadow-lg shadow-amber-500/20 flex items-center justify-center text-slate-950 font-bold">
              <AppWindow className="w-5 h-5 text-slate-950" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h1 className="text-base font-bold tracking-tight text-white">Notas Rápidas para Windows</h1>
                <span className="text-[10px] uppercase font-mono px-2 py-0.5 rounded bg-blue-950 text-blue-400 border border-blue-800">
                  .NET 8 + WinUI 3
                </span>
              </div>
              <p className="text-xs text-slate-400">
                Sincronización con Google Drive (appdata) • Modo Flotante & Panel Lateral SideNotes
              </p>
            </div>
          </div>

          {/* Navigation selector */}
          <div className="flex items-center bg-slate-950 p-1 rounded-xl border border-slate-800 text-xs">
            <button
              onClick={() => setActiveMainSection('code')}
              className={`px-3.5 py-1.5 rounded-lg font-medium transition-all flex items-center gap-1.5 ${
                activeMainSection === 'code'
                  ? 'bg-blue-600 text-white shadow-sm'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              <FileCode2 className="w-3.5 h-3.5" />
              Código C# (.NET 8)
            </button>
            <button
              onClick={() => setActiveMainSection('simulator')}
              className={`px-3.5 py-1.5 rounded-lg font-medium transition-all flex items-center gap-1.5 ${
                activeMainSection === 'simulator'
                  ? 'bg-blue-600 text-white shadow-sm'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              <Layers className="w-3.5 h-3.5" />
              Simulador Visual
            </button>
            <button
              onClick={() => setActiveMainSection('architecture')}
              className={`px-3.5 py-1.5 rounded-lg font-medium transition-all flex items-center gap-1.5 ${
                activeMainSection === 'architecture'
                  ? 'bg-blue-600 text-white shadow-sm'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              <FolderTree className="w-3.5 h-3.5" />
              Arquitectura .NET
            </button>
            <button
              onClick={() => setActiveMainSection('roadmap')}
              className={`px-3.5 py-1.5 rounded-lg font-medium transition-all flex items-center gap-1.5 ${
                activeMainSection === 'roadmap'
                  ? 'bg-blue-600 text-white shadow-sm'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              <ListTodo className="w-3.5 h-3.5" />
              Plan de Desarrollo
            </button>
          </div>
        </div>
      </header>

      {/* Main Container */}
      <main className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 py-6 space-y-6">
        {/* Quick Highlights Strip */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <div className="bg-slate-900/60 border border-slate-800/80 p-3 rounded-xl flex items-center gap-3">
            <div className="p-2 rounded-lg bg-yellow-500/10 text-yellow-400">
              <Layers className="w-4 h-4" />
            </div>
            <div>
              <div className="text-[11px] text-slate-400">Doble Modo</div>
              <div className="text-xs font-semibold text-slate-200">Flotante & Lateral</div>
            </div>
          </div>

          <div className="bg-slate-900/60 border border-slate-800/80 p-3 rounded-xl flex items-center gap-3">
            <div className="p-2 rounded-lg bg-emerald-500/10 text-emerald-400">
              <Cloud className="w-4 h-4" />
            </div>
            <div>
              <div className="text-[11px] text-slate-400">Google Drive</div>
              <div className="text-xs font-semibold text-slate-200">drive.appdata Scope</div>
            </div>
          </div>

          <div className="bg-slate-900/60 border border-slate-800/80 p-3 rounded-xl flex items-center gap-3">
            <div className="p-2 rounded-lg bg-blue-500/10 text-blue-400">
              <Database className="w-4 h-4" />
            </div>
            <div>
              <div className="text-[11px] text-slate-400">Persistencia</div>
              <div className="text-xs font-semibold text-slate-200">SQLite + EF Core 8</div>
            </div>
          </div>

          <div className="bg-slate-900/60 border border-slate-800/80 p-3 rounded-xl flex items-center gap-3">
            <div className="p-2 rounded-lg bg-purple-500/10 text-purple-400">
              <ShieldCheck className="w-4 h-4" />
            </div>
            <div>
              <div className="text-[11px] text-slate-400">Seguridad Windows</div>
              <div className="text-xs font-semibold text-slate-200">Credential Manager</div>
            </div>
          </div>
        </div>

        {/* Dynamic Section View */}
        {activeMainSection === 'code' && (
          <div className="space-y-4">
            <CodeGeneratedView />
          </div>
        )}

        {activeMainSection === 'simulator' && (
          <div className="space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div>
                <h2 className="text-sm font-bold text-slate-100 flex items-center gap-2">
                  <span>Prototipo Interactivo de UX / UI</span>
                  <span className="text-[11px] font-normal text-slate-400">
                    (Prueba el cambio entre ventanas flotantes y panel lateral de borde)
                  </span>
                </h2>
              </div>
              <div className="text-xs text-slate-400 flex items-center gap-2">
                <span className="flex items-center gap-1 font-mono bg-slate-900 px-2 py-1 rounded border border-slate-800">
                  <Command className="w-3 h-3 text-blue-400" /> Win + Alt + N
                </span>
                <span>Atajo global</span>
              </div>
            </div>

            <InteractiveStickyNotesSimulator />
          </div>
        )}

        {activeMainSection === 'architecture' && (
          <div className="space-y-4">
            <ArchitectureView />
          </div>
        )}

        {activeMainSection === 'roadmap' && (
          <div className="space-y-4">
            <ProjectRoadmap />
          </div>
        )}
      </main>

      {/* Footer */}
      <footer className="border-t border-slate-800/80 bg-slate-900/40 py-3 text-center text-xs text-slate-500">
        Notas Rápidas para Windows 11/10 • Arquitectura Clean C# con WinUI 3 y Sincronización Google Drive
      </footer>
    </div>
  );
}
