import { useCallback, useEffect, useRef, useState } from "react";
import { getCurrentWebview } from "@tauri-apps/api/webview";

/** 常见的图片扩展名白名单，与原 C# 版本的 IsImageFile 保持一致。 */
const IMAGE_EXTENSIONS = [".jpg", ".jpeg", ".png", ".bmp", ".gif"];

/** 判断路径是否为受支持的图片。 */
export function isImageFile(path: string): boolean {
  const lower = path.toLowerCase();
  return IMAGE_EXTENSIONS.some((ext) => lower.endsWith(ext));
}

/**
 * Tauri 2.0 的文件拖拽。
 *
 * 必须使用 `getCurrentWebview().onDragDropEvent`：
 * HTML5 的 `dataTransfer.files` 在 WebView 中拿不到本地绝对路径，
 * 只有该事件才会提供真实路径。
 */
export function useFileDrop(onDrop: (paths: string[]) => void) {
  const [isDragging, setIsDragging] = useState(false);
  const handler = useRef(onDrop);
  handler.current = onDrop;

  useEffect(() => {
    let unlisten: (() => void) | undefined;
    let cancelled = false;

    getCurrentWebview()
      .onDragDropEvent((event) => {
        const payload = event.payload;

        if (payload.type === "enter") {
          setIsDragging(true);
          return;
        }

        if (payload.type === "leave") {
          setIsDragging(false);
          return;
        }

        if (payload.type === "drop") {
          setIsDragging(false);
          const paths = payload.paths.filter(isImageFile);
          if (paths.length > 0) {
            handler.current(paths);
          }
        }
      })
      .then((fn) => {
        if (cancelled) {
          fn();
        } else {
          unlisten = fn;
        }
      })
      .catch(() => {
        // 非 Tauri 环境（例如浏览器预览）忽略拖拽事件。
      });

    return () => {
      cancelled = true;
      unlisten?.();
    };
  }, []);

  /** HTML5 拖拽的视觉状态开关。 */
  const setDragging = useCallback((value: boolean) => setIsDragging(value), []);

  return { isDragging, setDragging };
}
