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
  glow: _glow = false,
  padding = "md",
  children,
  ...props
}: SurfaceProps) {
  return (
    <div
      className={cn(
        "rounded-md border transition-colors",
        variant === "card" && "border-neutral-200 bg-white",
        variant === "glass" && "border-neutral-200 bg-white",
        variant === "plain" && "border-transparent bg-transparent",
        paddingMap[padding],
        className
      )}
      {...props}
    >
      {children}
    </div>
  );
}
