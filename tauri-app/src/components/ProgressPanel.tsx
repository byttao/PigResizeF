import { Progress } from "./ui/progress";
import type { ProgressEvent } from "@/types";

/** 批处理实时进度：阶段 + 百分比 + 进度条 + 当前文件与张数。 */
export function ProgressPanel({ progress }: { progress: ProgressEvent | null }) {
  if (!progress) return null;

  const percent = Math.round(progress.percent);

  return (
    <div className="space-y-1.5" aria-live="polite">
      <div className="flex items-baseline justify-between gap-2 text-[12px]">
        <span style={{ color: `hsl(var(--text-secondary))` }}>{progress.stage}</span>
        <span className="font-medium tabular-nums" style={{ color: `hsl(var(--text-primary))` }}>
          {percent}%
        </span>
      </div>
      <Progress value={progress.percent} />
      <div
        className="flex items-baseline justify-between gap-2 text-[12px]"
        style={{ color: `hsl(var(--text-tertiary))` }}
      >
        <span className="min-w-0 truncate" title={progress.file_name}>
          {progress.file_name}
        </span>
        <span className="shrink-0 tabular-nums">
          {progress.current} / {progress.total}
        </span>
      </div>
    </div>
  );
}
