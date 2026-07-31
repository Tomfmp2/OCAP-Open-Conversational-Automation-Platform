import React from "react";
import { cn } from "@/shared/utils/cn";

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  hint?: string;
  error?: string;
}

export function Input({
  className,
  label,
  hint,
  error,
  id,
  ...props
}: InputProps) {
  const inputId = id || props.name;

  return (
    <div className="space-y-1.5">
      {label && (
        <label
          htmlFor={inputId}
          className="block text-xs font-semibold tracking-wide text-zinc-700 dark:text-zinc-300"
        >
          {label}
        </label>
      )}
      <input
        id={inputId}
        className={cn(
          "w-full rounded-xl border bg-white px-3.5 py-2.5 text-sm text-zinc-900 placeholder:text-zinc-400 focus-ring dark:border-zinc-700 dark:bg-zinc-950 dark:text-zinc-100 dark:placeholder:text-zinc-500",
          error
            ? "border-red-400 dark:border-red-700"
            : "border-zinc-200 dark:border-zinc-700",
          className
        )}
        {...props}
      />
      {error ? (
        <p className="text-[11px] text-red-500 dark:text-red-400">{error}</p>
      ) : hint ? (
        <p className="text-[11px] text-zinc-400 dark:text-zinc-500">{hint}</p>
      ) : null}
    </div>
  );
}
