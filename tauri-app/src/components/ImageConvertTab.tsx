import { useCallback, useState } from "react";
import { open } from "@tauri-apps/plugin-dialog";
import { invoke } from "@tauri-apps/api/core";
import { Button } from "./ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "./ui/card";
import { ProgressPanel } from "./ProgressPanel";
import { LogViewer } from "./LogViewer";
import { DropZone } from "./DropZone";
import { useLogger } from "@/hooks/useLogger";
import { useFileDrop } from "@/hooks/useFileDrop";
import { useBatchProgress } from "@/hooks/useBatchProgress";
import type { BatchResult, ResizePreset } from "@/types";

const PRESETS: { value: ResizePreset; label: string }[] = [
  { value: "Square1440", label: "1440方图" },
  { value: "Tall1440", label: "1440长图" },
  { value: "Square800", label: "800方图" },
  { value: "ProductTall", label: "商品长图" },
];

export function ImageConvertTab() {
  const [files, setFiles] = useState<string[]>([]);
  const [preset, setPreset] = useState<ResizePreset>("Square1440");
  const [processing, setProcessing] = useState(false);
  const { logs, addLog } = useLogger();
  const { progress, start, finish } = useBatchProgress("resize-progress", (event) =>
    addLog(`[*] 正在处理: ${event.file_name}`, "processing")
  );

  /** 追加文件，自动去重。 */
  const appendFiles = useCallback(
    (paths: string[]) => {
      setFiles((prev) => {
        const existing = new Set(prev);
        const added = paths.filter((path) => !existing.has(path));
        if (added.length > 0) {
          addLog(`成功追加 ${added.length} 张图片。`, "info");
          added.forEach((path) => addLog(`  + ${splitName(path)}`, "info"));
          addLog(`当前列表共有 ${prev.length + added.length} 张图片。`, "info");
          return [...prev, ...added];
        }
        return prev;
      });
    },
    [addLog]
  );

  // 拖入文件时追加到列表末尾，已存在的路径不重复加入。
  const handleDropped = useCallback(
    (paths: string[]) => appendFiles(paths),
    [appendFiles]
  );

  const { isDragging, setDragging } = useFileDrop(handleDropped);


  const handleSelectFiles = useCallback(async () => {
    const selected = await open({
      multiple: true,
      filters: [{ name: "Images", extensions: ["jpg", "jpeg", "png", "bmp", "gif"] }],
    });

    if (!selected) return;
    const paths = Array.isArray(selected) ? selected : [selected];
    appendFiles(paths);
  }, [appendFiles]);

  const handleConvert = useCallback(async () => {
    if (files.length === 0) {
      addLog("请先拖拽图片或指定文件夹。", "error");
      return;
    }

    setProcessing(true);
    start(files.length);
    addLog("开始处理图片...", "info");

    try {
      const result = await invoke<BatchResult>("batch_resize", { files, preset });

      result.failed_files.forEach((path) => {
        addLog(`    [!] 处理失败 - ${splitName(path)}`, "error");
      });
      addLog(result.message, result.success ? "success" : "warning");
      finish(
        result.success ? "转换完成" : `转换完成，${result.failed_files.length} 张失败`
      );

      if (result.success) {
        setFiles([]);
      } else {
        setFiles((prev) => prev.filter((path) => !result.failed_files.includes(path)));
      }
    } catch (error) {
      addLog(`处理失败：${error}`, "error");
      finish("转换中断", false);
    } finally {
      setProcessing(false);
    }
  }, [files, preset, addLog, start, finish]);

  const handleClear = useCallback(() => {
    setFiles([]);
    addLog("图片列表已清空。", "info");
  }, [addLog]);

  return (
    <div className="flex h-full flex-col gap-4 overflow-hidden p-4">
      <div className="grid min-h-0 flex-1 grid-cols-[1fr_240px] gap-4">
        <Card className="flex min-h-0 flex-col">
          <CardHeader>
            <CardTitle>待转换图片</CardTitle>
          </CardHeader>
          <CardContent className="flex min-h-0 flex-1 flex-col gap-3">
            <DropZone
              isDragging={isDragging}
              onDragOver={() => setDragging(true)}
              onDragLeave={() => setDragging(false)}
              onClick={handleSelectFiles}
            />

            <div
              className="min-h-0 flex-1 overflow-auto rounded-md transition-colors duration-200"
              style={{ backgroundColor: `hsl(var(--input-bg) / 0.4)` }}
            >
              {files.length === 0 ? (
                <p
                  className="p-4 text-center text-[13px]"
                  style={{ color: `hsl(var(--text-tertiary))` }}
                >
                  暂无图片
                </p>
              ) : (
                <ul className="p-1">
                  {files.map((path, index) => (
                    <li
                      key={`${path}-${index}`}
                      className="flex items-center gap-2 rounded px-2 py-1.5 text-[13px] transition-all duration-200"
                      style={{ color: `hsl(var(--text-primary))` }}
                    >
                      <span className="w-5 text-right" style={{ color: `hsl(var(--text-tertiary))` }}>
                        {index + 1}.
                      </span>
                      <span className="truncate" title={path}>
                        {splitName(path)}
                      </span>
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div className="flex gap-2">
              <Button onClick={handleSelectFiles} disabled={processing}>
                选择图片
              </Button>
              <Button
                variant="ghost"
                onClick={handleClear}
                disabled={processing || files.length === 0}
              >
                清空列表
              </Button>
            </div>
          </CardContent>
        </Card>

        <Card className="flex flex-col">
          <CardHeader>
            <CardTitle>转换样式</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-1 flex-col gap-4">
            <div className="flex flex-col gap-1">
              {PRESETS.map((item) => (
                <label
                  key={item.value}
                  className="flex cursor-pointer items-center gap-2 rounded px-2 py-1.5 text-[13px] transition-all duration-200"
                  style={
                    preset === item.value
                      ? {
                          backgroundColor: `hsl(var(--active-bg))`,
                          color: `hsl(var(--text-primary))`,
                        }
                      : {
                          color: `hsl(var(--text-secondary))`,
                        }
                  }
                >
                  <input
                    type="radio"
                    name="preset"
                    checked={preset === item.value}
                    onChange={() => setPreset(item.value)}
                    disabled={processing}
                    className="accent-orange-500"
                  />
                  {item.label}
                </label>
              ))}
            </div>

            <div className="mt-auto flex flex-col gap-3">
              <ProgressPanel progress={progress} />

              <Button onClick={handleConvert} disabled={processing || files.length === 0}>
                {processing ? "转换中..." : "转换"}
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>

      <Card className="flex h-[180px] shrink-0 flex-col">
        <CardHeader className="py-2">
          <CardTitle>处理日志</CardTitle>
        </CardHeader>
        <CardContent className="flex min-h-0 flex-1 py-2">
          <LogViewer logs={logs} />
        </CardContent>
      </Card>
    </div>
  );
}

/** 从路径中取出文件名。 */
function splitName(path: string): string {
  const parts = path.split(/[/\\]/);
  return parts[parts.length - 1] || path;
}
