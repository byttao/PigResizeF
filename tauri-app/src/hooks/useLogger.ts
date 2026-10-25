import { useCallback, useRef, useState } from "react";
import type { LogEntry, LogLevel } from "@/types";

/** 日志级别对应的前缀与颜色（深色主题下的高亮色）。 */
export const LOG_STYLE: Record<LogLevel, { prefix: string; color: string }> = {
  info: { prefix: "[INFO]", color: "#60a5fa" },
  success: { prefix: "[SUCCESS]", color: "#4ade80" },
  warning: { prefix: "[WARN]", color: "#fb923c" },
  error: { prefix: "[ERROR]", color: "#f87171" },
  processing: { prefix: "[PROCESS]", color: "#c084fc" },
};

export function useLogger() {
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const counter = useRef(0);

  const addLog = useCallback((message: string, level: LogLevel = "info") => {
    const now = new Date();
    const time = `${String(now.getHours()).padStart(2, "0")}:${String(
      now.getMinutes()
    ).padStart(2, "0")}:${String(now.getSeconds()).padStart(2, "0")}`;

    counter.current += 1;
    const entry: LogEntry = {
      id: `${Date.now()}-${counter.current}`,
      time,
      level,
      message,
    };

    setLogs((prev) => [...prev, entry]);
  }, []);

  const clearLogs = useCallback(() => setLogs([]), []);

  return { logs, addLog, clearLogs };
}
