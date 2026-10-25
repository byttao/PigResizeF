import { useEffect, useRef } from "react";
import { LOG_STYLE } from "@/hooks/useLogger";
import type { LogEntry } from "@/types";

interface LogViewerProps {
  logs: LogEntry[];
}

/** 彩色处理日志面板，自动滚动到最新记录。 */
export function LogViewer({ logs }: LogViewerProps) {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const container = containerRef.current;
    if (container) {
      container.scrollTop = container.scrollHeight;
    }
  }, [logs]);

  return (
    <div
      ref={containerRef}
      data-ui-selectable
      className="log-container w-full font-mono text-[12px] leading-[1.7] overflow-auto rounded-md p-3 transition-colors duration-200"
      style={{
        backgroundColor: `hsl(var(--input-bg) / 0.6)`,
        color: `hsl(var(--text-primary))`,
      }}
    >
      {logs.length === 0 ? (
        <p style={{ color: `hsl(var(--text-tertiary))` }}>暂无日志</p>
      ) : (
        logs.map((log) => {
          const style = LOG_STYLE[log.level];
          return (
            <div key={log.id} className="flex gap-2 whitespace-pre-wrap">
              <span style={{ color: `hsl(var(--text-tertiary))` }}>{log.time}</span>
              <span style={{ color: style.color }} className="font-semibold">
                {style.prefix}
              </span>
              <span style={{ color: `hsl(var(--text-primary))` }}>{log.message}</span>
            </div>
          );
        })
      )}
    </div>
  );
}
