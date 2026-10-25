import * as React from "react";
import { cn } from "@/lib/utils";

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "ghost" | "text";
  size?: "default" | "sm" | "lg" | "icon";
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant = "primary", size = "default", ...props }, ref) => {
    return (
      <button
        className={cn(
          "inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-md text-[13px] font-medium",
          "transition-all duration-200",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-500 focus-visible:ring-offset-2",
          "disabled:pointer-events-none disabled:opacity-40",
          {
            // primary: 橙色按钮，两种主题下都是橙色背景白字
            "bg-orange-500 text-white shadow hover:bg-orange-600 active:bg-orange-700":
              variant === "primary",
            // ghost: 边框按钮，深色主题用 gray-600 边框，浅色用 gray-300
            "border bg-transparent shadow-sm":
              variant === "ghost",
            // text: 纯文字按钮
            "bg-transparent":
              variant === "text",
          },
          {
            "h-9 px-4 py-2": size === "default",
            "h-8 px-3 text-xs": size === "sm",
            "h-10 px-6 text-sm": size === "lg",
            "h-9 w-9 p-0": size === "icon",
          },
          className
        )}
        style={
          variant === "ghost"
            ? {
                borderColor: `hsl(var(--border))`,
                color: `hsl(var(--text-primary))`,
              }
            : variant === "text"
            ? {
                color: `hsl(var(--text-secondary))`,
              }
            : undefined
        }
        ref={ref}
        {...props}
      />
    );
  }
);
Button.displayName = "Button";

export { Button };
