import { create } from 'zustand';
import type { LogMessage, LogLevel } from './types';

interface LogStore {
  logs: LogMessage[];
  addLog: (text: string, level: LogLevel) => void;
  clearLogs: () => void;
}

export const useLogStore = create<LogStore>((set) => ({
  logs: [],
  addLog: (text, level) => {
    const log: LogMessage = {
      id: `${Date.now()}-${Math.random()}`,
      text,
      level,
      timestamp: new Date(),
    };
    set((state) => ({ logs: [...state.logs, log] }));
  },
  clearLogs: () => set({ logs: [] }),
}));
