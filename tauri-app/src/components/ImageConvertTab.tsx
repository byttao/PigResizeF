import { useCallback, useEffect, useRef, useState } from "react";
import { open } from "@tauri-apps/plugin-dialog";
import { invoke } from "@tauri-apps/api/core";
import { Button, Empty, Radio, Space, Typography } from "antd";
import {
  DropZone,
  FileRow,
  ListWell,
  LogViewer,
  Page,
  Panel,
  ProgressPanel,
  Workspace,
  fileName,
  useBatchProgress,
  useFileDrop,
  useLogger,
} from "@/ui-kit";
import { IMAGE_EXTENSIONS } from "@/lib/images";
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
  const { logs, log } = useLogger();
  const { progress, start, finish } = useBatchProgress("resize-progress", (event) =>
    log(`[*] 正在处理: ${event.file_name}`, "processing")
  );

  // 去重要基于最新列表；不在 setFiles 的更新函数里写日志（StrictMode 会执行两遍，日志重复）
  const filesRef = useRef<string[]>([]);
  useEffect(() => {
    filesRef.current = files;
  }, [files]);

  /** 追加文件，自动去重。 */
  const appendFiles = useCallback(
    (paths: string[]) => {
      const existing = new Set(filesRef.current);
      const added = [...new Set(paths)].filter((path) => !existing.has(path));
      if (added.length === 0) return;

      const next = [...filesRef.current, ...added];
      filesRef.current = next;
      setFiles(next);
      log(`成功追加 ${added.length} 张图片。`, "info");
      added.forEach((path) => log(`  + ${fileName(path)}`, "info"));
      log(`当前列表共有 ${next.length} 张图片。`, "info");
    },
    [log]
  );

  // 拖到窗口任意位置都会追加到列表末尾，已存在的路径不重复加入。
  const { hovering } = useFileDrop(appendFiles, { extensions: IMAGE_EXTENSIONS });

  const handleSelectFiles = useCallback(async () => {
    const selected = await open({
      multiple: true,
      filters: [{ name: "Images", extensions: IMAGE_EXTENSIONS }],
    });

    if (!selected) return;
    const paths = Array.isArray(selected) ? selected : [selected];
    appendFiles(paths);
  }, [appendFiles]);

  const handleConvert = useCallback(async () => {
    if (files.length === 0) {
      log("请先拖拽图片或指定文件夹。", "error");
      return;
    }

    setProcessing(true);
    start(files.length);
    log("开始处理图片...", "info");

    try {
      const result = await invoke<BatchResult>("batch_resize", { files, preset });

      result.failed_files.forEach((path) => {
        log(`    [!] 处理失败 - ${fileName(path)}`, "error");
      });
      log(result.message, result.success ? "success" : "warning");
      finish(result.success ? "转换完成" : `转换完成，${result.failed_files.length} 张失败`);

      if (result.success) {
        setFiles([]);
      } else {
        setFiles((prev) => prev.filter((path) => !result.failed_files.includes(path)));
      }
    } catch (error) {
      log(`处理失败：${error}`, "error");
      finish("转换中断", false);
    } finally {
      setProcessing(false);
    }
  }, [files, preset, log, start, finish]);

  const handleClear = useCallback(() => {
    setFiles([]);
    log("图片列表已清空。", "info");
  }, [log]);

  return (
    <Page>
      <Workspace
        aside={
          <>
            <Panel title="转换样式">
              <Radio.Group
                orientation="vertical"
                value={preset}
                disabled={processing}
                onChange={(e) => setPreset(e.target.value)}
                options={PRESETS}
              />
            </Panel>
            <div className="mt-auto flex flex-col gap-3">
              <ProgressPanel progress={progress} />
              <Button
                type="primary"
                block
                loading={processing}
                disabled={files.length === 0}
                onClick={() => void handleConvert()}
              >
                {processing ? "转换中…" : `开始转换 · ${files.length} 张`}
              </Button>
            </div>
          </>
        }
      >
        <Panel
          fill
          className="flex-1"
          title="待转换图片"
          extra={
            <Space size="small">
              <Typography.Text type="secondary">{files.length} 张</Typography.Text>
              <Button
                type="link"
                size="small"
                disabled={processing || files.length === 0}
                onClick={handleClear}
              >
                清空列表
              </Button>
            </Space>
          }
        >
          <DropZone
            active={hovering}
            compact={files.length > 0}
            disabled={processing}
            onBrowse={() => void handleSelectFiles()}
            hint="JPG、PNG、BMP、GIF · 可批量添加"
          />
          <ListWell>
            {files.length === 0 ? (
              <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="暂无图片" className="py-9" />
            ) : (
              <ul className="m-0 list-none p-0">
                {files.map((path, index) => (
                  <FileRow key={`${path}-${index}`} index={index + 1} title={path}>
                    {fileName(path)}
                  </FileRow>
                ))}
              </ul>
            )}
          </ListWell>
        </Panel>
      </Workspace>

      <Panel fill title="处理日志" size="small" className="h-[170px] shrink-0">
        <LogViewer logs={logs} />
      </Panel>
    </Page>
  );
}
