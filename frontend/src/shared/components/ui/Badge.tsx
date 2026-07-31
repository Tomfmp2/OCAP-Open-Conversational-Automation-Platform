import React from "react";
import { cn } from "@/shared/utils/cn";

type BadgeTone = "neutral" | "success" | "warning" | "danger" | "info" | "accent";

const tones: Record<BadgeTone, string> = {
  neutral: "bg-neutral-100 text-neutral-700 border-neutral-300",
  success: "bg-neutral-950 text-neutral-50 border-neutral-950",
  warning: "bg-neutral-200 text-neutral-800 border-neutral-400",
  danger: "bg-white text-neutral-950 border-neutral-950 border-2",
  info: "bg-neutral-100 text-neutral-800 border-neutral-400",
  accent: "bg-neutral-800 text-neutral-50 border-neutral-800",
};

export function Badge({
  tone = "neutral",
  className,
  children,
}: {
  tone?: BadgeTone;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-sm border px-2 py-0.5 text-[10px] font-medium uppercase tracking-wide",
        tones[tone],
        className
      )}
    >
      {children}
    </span>
  );
}
