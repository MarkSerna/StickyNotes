import React, { useState, useEffect } from 'react';
import { 
  Plus, 
  Trash2, 
  Palette, 
  Pin, 
  PinOff, 
  Bold, 
  Italic, 
  Underline, 
  Strikethrough, 
  CheckSquare, 
  CloudCheck, 
  CloudUpload,
  Layers,
  Sidebar as SidebarIcon,
  Monitor,
  Move,
  MoreVertical,
  ExternalLink,
  Copy,
  Check
} from 'lucide-react';
import { NoteItem, NoteColorConfig, DisplayMode, SidebarEdge } from '../types';

export const NOTE_COLORS: Record<NoteItem['color'], NoteColorConfig> = {
  yellow: { id: 'yellow', name: 'Amarillo', bgHex: '#FFF385', headerHex: '#FEE75C', borderHex: '#F6D83B', textColor: '#2D2817' },
  green: { id: 'green', name: 'Verde', bgHex: '#D2F8B8', headerHex: '#BAF096', borderHex: '#A2E278', textColor: '#1A3311' },
  pink: { id: 'pink', name: 'Rosa', bgHex: '#FFCEE8', headerHex: '#FCAFD9', borderHex: '#F389C3', textColor: '#3B1528' },
  purple: { id: 'purple', name: 'Morado', bgHex: '#E7DCFF', headerHex: '#D6C3FF', borderHex: '#BF9FFF', textColor: '#261543' },
  blue: { id: 'blue', name: 'Azul', bgHex: '#CEECFE', headerHex: '#AFDDFC', borderHex: '#8CCBF7', textColor: '#0F2B40' },
  gray: { id: 'gray', name: 'Gris carbón', bgHex: '#E9ECEF', headerHex: '#DEE2E6', borderHex: '#CED4DA', textColor: '#212529' },
};

export const InteractiveStickyNotesSimulator: React.FC = () => {
  const [mode, setMode] = useState<DisplayMode>('floating');
  const [sidebarEdge, setSidebarEdge] = useState<SidebarEdge>('right');
  const [sidebarHovered, setSidebarHovered] = useState(false);
  const [isSidebarPinned, setIsSidebarPinned] = useState(false);
  const [activeSidebarNoteId, setActiveSidebarNoteId] = useState<string | null>(null);

  const [notes, setNotes] = useState<NoteItem[]>([
    {
      id: '1',
      title: 'Arquitectura WinUI 3 y Drive',
      content: 'Reunión técnica:\n• Scope: drive.appdata\n• SQLite con EF Core\n• Debounce de guardado a 500ms',
      color: 'yellow',
      positionX: 60,
      positionY: 80,
      width: 280,
      height: 260,
      monitor: 1,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      deletedAt: null,
      deviceId: 'WIN-PC-DEV01',
      syncStatus: 'synced',
      isAlwaysOnTop: true,
    },
    {
      id: '2',
      title: 'Atajo Global de Teclado',
      content: 'Atajo global: Win+Alt+N para crear nota rápida.\nPanel lateral animado a 180ms con hover buffer.',
      color: 'blue',
      positionX: 380,
      positionY: 120,
      width: 290,
      height: 250,
      monitor: 1,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      deletedAt: null,
      deviceId: 'WIN-PC-DEV01',
      syncStatus: 'synced',
      isAlwaysOnTop: false,
    },
    {
      id: '3',
      title: 'Lista de Verificación',
      content: 'Colores disponibles:\n☑ Amarillo clásico\n☑ Verde menta\n☑ Rosa pastel\n☑ Morado lavanda\n☑ Azul cielo\n☑ Gris neutro',
      color: 'green',
      positionX: 700,
      positionY: 70,
      width: 270,
      height: 240,
      monitor: 1,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      deletedAt: null,
      deviceId: 'WIN-PC-DEV01',
      syncStatus: 'pending',
      isAlwaysOnTop: false,
    },
  ]);

  const [colorMenuNoteId, setColorMenuNoteId] = useState<string | null>(null);
  const [sideMenuOpen, setSideMenuOpen] = useState(false);
  const [draggingNoteId, setDraggingNoteId] = useState<string | null>(null);
  const [dragOffset, setDragOffset] = useState<{ x: number; y: number }>({ x: 0, y: 0 });

  // Duplicate note
  const handleDuplicateNote = (note: NoteItem) => {
    const duplicated: NoteItem = {
      ...note,
      id: Date.now().toString(),
      title: note.title ? `${note.title} (copia)` : 'Copia de nota',
      positionX: note.positionX + 30,
      positionY: note.positionY + 30,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      syncStatus: 'pending',
    };
    setNotes((prev) => [...prev, duplicated]);
    if (mode === 'sidebar') {
      setActiveSidebarNoteId(duplicated.id);
    }
    setSideMenuOpen(false);
  };

  // Detach to floating
  const handleDetachToFloating = (noteId: string) => {
    setMode('floating');
    setSideMenuOpen(false);
  };

  // Add new note
  const handleAddNote = () => {
    const newNote: NoteItem = {
      id: Date.now().toString(),
      title: '',
      content: 'Nueva nota...',
      color: 'yellow',
      positionX: 120 + (notes.length % 4) * 40,
      positionY: 120 + (notes.length % 4) * 35,
      width: 280,
      height: 250,
      monitor: 1,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      deletedAt: null,
      deviceId: 'WIN-PC-DEV01',
      syncStatus: 'pending',
      isAlwaysOnTop: false,
    };
    setNotes((prev) => [...prev, newNote]);
    if (mode === 'sidebar') {
      setActiveSidebarNoteId(newNote.id);
    }
  };

  const handleUpdateTitle = (id: string, newTitle: string) => {
    setNotes((prev) =>
      prev.map((n) =>
        n.id === id
          ? { ...n, title: newTitle, updatedAt: new Date().toISOString(), syncStatus: 'pending' }
          : n
      )
    );
  };

  const handleUpdateContent = (id: string, newContent: string) => {
    setNotes((prev) =>
      prev.map((n) =>
        n.id === id
          ? { ...n, content: newContent, updatedAt: new Date().toISOString(), syncStatus: 'pending' }
          : n
      )
    );
  };

  const handleChangeColor = (id: string, color: NoteItem['color']) => {
    setNotes((prev) =>
      prev.map((n) => (n.id === id ? { ...n, color, updatedAt: new Date().toISOString() } : n))
    );
    setColorMenuNoteId(null);
  };

  const handleDeleteNote = (id: string) => {
    setNotes((prev) => prev.filter((n) => n.id !== id));
    if (activeSidebarNoteId === id) {
      setActiveSidebarNoteId(null);
    }
  };

  const toggleAlwaysOnTop = (id: string) => {
    setNotes((prev) =>
      prev.map((n) => (n.id === id ? { ...n, isAlwaysOnTop: !n.isAlwaysOnTop } : n))
    );
  };

  // Dragging logic within desktop mockup
  const handleMouseDown = (e: React.MouseEvent, note: NoteItem) => {
    setDraggingNoteId(note.id);
    setDragOffset({
      x: e.clientX - note.positionX,
      y: e.clientY - note.positionY,
    });
  };

  const handleMouseMove = (e: React.MouseEvent) => {
    if (!draggingNoteId) return;
    setNotes((prev) =>
      prev.map((n) => {
        if (n.id === draggingNoteId) {
          return {
            ...n,
            positionX: Math.max(10, Math.min(850, e.clientX - dragOffset.x)),
            positionY: Math.max(10, Math.min(450, e.clientY - dragOffset.y)),
          };
        }
        return n;
      })
    );
  };

  const handleMouseUp = () => {
    setDraggingNoteId(null);
  };

  const isSidebarOpen = sidebarHovered || isSidebarPinned;

  return (
    <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden shadow-2xl">
      {/* Control bar / System tray simulator banner */}
      <div className="bg-slate-800/90 px-4 py-3 border-b border-slate-700/80 flex flex-wrap items-center justify-between gap-3 text-sm text-slate-200">
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2 bg-slate-950/60 px-3 py-1.5 rounded-lg border border-slate-700/60">
            <span className="w-2.5 h-2.5 rounded-full bg-emerald-400 animate-pulse"></span>
            <span className="font-semibold text-xs tracking-wider uppercase text-slate-300">Simulador de Experiencia Windows 11</span>
          </div>
          <span className="text-xs text-slate-400 hidden sm:inline">
            Replicando Notas rápidas + Panel lateral SideNotes
          </span>
        </div>

        {/* Mode Switcher */}
        <div className="flex items-center gap-2">
          <div className="bg-slate-950 p-1 rounded-lg border border-slate-700 flex items-center gap-1 text-xs">
            <button
              onClick={() => setMode('floating')}
              className={`px-3 py-1 rounded font-medium transition-all flex items-center gap-1.5 ${
                mode === 'floating'
                  ? 'bg-blue-600 text-white shadow-sm'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              <Layers className="w-3.5 h-3.5" />
              Ventanas flotantes
            </button>
            <button
              onClick={() => {
                setMode('sidebar');
                if (!activeSidebarNoteId && notes.length > 0) {
                  setActiveSidebarNoteId(notes[0].id);
                }
              }}
              className={`px-3 py-1 rounded font-medium transition-all flex items-center gap-1.5 ${
                mode === 'sidebar'
                  ? 'bg-blue-600 text-white shadow-sm'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              <SidebarIcon className="w-3.5 h-3.5" />
              Panel lateral
            </button>
          </div>

          {mode === 'sidebar' && (
            <div className="bg-slate-950 px-2 py-1 rounded-lg border border-slate-700 flex items-center gap-1 text-xs">
              <span className="text-slate-400 text-[11px] mr-1">Borde:</span>
              <button
                onClick={() => setSidebarEdge('left')}
                className={`px-2 py-0.5 rounded text-[11px] ${
                  sidebarEdge === 'left' ? 'bg-slate-700 text-white' : 'text-slate-400'
                }`}
              >
                Izq
              </button>
              <button
                onClick={() => setSidebarEdge('right')}
                className={`px-2 py-0.5 rounded text-[11px] ${
                  sidebarEdge === 'right' ? 'bg-slate-700 text-white' : 'text-slate-400'
                }`}
              >
                Der
              </button>
            </div>
          )}

          <button
            onClick={handleAddNote}
            className="bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold px-3 py-1.5 rounded-lg flex items-center gap-1.5 transition-colors shadow-sm"
          >
            <Plus className="w-3.5 h-3.5" />
            Nueva nota (Win+Alt+N)
          </button>
        </div>
      </div>

      {/* Desktop Canvas */}
      <div
        className="relative h-[560px] bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950/40 p-4 select-none overflow-hidden"
        onMouseMove={handleMouseMove}
        onMouseUp={handleMouseUp}
      >
        {/* Desktop Wallpaper Details */}
        <div className="absolute inset-0 opacity-10 bg-[radial-gradient(#38bdf8_1px,transparent_1px)] [background-size:24px_24px] pointer-events-none" />
        
        {/* Simulated System Info / Windows environment hints */}
        <div className="absolute top-4 left-4 text-[11px] font-mono text-slate-500 pointer-events-none flex flex-col gap-1">
          <span className="flex items-center gap-1.5">
            <Monitor className="w-3 h-3 text-slate-400" />
            Monitor 1: 1920x1080 (Escala 100%)
          </span>
          <span>BD Local: SQLite (EF Core 8.0) | Sincronización: Google Drive (appdata)</span>
        </div>

        {/* FLOATING MODE RENDERING */}
        {mode === 'floating' && (
          <>
            {notes.map((note) => {
              const colorCfg = NOTE_COLORS[note.color];
              const isColorMenuOpen = colorMenuNoteId === note.id;

              return (
                <div
                  key={note.id}
                  style={{
                    position: 'absolute',
                    left: `${note.positionX}px`,
                    top: `${note.positionY}px`,
                    width: `${note.width}px`,
                    backgroundColor: colorCfg.bgHex,
                    color: colorCfg.textColor,
                    zIndex: note.isAlwaysOnTop ? 30 : 10,
                  }}
                  className="rounded-xl shadow-xl border border-black/10 overflow-hidden flex flex-col transition-shadow hover:shadow-2xl"
                >
                  {/* Sticky Note Titlebar (WinUI 3 borderless drag region) */}
                  <div
                    style={{ backgroundColor: colorCfg.headerHex }}
                    className="px-2.5 py-1.5 flex items-center justify-between cursor-move text-xs border-b border-black/5"
                    onMouseDown={(e) => handleMouseDown(e, note)}
                  >
                    <div className="flex items-center gap-1">
                      <button
                        onClick={() => handleAddNote()}
                        title="Nueva nota (+)"
                        className="p-1 rounded hover:bg-black/10 transition-colors"
                      >
                        <Plus className="w-3.5 h-3.5" />
                      </button>
                      <button
                        onClick={() => toggleAlwaysOnTop(note.id)}
                        title={note.isAlwaysOnTop ? 'Siempre visible (activado)' : 'Fijar por encima'}
                        className={`p-1 rounded transition-colors ${
                          note.isAlwaysOnTop ? 'bg-black/15' : 'hover:bg-black/10'
                        }`}
                      >
                        {note.isAlwaysOnTop ? (
                          <Pin className="w-3.5 h-3.5 text-blue-900" />
                        ) : (
                          <PinOff className="w-3.5 h-3.5 opacity-60" />
                        )}
                      </button>
                    </div>

                    {/* Central editable title input in header */}
                    <div className="flex-1 mx-1.5 min-w-0" onMouseDown={(e) => e.stopPropagation()}>
                      <input
                        type="text"
                        value={note.title}
                        onChange={(e) => handleUpdateTitle(note.id, e.target.value)}
                        placeholder="Título..."
                        title="Haz clic para editar el título"
                        className="w-full bg-transparent outline-none font-semibold text-[11px] text-inherit placeholder-black/35 hover:bg-black/5 focus:bg-black/10 px-1 py-0.5 rounded truncate transition-colors"
                      />
                    </div>

                    <div className="flex items-center gap-1">
                      {/* Sync Status Badge */}
                      <span
                        title={note.syncStatus === 'synced' ? 'Sincronizado con Google Drive' : 'Guardado local (cambios pendientes)'}
                        className="mr-1"
                      >
                        {note.syncStatus === 'synced' ? (
                          <CloudCheck className="w-3.5 h-3.5 text-emerald-800/80" />
                        ) : (
                          <CloudUpload className="w-3.5 h-3.5 text-amber-800/80 animate-pulse" />
                        )}
                      </span>

                      {/* Color Menu Toggle */}
                      <div className="relative">
                        <button
                          onClick={() =>
                            setColorMenuNoteId(isColorMenuOpen ? null : note.id)
                          }
                          title="Colores"
                          className="p-1 rounded hover:bg-black/10 transition-colors"
                        >
                          <Palette className="w-3.5 h-3.5" />
                        </button>

                        {/* Color Picker Popover */}
                        {isColorMenuOpen && (
                          <div
                            style={{ backgroundColor: colorCfg.headerHex }}
                            className="absolute right-0 top-7 p-2 rounded-lg shadow-xl border border-black/15 flex gap-1.5 z-50"
                          >
                            {(Object.keys(NOTE_COLORS) as Array<NoteItem['color']>).map((cKey) => (
                              <button
                                key={cKey}
                                onClick={() => handleChangeColor(note.id, cKey)}
                                style={{ backgroundColor: NOTE_COLORS[cKey].bgHex }}
                                className={`w-5 h-5 rounded-full border border-black/20 hover:scale-110 transition-transform ${
                                  note.color === cKey ? 'ring-2 ring-blue-600 ring-offset-1' : ''
                                }`}
                                title={NOTE_COLORS[cKey].name}
                              />
                            ))}
                          </div>
                        )}
                      </div>

                      {/* Delete note */}
                      <button
                        onClick={() => handleDeleteNote(note.id)}
                        title="Eliminar nota"
                        className="p-1 rounded hover:bg-red-500/20 text-red-900/80 hover:text-red-900 transition-colors"
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  </div>

                  {/* Note Body (RichEditBox simulated) */}
                  <div className="p-3 flex-1 flex flex-col">
                    <textarea
                      value={note.content}
                      onChange={(e) => handleUpdateContent(note.id, e.target.value)}
                      className="w-full h-36 bg-transparent resize-none outline-none font-sans text-[13px] leading-relaxed placeholder-black/30"
                      placeholder="Escribe una nota..."
                    />
                  </div>

                  {/* Bottom Formatting Toolbar (Exact Microsoft Sticky Notes style) */}
                  <div
                    style={{ borderTopColor: 'rgba(0,0,0,0.08)' }}
                    className="px-2 py-1.5 border-t flex items-center justify-between text-xs opacity-75 hover:opacity-100 transition-opacity"
                  >
                    <div className="flex items-center gap-1">
                      <button className="p-1 rounded hover:bg-black/10 font-bold text-xs" title="Negrita (Ctrl+B)">
                        <Bold className="w-3.5 h-3.5" />
                      </button>
                      <button className="p-1 rounded hover:bg-black/10 text-xs" title="Cursiva (Ctrl+I)">
                        <Italic className="w-3.5 h-3.5" />
                      </button>
                      <button className="p-1 rounded hover:bg-black/10 text-xs" title="Subrayado (Ctrl+U)">
                        <Underline className="w-3.5 h-3.5" />
                      </button>
                      <button className="p-1 rounded hover:bg-black/10 text-xs" title="Tachado (Ctrl+T)">
                        <Strikethrough className="w-3.5 h-3.5" />
                      </button>
                      <button className="p-1 rounded hover:bg-black/10 text-xs" title="Lista de comprobación">
                        <CheckSquare className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  </div>
                </div>
              );
            })}
          </>
        )}

        {/* SIDEBAR MODE RENDERING (SideNotes / Noty style) */}
        {mode === 'sidebar' && (
          <div
            className={`absolute top-0 bottom-0 ${
              sidebarEdge === 'left' ? 'left-0' : 'right-0'
            } z-40 flex items-stretch transition-all duration-200 ease-out`}
            onMouseEnter={() => setSidebarHovered(true)}
            onMouseLeave={() => setSidebarHovered(false)}
          >
            {/* The Rest strip indicator (8-12px) */}
            <div
              className={`w-3.5 bg-slate-800/80 border-slate-700/60 hover:bg-blue-600/60 cursor-pointer flex flex-col justify-center items-center py-4 gap-2 transition-colors ${
                sidebarEdge === 'left' ? 'border-r' : 'border-l'
              }`}
              title="Pasa el mouse para desplegar el panel de notas"
            >
              {notes.map((n) => (
                <div
                  key={n.id}
                  style={{ backgroundColor: NOTE_COLORS[n.color].bgHex }}
                  className="w-2 h-5 rounded-full border border-black/20"
                />
              ))}
            </div>

            {/* Slide-out Drawer Panel */}
            <div
              className={`bg-slate-900/95 backdrop-blur-md border-slate-700 flex shadow-2xl overflow-hidden transition-all duration-300 ease-out ${
                isSidebarOpen ? 'w-[420px]' : 'w-0'
              } ${sidebarEdge === 'left' ? 'border-r flex-row' : 'border-l flex-row-reverse'}`}
            >
              {/* Vertical Tab Strip */}
              <div className="w-32 bg-slate-950/80 border-r border-slate-800 p-2 flex flex-col gap-2 overflow-y-auto">
                <div className="flex items-center justify-between pb-2 border-b border-slate-800">
                  <span className="text-[11px] font-semibold text-slate-400 uppercase tracking-wider">Notas</span>
                  <button
                    onClick={() => setIsSidebarPinned(!isSidebarPinned)}
                    title={isSidebarPinned ? 'Desanclar panel (ocultar por hover)' : 'Fijar panel abierto permanentemente'}
                    className={`p-1 rounded text-xs transition-colors ${
                      isSidebarPinned ? 'bg-blue-600 text-white' : 'text-slate-400 hover:bg-slate-800'
                    }`}
                  >
                    <Pin className="w-3 h-3" />
                  </button>
                </div>

                {notes.map((note) => {
                  const cfg = NOTE_COLORS[note.color];
                  const isSelected = (activeSidebarNoteId ?? notes[0]?.id) === note.id;

                  return (
                    <button
                      key={note.id}
                      onClick={() => setActiveSidebarNoteId(note.id)}
                      style={{
                        backgroundColor: isSelected ? cfg.bgHex : 'rgba(30, 41, 59, 0.6)',
                        color: isSelected ? cfg.textColor : '#94a3b8',
                        borderColor: isSelected ? cfg.borderHex : 'transparent',
                      }}
                      className="text-left p-2 rounded-lg border text-xs font-medium transition-all group relative overflow-hidden flex flex-col gap-1 shadow-sm"
                    >
                      <div className="flex items-center gap-1.5">
                        <span
                          style={{ backgroundColor: cfg.bgHex }}
                          className="w-2 h-2 rounded-full border border-black/20 shrink-0"
                        />
                        <span className="truncate font-semibold text-[11px]">
                          {note.title || note.content.split('\n')[0] || 'Nota sin título'}
                        </span>
                      </div>
                      <span className="text-[9px] opacity-70 line-clamp-1">
                        {note.title ? (note.content.split('\n')[0] || 'Sin contenido') : (note.content.split('\n')[1] || note.deviceId)}
                      </span>
                    </button>
                  );
                })}

                <button
                  onClick={handleAddNote}
                  className="mt-auto border border-dashed border-slate-700 hover:border-blue-500 text-slate-400 hover:text-blue-400 text-xs py-2 rounded-lg flex items-center justify-center gap-1 transition-colors"
                >
                  <Plus className="w-3 h-3" />
                  Nueva
                </button>
              </div>

              {/* Selected Note Content Area inside Sidebar */}
              <div className="flex-1 p-3 flex flex-col bg-slate-900">
                {(() => {
                  const currentNote = notes.find((n) => n.id === (activeSidebarNoteId ?? notes[0]?.id));
                  if (!currentNote) {
                    return (
                      <div className="h-full flex items-center justify-center text-slate-500 text-xs">
                        Selecciona o crea una nota
                      </div>
                    );
                  }
                  const colorCfg = NOTE_COLORS[currentNote.color];

                  return (
                    <div
                      style={{ backgroundColor: colorCfg.bgHex, color: colorCfg.textColor }}
                      className="h-full rounded-xl flex flex-col border border-black/10 overflow-hidden shadow-lg"
                    >
                      {/* Header with in-place editable title and contextual menu */}
                      <div
                        style={{ backgroundColor: colorCfg.headerHex }}
                        className="px-3 py-2 flex items-center justify-between gap-2 text-xs border-b border-black/10 relative"
                      >
                        <div className="flex-1 flex items-center gap-1.5 min-w-0">
                          <input
                            type="text"
                            value={currentNote.title}
                            onChange={(e) => handleUpdateTitle(currentNote.id, e.target.value)}
                            placeholder="Título de la nota (haz clic para editar)..."
                            title="Haz clic para editar el título"
                            className="w-full bg-transparent outline-none font-bold text-xs text-inherit placeholder-black/40 hover:bg-black/5 focus:bg-black/10 px-1.5 py-0.5 rounded transition-colors"
                          />
                        </div>

                        <div className="flex items-center gap-1 shrink-0">
                          {/* Sync status */}
                          <span
                            title={currentNote.syncStatus === 'synced' ? 'Sincronizado con Google Drive' : 'Guardado en SQLite (pendiente subir a Drive)'}
                            className="mr-0.5"
                          >
                            {currentNote.syncStatus === 'synced' ? (
                              <CloudCheck className="w-3.5 h-3.5 text-emerald-800/80" />
                            ) : (
                              <CloudUpload className="w-3.5 h-3.5 text-amber-800/80 animate-pulse" />
                            )}
                          </span>

                          {/* Lateral Note Menu Button */}
                          <div className="relative">
                            <button
                              onClick={() => setSideMenuOpen(!sideMenuOpen)}
                              className={`p-1 rounded transition-colors ${
                                sideMenuOpen ? 'bg-black/20' : 'hover:bg-black/10'
                              }`}
                              title="Menú de opciones de la nota"
                            >
                              <MoreVertical className="w-3.5 h-3.5" />
                            </button>

                            {/* Dropdown Menu */}
                            {sideMenuOpen && (
                              <div
                                style={{ backgroundColor: colorCfg.headerHex }}
                                className="absolute right-0 top-7 w-48 p-2 rounded-xl shadow-2xl border border-black/15 flex flex-col gap-2 z-50 text-xs"
                              >
                                <div>
                                  <span className="text-[10px] font-semibold text-black/60 uppercase tracking-wider block mb-1 px-1">
                                    Colores de nota
                                  </span>
                                  <div className="grid grid-cols-6 gap-1 bg-black/5 p-1.5 rounded-lg border border-black/10">
                                    {(Object.keys(NOTE_COLORS) as Array<NoteItem['color']>).map((cKey) => (
                                      <button
                                        key={cKey}
                                        onClick={() => {
                                          handleChangeColor(currentNote.id, cKey);
                                          setSideMenuOpen(false);
                                        }}
                                        style={{ backgroundColor: NOTE_COLORS[cKey].bgHex }}
                                        className={`w-5 h-5 rounded-full border border-black/20 hover:scale-110 transition-transform ${
                                          currentNote.color === cKey ? 'ring-2 ring-blue-600 ring-offset-1' : ''
                                        }`}
                                        title={NOTE_COLORS[cKey].name}
                                      />
                                    ))}
                                  </div>
                                </div>

                                <div className="border-t border-black/10 pt-1.5 flex flex-col gap-0.5">
                                  <button
                                    onClick={() => handleDetachToFloating(currentNote.id)}
                                    className="w-full text-left px-2 py-1.5 rounded hover:bg-black/10 flex items-center gap-2 text-inherit font-medium transition-colors"
                                  >
                                    <ExternalLink className="w-3.5 h-3.5 opacity-70" />
                                    <span>Abrir como ventana flotante</span>
                                  </button>

                                  <button
                                    onClick={() => handleDuplicateNote(currentNote)}
                                    className="w-full text-left px-2 py-1.5 rounded hover:bg-black/10 flex items-center gap-2 text-inherit font-medium transition-colors"
                                  >
                                    <Copy className="w-3.5 h-3.5 opacity-70" />
                                    <span>Duplicar nota</span>
                                  </button>
                                </div>

                                <div className="border-t border-black/10 pt-1.5 flex flex-col gap-0.5">
                                  <button
                                    onClick={() => {
                                      handleDeleteNote(currentNote.id);
                                      setSideMenuOpen(false);
                                    }}
                                    className="w-full text-left px-2 py-1.5 rounded hover:bg-red-500/20 text-red-900 font-medium flex items-center gap-2 transition-colors"
                                  >
                                    <Trash2 className="w-3.5 h-3.5 text-red-900" />
                                    <span>Eliminar nota</span>
                                  </button>
                                </div>
                              </div>
                            )}
                          </div>

                          <button
                            onClick={() => handleDeleteNote(currentNote.id)}
                            className="p-1 rounded hover:bg-red-500/20 text-red-900 transition-colors"
                            title="Eliminar nota"
                          >
                            <Trash2 className="w-3.5 h-3.5" />
                          </button>
                        </div>
                      </div>

                      {/* Content */}
                      <div className="p-3 flex-1 flex flex-col">
                        <textarea
                          value={currentNote.content}
                          onChange={(e) => handleUpdateContent(currentNote.id, e.target.value)}
                          className="w-full flex-1 bg-transparent resize-none outline-none font-sans text-[13px] leading-relaxed placeholder-black/30"
                          placeholder="Escribe aquí el contenido de la nota..."
                        />
                      </div>

                      {/* Bottom Formatting & Sync Toolbar */}
                      <div
                        style={{ borderTopColor: 'rgba(0,0,0,0.08)' }}
                        className="px-3 py-2 border-t flex items-center justify-between text-xs"
                      >
                        <div className="flex items-center gap-1">
                          <button className="p-1 rounded hover:bg-black/10 font-bold text-xs" title="Negrita (Ctrl+B)">
                            <Bold className="w-3.5 h-3.5" />
                          </button>
                          <button className="p-1 rounded hover:bg-black/10 text-xs" title="Cursiva (Ctrl+I)">
                            <Italic className="w-3.5 h-3.5" />
                          </button>
                          <button className="p-1 rounded hover:bg-black/10 text-xs" title="Subrayado (Ctrl+U)">
                            <Underline className="w-3.5 h-3.5" />
                          </button>
                          <button className="p-1 rounded hover:bg-black/10 text-xs" title="Tachado (Ctrl+T)">
                            <Strikethrough className="w-3.5 h-3.5" />
                          </button>
                          <button className="p-1 rounded hover:bg-black/10 text-xs" title="Lista de comprobación">
                            <CheckSquare className="w-3.5 h-3.5" />
                          </button>
                        </div>
                      </div>
                    </div>
                  );
                })()}
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
