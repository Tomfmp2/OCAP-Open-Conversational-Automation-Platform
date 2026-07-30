import React from "react";
import { cn } from "@/shared/utils/cn";

interface SurfaceProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: "card" | "glass" | "plain";
  glow?: boolean;
  padding?: "none" | "sm" | "md" | "lg";
}

const paddingMap = {
  none: "",
  sm: "p-3",
  md: "p-5",
  lg: "p-6",
};

export function Surface({
  className,
  variant = "card",
  glow = false,
  padding = "md",
  children,
  ...props
}: SurfaceProps) {
  return (
    <div
      className={cn(
        "rounded-2xl border transition-colors",
        variant === "card" && "bg-white/90 dark:bg-zinc-900/90 border-zinc-200 dark:border-zinc-800 shadow-sm",
        variant === "glass" &&
          "bg-white/70 dark:bg-zinc-950/55 border-zinc-200/80 dark:border-zinc-800/80 backdrop-blur-xl",
        variant === "plain" && "bg-transparent border-transparent",
        glow && "glow-ring",
        paddingMap[padding],
        className
      )}
      {...props}
    >
      {children}
    </div>
  );
}
