import { cn } from "@/lib/utils";

interface DropZoneProps {
  isDragging: boolean;
  onDragOver: () => void;
  onDragLeave: () => void;
  onClick: () => void;
  label?: string;
  className?: string;
}

/** 拖拽放置区域，悬停/拖入时高亮为品牌橙。 */
export function DropZone({
  isDragging,
  onDragOver,
  onDragLeave,
  onClick,
  label = "拖拽图片到此处，或点击选择文件",
  className,
}: DropZoneProps) {
  return (
    <div
      onDragOver={(e) => {
        e.preventDefault();
        onDragOver();
      }}
      onDragLeave={onDragLeave}
      onDrop={(e) => e.preventDefault()}
      onClick={onClick}
      className={cn(
        "flex cursor-pointer flex-col items-center justify-center gap-1 rounded-lg border-2 border-dashed",
        "px-4 py-6 text-center transition-all duration-200",
        className
      )}
      style={
        isDragging
          ? {
              borderColor: "hsl(24 95% 53%)",
              backgroundColor: "hsl(24 95% 53% / 0.1)",
              color: "hsl(24 95% 53%)",
            }
          : {
              borderColor: "hsl(var(--border))",
              backgroundColor: "hsl(var(--input-bg) / 0.4)",
              color: "hsl(var(--text-secondary))",
            }
      }
    >
      <span className="text-[13px]">{label}</span>
    </div>
  );
}
