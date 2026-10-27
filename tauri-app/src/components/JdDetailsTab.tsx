import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { open } from "@tauri-apps/plugin-dialog";
import { invoke } from "@tauri-apps/api/core";
import { ArrowDownOutlined, ArrowUpOutlined, DeleteOutlined, EyeOutlined } from "@ant-design/icons";
import { Button, Empty, Typography } from "antd";
import {
  DropZone,
  FileRow,
  ListWell,
  LogViewer,
  Page,
  Panel,
  ProgressPanel,
  fileName,
  useBatchProgress,
  useFileDrop,
  useLogger,
} from "@/ui-kit";
import { IMAGE_EXTENSIONS } from "@/lib/images";
import { JdPreview } from "./JdPreview";
import type { BatchResult } from "@/types";

interface JdEntry {
  path: string;
  name: string;
}

export function JdDetailsTab({ active = true }: { active?: boolean }) {
  const [files, setFiles] = useState<JdEntry[]>([]);
  const [selected, setSelected] = useState<Set<number>>(new Set());
  const [previewReady, setPreviewReady] = useState(false);
  const [processing, setProcessing] = useState(false);
  const { logs, log } = useLogger();
  const { progress, start, finish } = useBatchProgress("jd-progress", (event) =>
    log(`[*] 正在处理: ${event.file_name}`, "processing")
  );

  // 去重要基于最新列表；不在 setFiles 的更新函数里写日志（StrictMode 会执行两遍，日志重复）
  const filesRef = useRef<JdEntry[]>([]);
  useEffect(() => {
    filesRef.current = files;
  }, [files]);

  const appendFiles = useCallback(
    (paths: string[]) => {
      const existing = new Set(filesRef.current.map((item) => item.path));
      const fresh = [...new Set(paths)].filter((path) => !existing.has(path));
      if (fresh.length === 0) return;

      const sorted = [...fresh].sort((a, b) =>
        fileName(a).localeCompare(fileName(b), "zh-CN", { sensitivity: "base" })
      );
      const next = [...filesRef.current, ...sorted.map((path) => ({ path, name: fileName(path) }))];
      filesRef.current = next;
      setFiles(next);
      setPreviewReady(false);
      log(`京东详情页：已添加 ${sorted.length} 张图片。`, "info");
    },
    [log]
  );

  // 只有当前显示的页签接收拖入
  const { hovering } = useFileDrop(appendFiles, { extensions: IMAGE_EXTENSIONS, enabled: active });

  const handleSelectFiles = useCallback(async () => {
    const selectedPaths = await open({
      multiple: true,
      filters: [{ name: "Images", extensions: IMAGE_EXTENSIONS }],
    });

    if (!selectedPaths) return;
    appendFiles(Array.isArray(selectedPaths) ? selectedPaths : [selectedPaths]);
  }, [appendFiles]);

  const handleItemClick = useCallback(
    (index: number, event: React.MouseEvent) => {
      setSelected((prev) => {
        const next = new Set(prev);

        if (event.shiftKey && files.length > 0) {
          const anchor = prev.size > 0 ? Math.min(...prev) : 0;
          const [from, to] = anchor < index ? [anchor, index] : [index, anchor];
          for (let i = from; i <= to; i += 1) {
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

  // 页签隐藏时不响应 Delete，免得在别的页签误删这里的图片
  useEffect(() => {
    if (!active) return;
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Delete" && selected.size > 0) {
        event.preventDefault();
        deleteSelected();
      }
    };

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [active, deleteSelected, selected.size]);

  const handlePreview = useCallback(() => {
    if (files.length === 0) {
      log("请先拖拽详情图片到列表。", "warning");
      return;
    }
    setPreviewReady(true);
    log(`京东详情页：已加载 ${files.length} 张图片预览。`, "info");
  }, [files.length, log]);

  const handleExport = useCallback(async () => {
    if (files.length === 0) {
      log("请先拖拽详情图片到列表。", "warning");
      return;
    }

    setProcessing(true);
    start(files.length);
    log(`京东详情页：开始输出 ${files.length} 张图片。`, "processing");

    try {
      const outputDir = files[0].path.replace(/[/\\][^/\\]*$/, "");

      const result = await invoke<BatchResult>("jd_export", {
        filePaths: files.map((item) => item.path),
        outputDir,
      });

      log(result.message, "success");
      finish(`输出完成，共 ${result.output_count} 张`);
      setFiles([]);
      setSelected(new Set());
      setPreviewReady(false);
    } catch (error) {
      log(`京东详情页输出失败：${error}`, "error");
      finish("输出中断", false);
    } finally {
      setProcessing(false);
    }
  }, [files, log, start, finish]);

  const selectedIndices = useMemo(() => [...selected].sort((a, b) => a - b), [selected]);
  const canMoveUp = selectedIndices.length > 0 && selectedIndices[0] > 0;
  const canMoveDown =
    selectedIndices.length > 0 && selectedIndices[selectedIndices.length - 1] < files.length - 1;

  return (
    <Page>
      {/* 左侧列表与右侧预览：列表栏宽度与其他页面的设置栏一致（300px），切页时不跳 */}
      <div className="grid min-h-0 flex-1 grid-cols-[300px_minmax(0,1fr)] gap-5">
        <Panel
          fill
          title="详情图片"
          extra={<Typography.Text type="secondary">{files.length} 张</Typography.Text>}
        >
          <DropZone
            active={hovering}
            compact={files.length > 0}
            disabled={processing}
            onBrowse={() => void handleSelectFiles()}
            title="拖入详情图片，或点击选择"
          />
          <ListWell>
            {files.length === 0 ? (
              <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="暂无图片" className="py-9" />
            ) : (
              <ul className="m-0 list-none p-0" aria-multiselectable>
                {files.map((item, index) => (
                  <FileRow
                    key={item.path}
                    index={index + 1}
                    selected={selected.has(index)}
                    title={item.path}
                    onClick={(event) => handleItemClick(index, event)}
                  >
                    {item.name}
                  </FileRow>
                ))}
              </ul>
            )}
          </ListWell>
          <Typography.Text type="secondary" className="text-sm">
            Shift 连选、Ctrl / ⌘ 多选，Delete 键删除
          </Typography.Text>
          <div className="grid grid-cols-2 gap-2">
            <Button size="small" icon={<ArrowUpOutlined />} disabled={!canMoveUp} onClick={moveUp}>
              上移
            </Button>
            <Button size="small" icon={<ArrowDownOutlined />} disabled={!canMoveDown} onClick={moveDown}>
              下移
            </Button>
            {/* 只从列表移除，不删原图；按危险操作约束用描边 + 图标 */}
            <Button
              size="small"
              danger
              icon={<DeleteOutlined />}
              disabled={selected.size === 0}
              onClick={deleteSelected}
            >
              删除
            </Button>
            <Button size="small" icon={<EyeOutlined />} disabled={files.length === 0} onClick={handlePreview}>
              拼接预览
            </Button>
          </div>
        </Panel>

        <Panel fill className="min-w-0" title="拼接预览">
          {previewReady && files.length > 0 ? (
            <JdPreview files={files} />
          ) : (
            <div className="flex min-h-0 flex-1 items-center justify-center rounded bg-fill-quaternary">
              <Typography.Text type="secondary">点击「拼接预览」查看整体顺序</Typography.Text>
            </div>
          )}

          <ProgressPanel progress={progress} />

          <Button
            type="primary"
            block
            loading={processing}
            disabled={files.length === 0}
            onClick={() => void handleExport()}
          >
            {processing ? "输出中…" : `输出 · ${files.length} 张`}
          </Button>
        </Panel>
      </div>

      <Panel fill title="处理日志" size="small" className="h-[170px] shrink-0">
        <LogViewer logs={logs} />
      </Panel>
    </Page>
  );
}
