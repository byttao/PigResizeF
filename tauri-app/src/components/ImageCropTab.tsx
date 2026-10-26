import { useCallback, useEffect, useRef, useState } from "react";
import { open } from "@tauri-apps/plugin-dialog";
import { invoke } from "@tauri-apps/api/core";
import { FolderOpenOutlined } from "@ant-design/icons";
import { Button, Checkbox, Empty, Radio, Select, Table, Typography } from "antd";
import {
  DropZone,
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
import type { BatchResult, CropJob, CropMode, CropModeOption } from "@/types";

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
  const { logs, log } = useLogger();
  const { progress, start, finish } = useBatchProgress("crop-progress", (event) =>
    log(`[*] 正在处理: ${event.file_name}`, "processing")
  );
  const wellRef = useRef<HTMLDivElement>(null);

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
        log("请先拖拽图片到列表。", "warning");
        return;
      }

      setProcessing(true);
      start(jobs.length);
      log(`图片裁切：开始处理 ${jobs.length} 张图片。`, "processing");

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
          log(`    [!] 处理失败 - ${fileName(path)}`, "error");
        });
        log(result.message, result.success ? "success" : "warning");
        finish(
          result.success
            ? `裁切完成，输出 ${result.output_count} 张`
            : `裁切完成，${result.failed_files.length} 张失败`
        );

        if (result.success) {
          setFiles([]);
        } else {
          setFiles((prev) => prev.filter((item) => !result.failed_files.includes(item.path)));
        }
      } catch (error) {
        log(`图片裁切失败：${error}`, "error");
        finish("裁切中断", false);
      } finally {
        setProcessing(false);
      }
    },
    [files, useCustomOutput, customOutputDir, log, start, finish]
  );

  const resolveModes = useCallback(async (paths: string[], fallback: CropMode): Promise<CropEntry[]> => {
    const entries: CropEntry[] = [];

    for (const path of paths) {
      let mode = fallback;
      if (fallback === "auto") {
        try {
          mode = await invoke<CropMode>("detect_crop_mode_command", { filePath: path });
        } catch {
          mode = "randomcorners";
        }
      }
      entries.push({ path, name: fileName(path), mode });
    }

    return entries;
  }, []);

  const addPaths = useCallback(
    async (paths: string[]) => {
      const freshPaths = paths.filter((path) => !filesRef.current.some((item) => item.path === path));

      if (freshPaths.length === 0) {
        return;
      }

      const entries = await resolveModes(freshPaths, globalMode);
      setFiles((prev) => [...prev, ...entries]);
      log(`图片裁切：已添加 ${entries.length} 张图片。`, "info");

      if (quickMode) {
        void runCrop(entries);
      }
    },
    [globalMode, quickMode, resolveModes, runCrop, log]
  );

  const { hovering } = useFileDrop((paths) => void addPaths(paths), { extensions: IMAGE_EXTENSIONS });

  const handleSelectFiles = useCallback(async () => {
    const selected = await open({
      multiple: true,
      filters: [{ name: "Images", extensions: IMAGE_EXTENSIONS }],
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
            const detected = await invoke<CropMode>("detect_crop_mode_command", { filePath: item.path });
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

  const handleItemModeChange = useCallback((path: string, mode: CropMode) => {
    setFiles((prev) => prev.map((item) => (item.path === path ? { ...item, mode } : item)));
  }, []);

  return (
    <Page>
      <Workspace
        aside={
          <>
            <Panel title="裁切模式">
              <Radio.Group
                orientation="vertical"
                value={globalMode}
                disabled={processing}
                onChange={(e) => void handleGlobalModeChange(e.target.value)}
                options={modeOptions}
              />
              <Checkbox
                checked={quickMode}
                disabled={processing}
                onChange={(e) => setQuickMode(e.target.checked)}
              >
                快速模式：拖入后立即开始
              </Checkbox>
            </Panel>

            <Panel title="保存设置">
              <Checkbox
                checked={useCustomOutput}
                disabled={processing}
                onChange={(e) => setUseCustomOutput(e.target.checked)}
              >
                自定义保存路径
              </Checkbox>
              {useCustomOutput ? (
                <>
                  <Typography.Text
                    type={customOutputDir ? undefined : "secondary"}
                    ellipsis={{ tooltip: customOutputDir }}
                    className="rounded border px-2 py-1"
                  >
                    {customOutputDir || "未选择目录"}
                  </Typography.Text>
                  <Button
                    size="small"
                    icon={<FolderOpenOutlined />}
                    disabled={processing}
                    onClick={() => void handleBrowseOutput()}
                  >
                    选择文件夹
                  </Button>
                </>
              ) : (
                <Typography.Text type="secondary" className="text-sm">
                  默认保存到各原图所在目录
                </Typography.Text>
              )}
            </Panel>

            <div className="mt-auto flex flex-col gap-3">
              <ProgressPanel progress={progress} />
              <Button
                type="primary"
                block
                loading={processing}
                disabled={files.length === 0}
                onClick={() => void runCrop([])}
              >
                {processing ? "裁切中…" : `开始裁切 · ${files.length} 张`}
              </Button>
            </div>
          </>
        }
      >
        <Panel
          fill
          className="flex-1"
          title="待裁切图片"
          extra={
            <Typography.Text type="secondary">{files.length} 张</Typography.Text>
          }
        >
          <DropZone
            active={hovering}
            compact={files.length > 0}
            disabled={processing}
            onBrowse={() => void handleSelectFiles()}
            hint="JPG、PNG、BMP、GIF · 自动识别裁切模式"
          />
          <ListWell ref={wellRef}>
            {files.length === 0 ? (
              <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="暂无图片" className="py-9" />
            ) : (
              <Table<CropEntry>
                size="small"
                pagination={false}
                rowKey="path"
                sticky={{ getContainer: () => wellRef.current ?? document.body }}
                dataSource={files}
                columns={[
                  {
                    title: "图片文件",
                    dataIndex: "name",
                    ellipsis: { showTitle: false },
                    render: (name: string, item) => <span title={item.path}>{name}</span>,
                  },
                  {
                    title: "识别模式",
                    width: 140,
                    render: (_, item) => (
                      <Select<CropMode>
                        size="small"
                        className="w-full"
                        value={item.mode}
                        disabled={processing}
                        onChange={(mode) => handleItemModeChange(item.path, mode)}
                        options={modeOptions}
                      />
                    ),
                  },
                ]}
              />
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
