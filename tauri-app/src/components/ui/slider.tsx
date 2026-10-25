import * as React from "react"
import { cn } from "@/lib/utils"

export interface SliderProps
  extends Omit<React.InputHTMLAttributes<HTMLInputElement>, "type"> {}

const Slider = React.forwardRef<HTMLInputElement, SliderProps>(
  ({ className, ...props }, ref) => {
    return (
      <input
        type="range"
        ref={ref}
        className={cn(
          "h-2 w-full cursor-pointer appearance-none rounded-full bg-primary/20",
          "[&::-webkit-slider-thumb]:appearance-none [&::-webkit-slider-thumb]:h-4 [&::-webkit-slider-thumb]:w-4 [&::-webkit-slider-thumb]:rounded-full [&::-webkit-slider-thumb]:bg-primary [&::-webkit-slider-thumb]:ring-2 [&::-webkit-slider-thumb]:ring-ring [&::-webkit-slider-thumb]:ring-offset-2 [&::-webkit-slider-thumb]:ring-offset-background",
          "[&::-moz-range-thumb]:h-4 [&::-moz-range-thumb]:w-4 [&::-moz-range-thumb]:rounded-full [&::-moz-range-thumb]:bg-primary [&::-moz-range-thumb]:ring-2 [&::-moz-range-thumb]:ring-ring [&::-moz-range-thumb]:ring-offset-2 [&::-moz-range-thumb]:ring-offset-background",
          className
        )}
        {...props}
      />
    )
  }
)
Slider.displayName = "Slider"

export { Slider }
