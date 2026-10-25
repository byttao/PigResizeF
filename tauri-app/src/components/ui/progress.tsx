import * as React from "react";
import { cn } from "@/lib/utils";

export interface ProgressProps extends React.HTMLAttributes<HTMLDivElement> {
  value?: number;
}

const Progress = React.forwardRef<HTMLDivElement, ProgressProps>(
  ({ className, value = 0, ...props }, ref) => (
    <div
      ref={ref}
      role="progressbar"
      aria-valuemin={0}
      aria-valuemax={100}
      aria-valuenow={Math.round(value || 0)}
      className={cn("relative h-1.5 w-full overflow-hidden rounded-full", className)}
      style={{ backgroundColor: `hsl(var(--border) / 0.6)` }}
      {...props}
    >
      <div
        className="h-full w-full flex-1 bg-orange-500 transition-all duration-300 ease-out"
        style={{ transform: `translateX(-${100 - (value || 0)}%)` }}
      />
    </div>
  )
);
Progress.displayName = "Progress";

export { Progress };
