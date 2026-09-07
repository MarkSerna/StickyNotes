export type DisplayMode = 'floating' | 'sidebar';
export type SidebarEdge = 'left' | 'right';

export interface NoteItem {
  id: string;
  title: string;
  content: string;
  color: 'yellow' | 'green' | 'pink' | 'purple' | 'blue' | 'gray';
  positionX: number;
  positionY: number;
  width: number;
  height: number;
  monitor: number;
  createdAt: string;
  updatedAt: string;
  deletedAt: string | null;
  deviceId: string;
  syncStatus: 'synced' | 'pending' | 'conflict';
  isAlwaysOnTop?: boolean;
}

export interface NoteColorConfig {
  id: NoteItem['color'];
  name: string;
  bgHex: string;
  headerHex: string;
  borderHex: string;
  textColor: string;
}

export interface SolutionStep {
  id: number;
  title: string;
  status: 'current' | 'upcoming' | 'ready';
  description: string;
  technologies: string[];
}
