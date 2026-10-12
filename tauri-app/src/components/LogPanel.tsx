import { useLogStore } from '../store';
import type { LogLevel } from '../types';

const levelColors: Record<LogLevel, string> = {
  info: '#475569',
  success: '#0f766e',
  warning: '#d97706',
  error: '#dc2626',
  processing: '#2563eb',
};

export function LogPanel() {
  const logs = useLogStore((state) => state.logs);

  return (
    <div className="log-panel">
      <div className="log-header">处理日志</div>
      <div className="log-content">
        {logs.map((log) => (
          <div
            key={log.id}
            className="log-item"
            style={{ color: levelColors[log.level] }}
          >
            <span className="log-time">
              {log.timestamp.toLocaleTimeString()}
            </span>
            <span className="log-text">{log.text}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
