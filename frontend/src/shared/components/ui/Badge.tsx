import React from "react";
import { cn } from "@/shared/utils/cn";

type BadgeTone = "neutral" | "success" | "warning" | "danger" | "info" | "accent";

const tones: Record<BadgeTone, string> = {
  neutral: "bg-zinc-500/10 text-zinc-600 dark:text-zinc-300 border-zinc-500/20",
  success: "bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/20",
  warning: "bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/20",
  danger: "bg-red-500/10 text-red-600 dark:text-red-400 border-red-500/20",
  info: "bg-blue-500/10 text-blue-600 dark:text-blue-400 border-blue-500/20",
  accent: "bg-violet-500/10 text-violet-600 dark:text-violet-400 border-violet-500/20",
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
        "inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide",
        tones[tone],
        className
      )}
    >
      {children}
    </span>
  );
}
