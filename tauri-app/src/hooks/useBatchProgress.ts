import { useCallback, useEffect, useRef, useState } from "react";
import { listen } from "@tauri-apps/api/event";
import type { ProgressEvent } from "@/types";

/** 完成后进度条保留的时间，便于用户看到 100%。 */
const HIDE_DELAY_MS = 2500;

/**
 * 订阅后端的批处理进度事件。
 * onFileStart 在每张图片开始处理时触发一次，用于写日志。
 */
export function useBatchProgress(
  eventName: string,
  onFileStart?: (event: ProgressEvent) => void
) {
  const [progress, setProgress] = useState<ProgressEvent | null>(null);
  const activeRef = useRef(false);
  const hideTimer = useRef<number | undefined>(undefined);
  const onFileStartRef = useRef(onFileStart);

  useEffect(() => {
    onFileStartRef.current = onFileStart;
  }, [onFileStart]);

  useEffect(() => {
    let unlisten: (() => void) | undefined;
    let cancelled = false;

    listen<ProgressEvent>(eventName, (event) => {
      // 任务结束后才到达的事件直接丢弃，避免进度条回退。
      if (!activeRef.current) return;
      setProgress(event.payload);
      if (event.payload.stage === "读取") {
        onFileStartRef.current?.(event.payload);
      }
    })
      .then((fn) => {
        if (cancelled) fn();
        else unlisten = fn;
      })
      .catch(() => {});

    return () => {
      cancelled = true;
      unlisten?.();
      window.clearTimeout(hideTimer.current);
    };
  }, [eventName]);

  /** 开始一批任务，立即显示 0% 的进度条。 */
  const start = useCallback((total: number) => {
    window.clearTimeout(hideTimer.current);
    activeRef.current = true;
    setProgress({ current: 0, total, file_name: "", percent: 0, stage: "准备中" });
  }, []);

  /** 结束一批任务，显示最终状态后自动隐藏。 */
  const finish = useCallback((stage: string, completed = true) => {
    activeRef.current = false;
    setProgress((prev) =>
      prev
        ? {
            ...prev,
            current: completed ? prev.total : prev.current,
            percent: completed ? 100 : prev.percent,
            stage,
          }
        : null
    );
    window.clearTimeout(hideTimer.current);
    hideTimer.current = window.setTimeout(() => setProgress(null), HIDE_DELAY_MS);
  }, []);

  return { progress, start, finish };
}
