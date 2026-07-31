import React from "react";
import type { LucideIcon } from "lucide-react";
import { Surface } from "./Surface";
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
  className,
}: MetricCardProps) {
  return (
    <Surface className={cn(className)} padding="md">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-xs font-medium text-neutral-500">{title}</p>
          <p className="mt-2 font-mono text-3xl font-medium tracking-tight text-neutral-950">
            {value}
          </p>
          {subtitle && (
            <p className="mt-1 text-[11px] text-neutral-500">{subtitle}</p>
          )}
        </div>
        {Icon && (
          <div className="rounded-md border border-neutral-200 bg-neutral-50 p-2 text-neutral-700">
            <Icon className="h-4 w-4" />
          </div>
        )}
      </div>
    </Surface>
  );
}
