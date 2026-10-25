import { useCallback, useEffect, useRef, useState } from "react";
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
import type {
  BatchResult,
  CropJob,
  CropMode,
  CropModeOption,
} from "@/types";

interface CropEntry {
  path: string;
  name: string;
  mode: CropMode;
}

const FALLBACK_MODES: CropModeOption[] = [
  { value: "auto", label: "自动识别" },
  { value: "randomcorners", label: "四角乱切" },
  { value: "grid2", label: "四宫格拆分" },
  { value: "grid3", label: "九宫格拆分" },
];

export function ImageCropTab() {
  const [files, setFiles] = useState<CropEntry[]>([]);
  const [modeOptions, setModeOptions] = useState<CropModeOption[]>(FALLBACK_MODES);
  const [globalMode, setGlobalMode] = useState<CropMode>("auto");
  const [quickMode, setQuickMode] = useState(false);
  const [useCustomOutput, setUseCustomOutput] = useState(false);
  const [customOutputDir, setCustomOutputDir] = useState("");
  const [processing, setProcessing] = useState(false);
  const { logs, addLog } = useLogger();
  const { progress, start, finish } = useBatchProgress("crop-progress", (event) =>
    addLog(`[*] 正在处理: ${event.file_name}`, "processing")
  );

  const filesRef = useRef<CropEntry[]>([]);
  filesRef.current = files;

  useEffect(() => {
    invoke<CropModeOption[]>("crop_mode_options")
      .then((options) => {
        if (options.length > 0) {
          setModeOptions(options);
        }
      })
      .catch(() => {});
  }, []);

  const runCrop = useCallback(
    async (targets: CropEntry[]) => {
      const jobs = targets.length > 0 ? targets : files;
      if (jobs.length === 0) {
        addLog("请先拖拽图片到列表。", "warning");
        return;
      }

      setProcessing(true);
      start(jobs.length);
      addLog(`图片裁切：开始处理 ${jobs.length} 张图片。`, "processing");

      try {
        const payload: CropJob[] = jobs.map((item) => ({
          path: item.path,
          mode: item.mode,
        }));

        const result = await invoke<BatchResult>("batch_crop", {
          jobs: payload,
          outputDir: useCustomOutput ? customOutputDir : null,
        });

        result.failed_files.forEach((path) => {
          addLog(`    [!] 处理失败 - ${splitName(path)}`, "error");
        });
        addLog(result.message, result.success ? "success" : "warning");
        finish(
          result.success
            ? `裁切完成，输出 ${result.output_count} 张`
            : `裁切完成，${result.failed_files.length} 张失败`
        );

        if (result.success) {
          setFiles([]);
        } else {
          setFiles((prev) =>
            prev.filter((item) => !result.failed_files.includes(item.path))
          );
        }
      } catch (error) {
        addLog(`图片裁切失败：${error}`, "error");
        finish("裁切中断", false);
      } finally {
        setProcessing(false);
      }
    },
    [files, useCustomOutput, customOutputDir, addLog, start, finish]
  );

  const resolveModes = useCallback(
    async (paths: string[], fallback: CropMode): Promise<CropEntry[]> => {
      const entries: CropEntry[] = [];

      for (const path of paths) {
        let mode = fallback;
        if (fallback === "auto") {
          try {
            mode = await invoke<CropMode>("detect_crop_mode_command", {
              filePath: path,
            });
          } catch {
            mode = "randomcorners";
          }
        }
        entries.push({ path, name: splitName(path), mode });
      }

      return entries;
    },
    []
  );

  const addPaths = useCallback(
    async (paths: string[]) => {
      const freshPaths = paths.filter(
        (path) => !filesRef.current.some((item) => item.path === path)
      );

      if (freshPaths.length === 0) {
        return;
      }

      const entries = await resolveModes(freshPaths, globalMode);
      setFiles((prev) => [...prev, ...entries]);
      addLog(`图片裁切：已添加 ${entries.length} 张图片。`, "info");

      if (quickMode) {
        void runCrop(entries);
      }
    },
    [globalMode, quickMode, resolveModes, runCrop, addLog]
  );

  const handleDropped = useCallback(
    (paths: string[]) => {
      void addPaths(paths);
    },
    [addPaths]
  );

  const { isDragging, setDragging } = useFileDrop(handleDropped);


  const handleSelectFiles = useCallback(async () => {
    const selected = await open({
      multiple: true,
      filters: [{ name: "Images", extensions: ["jpg", "jpeg", "png", "bmp", "gif"] }],
    });

    if (!selected) return;
    const paths = Array.isArray(selected) ? selected : [selected];
    await addPaths(paths);
  }, [addPaths]);

  const handleBrowseOutput = useCallback(async () => {
    const selected = await open({ directory: true });
    if (typeof selected === "string") {
      setCustomOutputDir(selected);
    }
  }, []);

  const handleGlobalModeChange = useCallback(
    async (mode: CropMode) => {
      setGlobalMode(mode);

      if (mode !== "auto") {
        setFiles((prev) => prev.map((item) => ({ ...item, mode })));
        return;
      }

      const updated = await Promise.all(
        files.map(async (item) => {
          try {
            const detected = await invoke<CropMode>("detect_crop_mode_command", {
              filePath: item.path,
            });
            return { ...item, mode: detected };
          } catch {
            return { ...item, mode: "randomcorners" as CropMode };
          }
        })
      );
      setFiles(updated);
    },
    [files]
  );

  const handleItemModeChange = useCallback((index: number, mode: CropMode) => {
    setFiles((prev) =>
      prev.map((item, i) => (i === index ? { ...item, mode } : item))
    );
  }, []);

  return (
    <div className="flex h-full flex-col gap-4 overflow-hidden p-4">
      <div className="grid min-h-0 flex-1 grid-cols-[1fr_214px] gap-4">
        <Card className="flex min-h-0 flex-col">
          <CardHeader>
            <CardTitle>待裁切图片</CardTitle>
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
                <table className="w-full text-[13px]">
                  <thead
                    className="sticky top-0 backdrop-blur transition-colors duration-200"
                    style={{ backgroundColor: `hsl(var(--card-bg) / 0.95)` }}
                  >
                    <tr>
                      <th
                        className="px-3 py-2 text-left font-semibold"
                        style={{ color: `hsl(var(--text-primary))` }}
                      >
                        图片文件
                      </th>
                      <th
                        className="w-[130px] px-3 py-2 text-left font-semibold"
                        style={{ color: `hsl(var(--text-primary))` }}
                      >
                        识别模式
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {files.map((item, index) => (
                      <tr
                        key={item.path}
                        className="transition-all duration-200"
                        style={{ borderTop: `1px solid hsl(var(--border) / 0.5)` }}
                      >
                        <td
                          className="px-3 py-1.5"
                          style={{ color: `hsl(var(--text-primary))` }}
                          title={item.path}
                        >
                          {item.name}
                        </td>
                        <td className="px-3 py-1.5">
                          <select
                            value={item.mode}
                            onChange={(e) =>
                              handleItemModeChange(index, e.target.value as CropMode)
                            }
                            disabled={processing}
                            className="w-full rounded border px-2 py-1 text-[13px] transition-all duration-200 focus:border-orange-500 focus:outline-none"
                            style={{
                              borderColor: `hsl(var(--border))`,
                              backgroundColor: `hsl(var(--input-bg))`,
                              color: `hsl(var(--text-primary))`,
                            }}
                          >
                            {modeOptions.map((option) => (
                              <option key={option.value} value={option.value}>
                                {option.label}
                              </option>
                            ))}
                          </select>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </CardContent>
        </Card>

        <div className="flex min-h-0 flex-col gap-4">
          <Card className="flex flex-col">
            <CardHeader>
              <CardTitle>裁切模式</CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col gap-1">
              {modeOptions.map((option) => (
                <label
                  key={option.value}
                  className="flex cursor-pointer items-center gap-2 rounded px-2 py-1.5 text-[13px] transition-all duration-200"
                  style={
                    globalMode === option.value
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
                    name="cropMode"
                    checked={globalMode === option.value}
                    onChange={() => void handleGlobalModeChange(option.value)}
                    disabled={processing}
                    className="accent-orange-500"
                  />
                  {option.label}
                </label>
              ))}

              <label
                className="mt-2 flex cursor-pointer items-center gap-2 rounded px-2 py-1.5 text-[13px] transition-all duration-200"
                style={{ color: `hsl(var(--text-secondary))` }}
              >
                <input
                  type="checkbox"
                  checked={quickMode}
                  onChange={(e) => setQuickMode(e.target.checked)}
                  disabled={processing}
                  className="accent-orange-500"
                />
                快速模式
              </label>
            </CardContent>
          </Card>

          <Card className="flex flex-col">
            <CardHeader>
              <CardTitle>保存设置</CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col gap-2">
              <label
                className="flex cursor-pointer items-center gap-2 text-[13px] transition-all duration-200"
                style={{ color: `hsl(var(--text-secondary))` }}
              >
                <input
                  type="checkbox"
                  checked={useCustomOutput}
                  onChange={(e) => setUseCustomOutput(e.target.checked)}
                  disabled={processing}
                  className="accent-orange-500"
                />
                自定义保存路径
              </label>

              {useCustomOutput && (
                <>
                  <div
                    className="truncate rounded border px-2 py-1 text-[12px] transition-colors duration-200"
                    style={{
                      borderColor: `hsl(var(--border))`,
                      backgroundColor: `hsl(var(--input-bg) / 0.4)`,
                      color: `hsl(var(--text-secondary))`,
                    }}
                    title={customOutputDir}
                  >
                    {customOutputDir || "未选择目录"}
                  </div>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={handleBrowseOutput}
                    disabled={processing}
                  >
                    选择文件夹
                  </Button>
                </>
              )}
            </CardContent>
          </Card>

          {progress && (
            <Card className="p-3">
              <ProgressPanel progress={progress} />
            </Card>
          )}

          <Button
            className="mt-auto"
            onClick={() => void runCrop([])}
            disabled={processing || files.length === 0}
          >
            {processing ? "裁切中..." : "开始裁切"}
          </Button>
        </div>
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

function splitName(path: string): string {
  const parts = path.split(/[/\\]/);
  return parts[parts.length - 1] || path;
}
