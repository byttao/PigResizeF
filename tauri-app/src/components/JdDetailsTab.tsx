import { useCallback, useEffect, useMemo, useState } from "react";
import { open } from "@tauri-apps/plugin-dialog";
import { invoke } from "@tauri-apps/api/core";
import { Button } from "./ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "./ui/card";
import { JdPreview } from "./JdPreview";
import { LogViewer } from "./LogViewer";
import { ProgressPanel } from "./ProgressPanel";
import { DropZone } from "./DropZone";
import { useLogger } from "@/hooks/useLogger";
import { useFileDrop } from "@/hooks/useFileDrop";
import { useBatchProgress } from "@/hooks/useBatchProgress";
import type { BatchResult } from "@/types";

interface JdEntry {
  path: string;
  name: string;
}

export function JdDetailsTab() {
  const [files, setFiles] = useState<JdEntry[]>([]);
  const [selected, setSelected] = useState<Set<number>>(new Set());
  const [previewReady, setPreviewReady] = useState(false);
  const [processing, setProcessing] = useState(false);
  const { logs, addLog } = useLogger();
  const { progress, start, finish } = useBatchProgress("jd-progress", (event) =>
    addLog(`[*] 正在处理: ${event.file_name}`, "processing")
  );

  const appendFiles = useCallback(
    (paths: string[]) => {
      setFiles((prev) => {
        const existing = new Set(prev.map((item) => item.path));
        const fresh = paths.filter((path) => !existing.has(path));

        if (fresh.length === 0) {
          return prev;
        }

        const sorted = [...fresh].sort((a, b) =>
          splitName(a).localeCompare(splitName(b), "zh-CN", { sensitivity: "base" })
        );

        addLog(`京东详情页：已添加 ${sorted.length} 张图片。`, "info");
        return [...prev, ...sorted.map((path) => ({ path, name: splitName(path) }))];
      });
    },
    [addLog]
  );

  const handleDropped = useCallback(
    (paths: string[]) => {
      appendFiles(paths);
      setPreviewReady(false);
    },
    [appendFiles]
  );

  const { isDragging, setDragging } = useFileDrop(handleDropped);


  const handleSelectFiles = useCallback(async () => {
    const selectedPaths = await open({
      multiple: true,
      filters: [{ name: "Images", extensions: ["jpg", "jpeg", "png", "bmp", "gif"] }],
    });

    if (!selectedPaths) return;
    const paths = Array.isArray(selectedPaths) ? selectedPaths : [selectedPaths];
    appendFiles(paths);
    setPreviewReady(false);
  }, [appendFiles]);

  const handleItemClick = useCallback(
    (index: number, event: React.MouseEvent) => {
      setSelected((prev) => {
        const next = new Set(prev);

        if (event.shiftKey && files.length > 0) {
          const anchor = prev.size > 0 ? Math.min(...prev) : 0;
          const [start, end] = anchor < index ? [anchor, index] : [index, anchor];
          for (let i = start; i <= end; i += 1) {
            next.add(i);
          }
          return next;
        }

        if (event.ctrlKey || event.metaKey) {
          if (next.has(index)) {
            next.delete(index);
          } else {
            next.add(index);
          }
          return next;
        }

        return new Set([index]);
      });
    },
    [files.length]
  );

  const moveUp = useCallback(() => {
    const indices = [...selected].sort((a, b) => a - b);
    if (indices.length === 0 || indices[0] === 0) return;

    setFiles((prev) => {
      const next = [...prev];
      indices.forEach((index) => {
        [next[index - 1], next[index]] = [next[index], next[index - 1]];
      });
      return next;
    });

    setSelected((prev) => new Set([...prev].map((index) => index - 1)));
  }, [selected]);

  const moveDown = useCallback(() => {
    const indices = [...selected].sort((a, b) => a - b);
    if (indices.length === 0 || indices[indices.length - 1] === files.length - 1) return;

    setFiles((prev) => {
      const next = [...prev];
      [...indices].reverse().forEach((index) => {
        [next[index], next[index + 1]] = [next[index + 1], next[index]];
      });
      return next;
    });

    setSelected((prev) => new Set([...prev].map((index) => index + 1)));
  }, [selected, files.length]);

  const deleteSelected = useCallback(() => {
    const indices = [...selected].sort((a, b) => a - b);
    if (indices.length === 0) return;

    setFiles((prev) => prev.filter((_, index) => !selected.has(index)));

    const nextIndex = Math.min(indices[0], files.length - indices.length - 1);
    setSelected(nextIndex >= 0 ? new Set([nextIndex]) : new Set());
  }, [selected, files.length]);

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Delete" && selected.size > 0) {
        event.preventDefault();
        deleteSelected();
      }
    };

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [deleteSelected, selected.size]);

  const handlePreview = useCallback(() => {
    if (files.length === 0) {
      addLog("请先拖拽详情图片到列表。", "warning");
      return;
    }
    setPreviewReady(true);
    addLog(`京东详情页：已加载 ${files.length} 张图片预览。`, "info");
  }, [files.length, addLog]);

  const handleExport = useCallback(async () => {
    if (files.length === 0) {
      addLog("请先拖拽详情图片到列表。", "warning");
      return;
    }

    setProcessing(true);
    start(files.length);
    addLog(`京东详情页：开始输出 ${files.length} 张图片。`, "processing");

    try {
      const outputDir = files[0].path.replace(/[/\\][^/\\]*$/, "");

      const result = await invoke<BatchResult>("jd_export", {
        filePaths: files.map((item) => item.path),
        outputDir,
      });

      addLog(result.message, "success");
      finish(`输出完成，共 ${result.output_count} 张`);
      setFiles([]);
      setSelected(new Set());
      setPreviewReady(false);
    } catch (error) {
      addLog(`京东详情页输出失败：${error}`, "error");
      finish("输出中断", false);
    } finally {
      setProcessing(false);
    }
  }, [files, addLog, start, finish]);

  const selectedIndices = useMemo(() => [...selected].sort((a, b) => a - b), [selected]);
  const canMoveUp = selectedIndices.length > 0 && selectedIndices[0] > 0;
  const canMoveDown =
    selectedIndices.length > 0 && selectedIndices[selectedIndices.length - 1] < files.length - 1;

  return (
    <div className="flex h-full flex-col gap-4 overflow-hidden p-4">
      <div className="grid min-h-0 flex-1 grid-cols-[280px_1fr] gap-4">
        <Card className="flex min-h-0 flex-col">
          <CardHeader>
            <CardTitle>详情图片</CardTitle>
          </CardHeader>
          <CardContent className="flex min-h-0 flex-1 flex-col gap-3">
            <DropZone
              isDragging={isDragging}
              onDragOver={() => setDragging(true)}
              onDragLeave={() => setDragging(false)}
              onClick={handleSelectFiles}
              label="拖拽详情图片到此处，或点击选择"
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
                  {files.map((item, index) => (
                    <li
                      key={item.path}
                      onClick={(e) => handleItemClick(index, e)}
                      className="flex cursor-pointer items-center gap-2 rounded px-2 py-1.5 text-[13px] transition-all duration-200"
                      style={
                        selected.has(index)
                          ? {
                              backgroundColor: "hsl(24 95% 53% / 0.15)",
                              color: "hsl(var(--text-primary))",
                              boxShadow: "inset 0 0 0 1px hsl(24 95% 53% / 0.5)",
                            }
                          : {
                              color: `hsl(var(--text-primary))`,
                            }
                      }
                    >
                      <span
                        className="w-5 shrink-0"
                        style={{
                          color: selected.has(index)
                            ? "hsl(24 95% 53%)"
                            : `hsl(var(--text-tertiary))`,
                        }}
                      >
                        {index + 1}
                      </span>
                      <span className="truncate" title={item.path}>
                        {item.name}
                      </span>
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div className="grid grid-cols-2 gap-2">
              <Button variant="ghost" size="sm" onClick={moveUp} disabled={!canMoveUp}>
                上移
              </Button>
              <Button variant="ghost" size="sm" onClick={moveDown} disabled={!canMoveDown}>
                下移
              </Button>
              <Button
                variant="ghost"
                size="sm"
                onClick={deleteSelected}
                disabled={selected.size === 0}
              >
                删除
              </Button>
              <Button variant="ghost" size="sm" onClick={handlePreview}>
                拼接预览
              </Button>
            </div>
          </CardContent>
        </Card>

        <Card className="flex min-h-0 flex-col">
          <CardHeader>
            <CardTitle>拼接预览</CardTitle>
          </CardHeader>
          <CardContent className="flex min-h-0 flex-1 flex-col gap-3">
            {previewReady && files.length > 0 ? (
              <JdPreview files={files} />
            ) : (
              <div
                className="flex min-h-0 flex-1 items-center justify-center rounded-md transition-colors duration-200"
                style={{ backgroundColor: `hsl(var(--input-bg) / 0.5)` }}
              >
                <p className="text-[13px]" style={{ color: `hsl(var(--text-tertiary))` }}>
                  点击「拼接预览」查看整体顺序
                </p>
              </div>
            )}

            <ProgressPanel progress={progress} />

            <Button
              onClick={() => void handleExport()}
              disabled={processing || files.length === 0}
            >
              {processing ? "输出中..." : "输出"}
            </Button>
          </CardContent>
        </Card>
      </div>

      <Card className="flex h-[150px] shrink-0 flex-col">
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
