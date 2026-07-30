import React from "react";
import type { LucideIcon } from "lucide-react";
import { Surface } from "./Surface";
import { Badge } from "./Badge";
import { cn } from "@/shared/utils/cn";

interface MetricCardProps {
  title: string;
  value: string | number;
  subtitle?: string;
  icon?: LucideIcon;
  tone?: "info" | "success" | "warning" | "danger" | "accent" | "neutral";
  className?: string;
}

export function MetricCard({
  title,
  value,
  subtitle,
  icon: Icon,
  tone = "info",
  className,
}: MetricCardProps) {
  return (
    <Surface
      variant="glass"
      className={cn("relative overflow-hidden", className)}
      padding="md"
    >
      <div className="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-blue-500/50 to-transparent" />
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-xs font-medium text-zinc-500">{title}</p>
          <p className="mt-2 text-3xl font-bold tracking-tight text-zinc-900 dark:text-zinc-50">
            {value}
          </p>
          {subtitle && (
            <p className="mt-1 text-[11px] text-zinc-400">{subtitle}</p>
          )}
        </div>
        {Icon && (
          <div className="rounded-xl bg-blue-500/10 p-2 text-blue-500 dark:text-blue-400">
            <Icon className="h-4 w-4" />
          </div>
        )}
      </div>
      {tone !== "neutral" && (
        <div className="mt-3">
          <Badge tone={tone}>{tone}</Badge>
        </div>
      )}
    </Surface>
  );
}
