import React from 'react';
import { 
  CheckCircle2, 
  Circle, 
  ArrowRight, 
  FolderTree, 
  Database, 
  AppWindow, 
  Sidebar, 
  Cloud, 
  KeyRound, 
  Play 
} from 'lucide-react';
import { SolutionStep } from '../types';

const STEPS: SolutionStep[] = [
  {
    id: 1,
    title: 'Confirmación de Stack y Estructura de Carpetas',
    status: 'completed',
    description: 'Validación de WinUI 3 vs WPF, arquitectura de 4 proyectos, dependencias NuGet y configuración desempaquetada.',
    technologies: ['.NET 8', 'WinUI 3', 'Windows App SDK', 'Clean Architecture'],
  },
  {
    id: 2,
    title: 'Esquema SQLite y Entity Framework Core',
    status: 'completed',
    description: 'Creación de la clase Note con Title, NotesDbContext, configuración Fluent API, migración inicial y repositorio con soft-delete.',
    technologies: ['EF Core 8', 'Microsoft.EntityFrameworkCore.Sqlite', 'Migrations', 'Repository Pattern'],
  },
  {
    id: 3,
    title: 'Ventana de Nota Individual (WinUI 3 XAML + Code-Behind)',
    status: 'completed',
    description: 'Ventana sin barra de título estándar, draggable Win32, título editable en cabecera, RichEditBox con toolbar de formato, 6 colores y debounce de 500ms.',
    technologies: ['XAML', 'WinUI 3 Window', 'RichEditBox', 'AppWindow P/Invoke'],
  },
  {
    id: 4,
    title: 'Panel Lateral Tipo SideNotes (Hover, Pestañas y Always on Top)',
    status: 'completed',
    description: 'Ventana acoplada al borde izquierdo/derecho con animación de 180ms, franja de 10px en reposo, pestañas apiladas y toggle de fijar.',
    technologies: ['Composition Animation', 'TopMost HWND', 'Monitor WorkArea', 'VisualStateManager'],
  },
  {
    id: 5,
    title: 'Servicio de Sincronización Google Drive (drive.appdata)',
    status: 'completed',
    description: 'OAuth 2.0 InstalledAppFlow, almacenamiento seguro en Windows DPAPI, sincronización bidireccional en appDataFolder y Last-Write-Wins.',
    technologies: ['Google.Apis.Drive.v3', 'Windows DPAPI', 'System.Text.Json', 'Last-Write-Wins'],
  },
  {
    id: 6,
    title: 'Bandeja del Sistema (Tray Icon) y Atajo Global Win+Alt+N',
    status: 'completed',
    description: 'Menú contextual nativo en la barra de tareas de Windows, alternar entre modos, hook de teclado global sin foco y arranque con Windows.',
    technologies: ['H.NotifyIcon', 'RegisterHotKey Win32', 'DispatcherQueue', 'HKCU Run'],
  },
  {
    id: 7,
    title: 'Registro en Google Cloud Console y Guía de Compilación Local',
    status: 'completed',
    description: 'Paso a paso para credenciales OAuth (Client ID, Secret, redirect URI), inyección de dependencias (Program.cs) y compilación single-file en Visual Studio 2022 / CLI.',
    technologies: ['Google Cloud Console', 'Visual Studio 2022', '.NET 8 Unpackaged', 'Single-File Publish'],
  },
];

export const ProjectRoadmap: React.FC = () => {
  return (
    <div className="bg-slate-900 border border-slate-800 rounded-xl p-5 shadow-xl">
      <div className="flex items-center justify-between mb-4">
        <div>
          <h3 className="text-sm font-semibold text-slate-100 flex items-center gap-2">
            <span>Hoja de Ruta de Implementación Paso a Paso</span>
          </h3>
          <p className="text-xs text-slate-400 mt-0.5">
            Desarrollo metódico según tu requerimiento con entrega de código 100% funcional.
          </p>
        </div>
        <span className="text-xs font-mono bg-emerald-950 text-emerald-400 border border-emerald-800 px-2.5 py-1 rounded-full">
          Proyecto 100% Completado (7/7 Pasos)
        </span>
      </div>

      <div className="space-y-3">
        {STEPS.map((step) => {
          const isCurrent = step.status === 'current';
          return (
            <div
              key={step.id}
              className={`p-3.5 rounded-lg border transition-all ${
                isCurrent
                  ? 'bg-blue-950/40 border-blue-600/80 shadow-md ring-1 ring-blue-500/20'
                  : 'bg-slate-950/60 border-slate-800/80 hover:border-slate-700'
              }`}
            >
              <div className="flex items-start gap-3">
                <div className="mt-0.5">
                  {isCurrent ? (
                    <span className="w-5 h-5 rounded-full bg-blue-600 text-white flex items-center justify-center text-xs font-bold ring-4 ring-blue-950">
                      {step.id}
                    </span>
                  ) : (
                    <span className="w-5 h-5 rounded-full bg-slate-800 text-slate-400 flex items-center justify-center text-xs font-medium border border-slate-700">
                      {step.id}
                    </span>
                  )}
                </div>

                <div className="flex-1">
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <h4
                      className={`text-xs font-semibold ${
                        isCurrent ? 'text-blue-300' : 'text-slate-200'
                      }`}
                    >
                      {step.title}
                    </h4>
                    {isCurrent && (
                      <span className="text-[10px] uppercase font-bold tracking-wider bg-blue-500/20 text-blue-300 border border-blue-500/40 px-2 py-0.5 rounded">
                        En curso ahora
                      </span>
                    )}
                  </div>
                  <p className="text-xs text-slate-400 mt-1 leading-relaxed">
                    {step.description}
                  </p>

                  <div className="flex flex-wrap gap-1.5 mt-2.5">
                    {step.technologies.map((tech) => (
                      <span
                        key={tech}
                        className="text-[10px] font-mono bg-slate-900 border border-slate-700 text-slate-300 px-2 py-0.5 rounded"
                      >
                        {tech}
                      </span>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
