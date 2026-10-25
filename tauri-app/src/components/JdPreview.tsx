import { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import { convertFileSrc, invoke } from "@tauri-apps/api/core";
import { Button } from "./ui/button";

/** 预览以输出宽度 790 像素为 100% 基准。 */
const OUTPUT_WIDTH = 790;
const MIN_ZOOM = 0.05;
const MAX_ZOOM = 2;
/** 按钮缩放的步进倍率，与原 C# 版本一致。 */
const ZOOM_STEP = 1.15;
/** 滚轮每 100 像素对应一次 ZOOM_STEP，触控板捏合时会更平滑。 */
const WHEEL_ZOOM_RATE = Math.log(ZOOM_STEP) / 100;
/** 可见区域上下额外预加载的距离。 */
const OVERSCAN_PX = 800;
/** 同时生成缩略图的数量上限，避免大图同时解码占满内存。 */
const MAX_CONCURRENT = 2;
/** 尺寸尚未读到时的占位高宽比。 */
const FALLBACK_RATIO = 1;

interface PreviewEntry {
  path: string;
  name: string;
}

type Thumbnail = { status: "ready"; url: string } | { status: "failed" };

// 缓存放在模块级：调整顺序、缩放、关闭后重新预览、切换页面都直接复用。
const sizeCache = new Map<string, { width: number; height: number } | null>();
const thumbnailCache = new Map<string, Thumbnail>();
const inflight = new Set<string>();
const sizeInflight = new Set<string>();
const listeners = new Set<() => void>();
let queue: string[] = [];

/** 用当前可见的图片替换等待队列，滚出视野且尚未开始的请求直接丢弃。 */
function requestThumbnails(paths: string[]) {
  queue = paths.filter((path) => !thumbnailCache.has(path) && !inflight.has(path));
  pumpQueue();
}

function pumpQueue() {
  while (inflight.size < MAX_CONCURRENT && queue.length > 0) {
    const path = queue.shift()!;
    if (thumbnailCache.has(path) || inflight.has(path)) continue;

    inflight.add(path);
    invoke<string>("jd_preview_thumbnail", { filePath: path })
      .then((thumbnail) =>
        thumbnailCache.set(path, { status: "ready", url: convertFileSrc(thumbnail) })
      )
      .catch(() => thumbnailCache.set(path, { status: "failed" }))
      .finally(() => {
        inflight.delete(path);
        listeners.forEach((listener) => listener());
        pumpQueue();
      });
  }
}

function clampZoom(value: number) {
  return Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, value));
}

/**
 * 京东详情页拼接预览：只渲染可见区域附近的图片，图片使用后台生成的缩略图。
 * 按住 Ctrl（macOS 也可用 ⌘）滚动滚轮或在触控板捏合即可缩放。
 */
export function JdPreview({ files }: { files: PreviewEntry[] }) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [viewport, setViewport] = useState({ width: 0, height: 0, scrollTop: 0 });
  // null 表示“适应宽度”，容器尺寸变化时自动跟随。
  const [manualZoom, setManualZoom] = useState<number | null>(null);
  const [, setCacheVersion] = useState(0);
  const [sizeVersion, setSizeVersion] = useState(0);
  const pendingScroll = useRef<{ top: number; left: number } | null>(null);
  const zoomRef = useRef(1);

  const refresh = useCallback(() => setCacheVersion((version) => version + 1), []);

  useEffect(() => {
    listeners.add(refresh);
    return () => {
      listeners.delete(refresh);
    };
  }, [refresh]);

  // 只为尚未读取过的图片请求尺寸（仅解析文件头，速度很快）。
  useEffect(() => {
    const missing = files
      .map((item) => item.path)
      .filter((path) => !sizeCache.has(path) && !sizeInflight.has(path));
    if (missing.length === 0) return;

    missing.forEach((path) => sizeInflight.add(path));
    invoke<[string, number, number][]>("read_image_sizes", { filePaths: missing })
      .then((sizes) => {
        sizes.forEach(([path, width, height]) => sizeCache.set(path, { width, height }));
        missing.forEach((path) => {
          if (!sizeCache.has(path)) sizeCache.set(path, null);
        });
      })
      .catch(() => missing.forEach((path) => sizeCache.set(path, null)))
      .finally(() => {
        missing.forEach((path) => sizeInflight.delete(path));
        setSizeVersion((version) => version + 1);
      });
  }, [files]);

  // 跟踪容器尺寸，用于“适应宽度”和可见区域计算。
  useEffect(() => {
    const element = containerRef.current;
    if (!element) return;

    const update = () =>
      setViewport({
        width: element.clientWidth,
        height: element.clientHeight,
        scrollTop: element.scrollTop,
      });
    update();

    const observer = new ResizeObserver(update);
    observer.observe(element);
    return () => observer.disconnect();
  }, []);

  const fitZoom = viewport.width > 0 ? clampZoom(Math.min(1, viewport.width / OUTPUT_WIDTH)) : 1;
  const zoom = manualZoom ?? fitZoom;
  const displayWidth = Math.round(OUTPUT_WIDTH * zoom);

  // 布局按 790 宽度的基准高度累加，再乘以缩放比例，避免逐项取整误差累积。
  const layout = useMemo(() => {
    let baseTop = 0;
    const items = files.map((item) => {
      const size = sizeCache.get(item.path);
      const ratio = size ? size.height / size.width : FALLBACK_RATIO;
      const baseHeight = Math.max(1, Math.round(OUTPUT_WIDTH * ratio));
      const entry = { ...item, top: baseTop * zoom, height: baseHeight * zoom };
      baseTop += baseHeight;
      return entry;
    });
    return { items, totalHeight: Math.round(baseTop * zoom) };
    // sizeVersion 变化代表 sizeCache 有新数据。
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [files, zoom, sizeVersion]);

  const visibleItems = useMemo(() => {
    const start = viewport.scrollTop - OVERSCAN_PX;
    const end = viewport.scrollTop + viewport.height + OVERSCAN_PX;
    return layout.items.filter((item) => item.top + item.height >= start && item.top <= end);
  }, [layout, viewport.scrollTop, viewport.height]);

  const visibleKey = visibleItems.map((item) => item.path).join("\n");
  useEffect(() => {
    requestThumbnails(visibleKey ? visibleKey.split("\n") : []);
  }, [visibleKey]);

  // 离开预览时清空等待队列，已开始的请求完成后仍会写入缓存。
  useEffect(() => () => requestThumbnails([]), []);

  /** 以容器内的 (anchorX, anchorY) 为锚点缩放，保持锚点下的内容不动。 */
  const zoomAt = useCallback((nextZoom: number, anchorX: number, anchorY: number) => {
    const element = containerRef.current;
    const previous = zoomRef.current;
    const next = clampZoom(nextZoom);
    if (!element || Math.abs(next - previous) < 1e-4) return;

    const offset = (zoomValue: number) =>
      Math.max(0, (element.clientWidth - OUTPUT_WIDTH * zoomValue) / 2);
    const contentX = (element.scrollLeft + anchorX - offset(previous)) / previous;
    const contentY = (element.scrollTop + anchorY) / previous;

    pendingScroll.current = {
      left: contentX * next + offset(next) - anchorX,
      top: contentY * next - anchorY,
    };
    zoomRef.current = next;
    setManualZoom(next);
  }, []);

  useLayoutEffect(() => {
    zoomRef.current = zoom;
    const element = containerRef.current;
    const scroll = pendingScroll.current;
    if (!element || !scroll) return;

    pendingScroll.current = null;
    element.scrollLeft = Math.max(0, scroll.left);
    element.scrollTop = Math.max(0, scroll.top);
    setViewport((prev) => ({ ...prev, scrollTop: element.scrollTop }));
  }, [zoom, manualZoom]);

  // 需要 passive: false 才能阻止 Ctrl + 滚轮触发整页缩放。
  useEffect(() => {
    const element = containerRef.current;
    if (!element) return;

    const onWheel = (event: WheelEvent) => {
      if (!event.ctrlKey && !event.metaKey) return;
      event.preventDefault();

      const rect = element.getBoundingClientRect();
      const delta = Math.max(-100, Math.min(100, event.deltaY));
      zoomAt(
        zoomRef.current * Math.exp(-delta * WHEEL_ZOOM_RATE),
        event.clientX - rect.left,
        event.clientY - rect.top
      );
    };

    element.addEventListener("wheel", onWheel, { passive: false });
    return () => element.removeEventListener("wheel", onWheel);
  }, [zoomAt]);

  const scrollFrame = useRef(0);
  const handleScroll = useCallback(() => {
    cancelAnimationFrame(scrollFrame.current);
    scrollFrame.current = requestAnimationFrame(() => {
      const element = containerRef.current;
      if (element) setViewport((prev) => ({ ...prev, scrollTop: element.scrollTop }));
    });
  }, []);

  const zoomFromCenter = (factor: number) => {
    const element = containerRef.current;
    if (!element) return;
    zoomAt(zoomRef.current * factor, element.clientWidth / 2, element.clientHeight / 2);
  };

  const fitWidth = () => {
    const element = containerRef.current;
    if (element) {
      const contentY = (element.scrollTop + element.clientHeight / 2) / zoomRef.current;
      pendingScroll.current = {
        left: 0,
        top: contentY * fitZoom - element.clientHeight / 2,
      };
    }
    zoomRef.current = fitZoom;
    setManualZoom(null);
  };

  return (
    <div className="flex min-h-0 flex-1 flex-col gap-2">
      <div
        className="flex items-center justify-between gap-2 text-[12px]"
        style={{ color: `hsl(var(--text-tertiary))` }}
      >
        <span>
          共 {files.length} 张 · 按住 Ctrl 滚动滚轮缩放
        </span>
        <div className="flex items-center gap-1.5">
          <Button
            variant="ghost"
            size="sm"
            className="h-7 w-7 px-0"
            onClick={() => zoomFromCenter(1 / ZOOM_STEP)}
            disabled={zoom <= MIN_ZOOM}
            title="缩小"
          >
            −
          </Button>
          <span
            className="w-11 text-center tabular-nums"
            style={{ color: `hsl(var(--text-primary))` }}
          >
            {Math.round(zoom * 100)}%
          </span>
          <Button
            variant="ghost"
            size="sm"
            className="h-7 w-7 px-0"
            onClick={() => zoomFromCenter(ZOOM_STEP)}
            disabled={zoom >= MAX_ZOOM}
            title="放大"
          >
            +
          </Button>
          <Button
            variant="ghost"
            size="sm"
            className="h-7 px-2"
            onClick={fitWidth}
            disabled={manualZoom === null}
          >
            适应宽度
          </Button>
        </div>
      </div>

      <div
        ref={containerRef}
        onScroll={handleScroll}
        className="min-h-0 flex-1 overflow-auto rounded-md transition-colors duration-200"
        style={{ backgroundColor: `hsl(var(--input-bg) / 0.5)` }}
      >
        <div
          className="relative"
          style={{ width: displayWidth, height: layout.totalHeight, margin: "0 auto" }}
        >
          {visibleItems.map((item) => (
            <PreviewTile
              key={item.path}
              path={item.path}
              name={item.name}
              top={item.top}
              height={item.height}
              onFailed={refresh}
            />
          ))}
        </div>
      </div>
    </div>
  );
}

function PreviewTile({
  path,
  name,
  top,
  height,
  onFailed,
}: {
  path: string;
  name: string;
  top: number;
  height: number;
  onFailed: () => void;
}) {
  const thumbnail = thumbnailCache.get(path);
  const style = { top, height } as const;

  if (thumbnail?.status === "ready") {
    return (
      <img
        src={thumbnail.url}
        alt={name}
        draggable={false}
        decoding="async"
        onError={() => {
          thumbnailCache.set(path, { status: "failed" });
          onFailed();
        }}
        className="absolute left-0 block w-full bg-white"
        style={style}
      />
    );
  }

  return (
    <div
      className="absolute left-0 flex w-full items-center justify-center overflow-hidden text-[12px]"
      style={{
        ...style,
        backgroundColor: `hsl(var(--muted))`,
        color: `hsl(var(--text-tertiary))`,
        borderBottom: `1px solid hsl(var(--border) / 0.4)`,
      }}
    >
      {thumbnail?.status === "failed" ? `无法加载 ${name}` : `正在生成预览 ${name}`}
    </div>
  );
}
