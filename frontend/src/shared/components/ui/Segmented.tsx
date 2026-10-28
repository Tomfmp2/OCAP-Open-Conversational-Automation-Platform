"use client";

import { cn } from "@/shared/utils/cn";

/** Opciones excluyentes (filtros): un grupo de botones con aria-pressed. */
export function Segmented<T extends string>({
  label,
  options,
  value,
  onChange,
}: {
  label: string;
  options: Array<{ value: T; label: string }>;
  value: T;
  onChange: (value: T) => void;
}) {
  return (
    <div role="group" aria-label={label} className="inline-flex rounded-md border border-neutral-200 bg-white p-0.5">
      {options.map((option) => (
        <button
          key={option.value}
          type="button"
          aria-pressed={option.value === value}
          onClick={() => onChange(option.value)}
          className={cn(
            "focus-ring h-7 rounded px-3 text-sm transition-colors",
            option.value === value ? "bg-neutral-950 text-white" : "text-neutral-600 hover:text-neutral-950"
          )}
        >
          {option.label}
        </button>
      ))}
    </div>
  );
}
